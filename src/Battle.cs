using Godot;
using System.Collections.Generic;

/// <summary>
/// Pelea por turnos estilo FF5 con la party de 4 contra el enemigo de
/// <see cref="GameState.SelectedEnemy"/> ("slime", "nullrat" o "wraith").
/// Cola de turnos por ronda: player → ally0 → ally1 → ally2 → enemy,
/// salteando muertos; Phase.Enemy = ocupado (sin input).
/// Aliados con 2 usos c/u: Tux = Shield Bash, Ada = Fireball,
/// Grace = Hotfix (35) al miembro vivo con menor fracción de HP si baja
/// de 0.6, si no ataca. El enemigo pega a un miembro aleatorio vivo.
/// Derrota solo si GameState.AllDead(). TurnLog anota cada turno.
/// Comandos: Atacar, Habilidad (del job actual, 2 usos), Reto de código
/// (puzzle al azar, 1 uso por batalla), Job (cambiar, cuesta el turno) y Huir. Victoria → XP (+level up) y vuelta al mapa.
/// El menú funciona con mouse y con teclado (foco + Enter).
/// </summary>
public partial class Battle : Control
{
	public enum Phase { Player, Enemy, Over }

	public Phase CurrentPhase { get; private set; } = Phase.Player;
	public string EnemyId { get; private set; } = "slime";
	public int EnemyHpLeft => _enemyHp;
	public int AbilityUsesLeft => _abilityUses;
	public readonly List<string> TurnLog = new();

	const string MainScene = "res://main.tscn";
	const string DungeonScene = "res://scenes/world/Dungeon.tscn";
	const string TowerScene = "res://scenes/world/Tower.tscn";
	const string BattleScene = "res://scenes/battle/Battle.tscn";
	const int AbilityUsesPerBattle = 2;
	const int PuzzleUsesPerBattle = 1;

	public static readonly Dictionary<string, (string Question, string Answer)> Puzzles = new()
	{
		["offbyone"] = ("Array de 5 elementos: ¿índice del último? (solo el número)", "4"),
		["loop"] = ("for i in range(3): print(i) — ¿qué imprime la ÚLTIMA línea?", "2"),
		["http"] = ("¿Qué código HTTP devuelve un GET exitoso? (solo el número)", "200"),
		["strictnull"] = ("¿Qué devuelve null === undefined en JS? (true/false)", "false"),
		["sql"] = ("¿Qué cláusula filtra filas ANTES de agrupar? (where/having)", "where"),
		["recursion"] = ("¿Qué necesita toda función recursiva para terminar? (dos palabras)", "caso base"),
		["bigo"] = ("Acceso por índice en un array: ¿complejidad? (ej: O(1))", "o(1)"),
		["push"] = ("¿Qué comando sube tu rama al remoto? (una palabra)", "push"),
	};

	public string PuzzleId { get; private set; } = "";
	public int PuzzleUsesLeft { get; private set; } = PuzzleUsesPerBattle;

	GameState.EnemyDef _def;
	int _enemyHp;
	int _abilityUses = AbilityUsesPerBattle;
	readonly int[] _allyUses = { AbilityUsesPerBattle, AbilityUsesPerBattle, AbilityUsesPerBattle };

	Label _message = null!;
	Label _enemyName = null!;
	Label _enemyHpLabel = null!;
	ProgressBar _enemyHpBar = null!;
	Label _playerInfo = null!;
	Label _playerHpLabel = null!;
	ProgressBar _playerHpBar = null!;
	Label _xpLabel = null!;
	Polygon2D _enemySprite = null!;
	Polygon2D _playerSprite = null!;
	readonly Polygon2D[] _allySprites = new Polygon2D[3];
	readonly Label[] _allyStatus = new Label[3];
	readonly ProgressBar[] _allyBars = new ProgressBar[3];
	Button _attackButton = null!;
	Button _abilityButton = null!;
	Button _jobButton = null!;
	Button _fleeButton = null!;
	Button _retryButton = null!;
	Button _retoButton = null!;
	PanelContainer _puzzlePanel = null!;
	Label _puzzleQuestion = null!;
	LineEdit _answerLine = null!;
	VBoxContainer _jobPanel = null!;
	PanelContainer _endPanel = null!;
	Label _endStats = null!;

	public override void _Ready()
	{
		if (!GameState.Enemies.TryGetValue(GameState.SelectedEnemy, out _def))
		{
			_def = GameState.Enemies["slime"];
		}
		EnemyId = GameState.SelectedEnemy;
		_enemyHp = _def.MaxHp;

		_message = GetNode<Label>("Message");
		_enemyName = GetNode<Label>("EnemyName");
		_enemyHpLabel = GetNode<Label>("EnemyHpLabel");
		_enemyHpBar = GetNode<ProgressBar>("EnemyHpBar");
		_playerInfo = GetNode<Label>("PlayerName");
		_playerHpLabel = GetNode<Label>("PlayerHpLabel");
		_playerHpBar = GetNode<ProgressBar>("PlayerHpBar");
		_xpLabel = GetNode<Label>("XpLabel");
		_enemySprite = GetNode<Polygon2D>("EnemySprite");
		_playerSprite = GetNode<Polygon2D>("PlayerSprite");
		for (int i = 0; i < 3; i++)
		{
			_allySprites[i] = GetNode<Polygon2D>($"Ally{i}Sprite");
			_allyStatus[i] = GetNode<Label>($"Ally{i}Status");
			_allyBars[i] = GetNode<ProgressBar>($"Ally{i}Bar");
			string allyJob = GameState.AllyJobs[i];
			if (GameState.Jobs.TryGetValue(allyJob, out GameState.JobDef ajob))
			{
				_allySprites[i].Color = ajob.Tint;
			}
		}
		_attackButton = GetNode<Button>("Menu/AttackButton");
		_abilityButton = GetNode<Button>("Menu/AbilityButton");
		_jobButton = GetNode<Button>("Menu/JobButton");
		_fleeButton = GetNode<Button>("Menu/FleeButton");
		_retryButton = GetNode<Button>("RetryButton");
		_retoButton = GetNode<Button>("Menu/RetoButton");
		_puzzlePanel = GetNode<PanelContainer>("PuzzlePanel");
		_puzzleQuestion = GetNode<Label>("PuzzlePanel/VBox/Question");
		_answerLine = GetNode<LineEdit>("PuzzlePanel/VBox/AnswerLine");
		_jobPanel = GetNode<VBoxContainer>("JobPanel");
		_endPanel = GetNode<PanelContainer>("EndPanel");
		_endStats = GetNode<Label>("EndPanel/VBox/EndStats");

		_enemyName.Text = _def.Name;
		_enemySprite.Color = _def.Tint;
		_enemySprite.Scale = new Vector2(_def.Scale, _def.Scale);
		_enemyHpBar.MaxValue = _def.MaxHp;
		_playerHpBar.MaxValue = GameState.PlayerMaxHp;
		RefreshUi();
		Say($"¡Un {_def.Name} salvaje aparece! ¡Tu turno, dev!");
		_attackButton.GrabFocus();
	}

	public void OnAttackPressed()
	{
		ChooseAttack();
	}

	public void OnAbilityPressed()
	{
		UseAbility();
	}

	public void OnJobPressed()
	{
		ToggleJobPanel();
	}

	public void OnFleePressed()
	{
		if (CurrentPhase != Phase.Player)
		{
			return;
		}
		CurrentPhase = Phase.Over;
		SetMenuEnabled(false);
		GameState.CalmSteps = 120;
		Say("¡Huiste del combate como un deploy un viernes!");
		ReturnToMap(0.8);
	}

	public void OnRetryPressed()
	{
		GameState.ResetHp();
		GetTree().ChangeSceneToFile(BattleScene);
	}

	public void OnPlayAgainPressed()
	{
		GameState.ResetRun();
		GetTree().ChangeSceneToFile(MainScene);
	}

	public void OnRetoPressed()
	{
		OpenPuzzle();
	}

	public void OnPuzzleSubmitPressed()
	{
		if (_answerLine != null)
		{
			SubmitPuzzleAnswer(_answerLine.Text);
		}
	}

	public void OnPuzzleClosePressed()
	{
		ClosePuzzle();
	}

	public void OnPuzzleTextSubmitted(string text)
	{
		SubmitPuzzleAnswer(text);
	}

	public void OnJobChosen(string jobId)
	{
		_jobPanel.Visible = false;
		if (CurrentPhase != Phase.Player)
		{
			return;
		}
		if (jobId == GameState.CurrentJob)
		{
			_attackButton.GrabFocus();
			return;
		}
		GameState.ApplyJob(jobId);
		RefreshUi();
		Say($"¡Ahora sos {GameState.Jobs[jobId].Name}! Pero cambiar cuesta el turno…");
		TurnLog.Add("player");
		ContinueAfterPlayer();
	}

	/// <summary>Reto de código: pregunta al azar, 1 uso por batalla.</summary>
	public void OpenPuzzle()
	{
		if (CurrentPhase != Phase.Player || PuzzleUsesLeft <= 0)
		{
			return;
		}
		var keys = new List<string>(Puzzles.Keys);
		PuzzleId = keys[GD.RandRange(0, keys.Count - 1)];
		_puzzleQuestion.Text = Puzzles[PuzzleId].Question;
		_puzzlePanel.Visible = true;
		_jobPanel.Visible = false;
		SetMenuEnabled(false);
		_answerLine.Text = "";
		_answerLine.GrabFocus();
	}

	/// <summary>Cierra el panel del reto sin responder (no consume el uso).</summary>
	public void ClosePuzzle()
	{
		_puzzlePanel.Visible = false;
		if (CurrentPhase == Phase.Player)
		{
			SetMenuEnabled(true);
			_attackButton.GrabFocus();
		}
	}

	/// <summary>
	/// Responde el reto abierto. Normaliza con StripEdges().ToLower().
	/// Correcta: daño 25-35 por la vía de DoOffense. Incorrecta: el turno
	/// pasa al enemigo por la misma vía que tras un ataque. Consume el uso
	/// y cierra el panel en ambos casos.
	/// </summary>
	public bool SubmitPuzzleAnswer(string answer)
	{
		if (CurrentPhase != Phase.Player || PuzzleUsesLeft <= 0)
		{
			return false;
		}
		if (_puzzlePanel == null || !_puzzlePanel.Visible || !Puzzles.ContainsKey(PuzzleId))
		{
			return false;
		}
		string normalized = (answer ?? "").StripEdges().ToLower();
		string expected = Puzzles[PuzzleId].Answer;
		bool correct = normalized == expected;
		PuzzleUsesLeft--;
		_puzzlePanel.Visible = false;
		TurnLog.Add("player");
		if (correct)
		{
			DoPlayerOffense(25, 35, $"¡conocimiento aplicado! (respuesta: {expected})");
			return true;
		}
		Say("¡Respuesta incorrecta! El bug contraataca…");
		ContinueAfterPlayer();
		return false;
	}

	/// <summary>Turno del jugador. La llama el botón Atacar o el smoke test.</summary>
	public void ChooseAttack()
	{
		if (CurrentPhase != Phase.Player || GameState.PlayerHp <= 0)
		{
			return;
		}
		TurnLog.Add("player");
		DoPlayerOffense(GameState.Atk - 2, GameState.Atk + 2, "refactor crítico");
	}

	/// <summary>Habilidad del job actual (2 usos por batalla).</summary>
	public void UseAbility()
	{
		if (CurrentPhase != Phase.Player || GameState.PlayerHp <= 0 || _abilityUses <= 0)
		{
			return;
		}
		if (!GameState.Jobs.TryGetValue(GameState.CurrentJob, out GameState.JobDef job))
		{
			return;
		}
		_abilityUses--;
		TurnLog.Add("player");
		if (job.AbilityHeals)
		{
			int heal = GD.RandRange(job.AbilityMin, job.AbilityMax);
			GameState.PlayerHp = Mathf.Min(GameState.PlayerMaxHp, GameState.PlayerHp + heal);
			RefreshUi();
			Say($"¡{job.Ability} restaura {heal} HP! (el turno pasa igual…)");
			ContinueAfterPlayer();
			return;
		}
		DoPlayerOffense(job.AbilityMin, job.AbilityMax, job.Ability);
	}

	async void DoPlayerOffense(int dmgMin, int dmgMax, string label)
	{
		CurrentPhase = Phase.Enemy;
		SetMenuEnabled(false);
		_jobPanel.Visible = false;

		int dmg = GD.RandRange(dmgMin, dmgMax);
		_enemyHp = Mathf.Max(0, _enemyHp - dmg);
		RefreshUi();
		FlashEnemy();
		Say($"¡{_def.Name} recibe {dmg} de daño! ({label})");
		await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
		if (CurrentPhase == Phase.Over)
		{
			return;
		}
		await RunAlliesAndEnemy();
	}

	async void ContinueAfterPlayer()
	{
		CurrentPhase = Phase.Enemy;
		SetMenuEnabled(false);
		_jobPanel.Visible = false;
		await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
		if (CurrentPhase == Phase.Over)
		{
			return;
		}
		await RunAlliesAndEnemy();
	}

	/// <summary>
	/// Completa la ronda: cada aliado vivo actúa en orden y el enemigo
	/// cierra si sigue vivo. La victoria se resuelve al vaciar la cola
	/// para que ally2 siempre tenga su turno en la primera ronda.
	/// </summary>
	async System.Threading.Tasks.Task RunAlliesAndEnemy()
	{
		for (int i = 0; i < 3; i++)
		{
			if (GameState.AllyHp[i] <= 0)
			{
				continue;
			}
			AllyAct(i);
			RefreshUi();
			await ToSignal(GetTree().CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);
			if (CurrentPhase == Phase.Over)
			{
				return;
			}
		}
		if (_enemyHp <= 0)
		{
			DoVictory();
			return;
		}
		EnemyStrike();
	}

	void AllyAct(int i)
	{
		TurnLog.Add($"ally{i}");
		string name = GameState.AllyNames[i];
		string jobId = GameState.AllyJobs[i];
		GameState.JobDef job = GameState.Jobs[jobId];
		if (i == 2)
		{
			// Grace: Hotfix al miembro vivo con menor fracción de HP si < 0.6.
			if (_allyUses[i] > 0)
			{
				int target = LowestFractionMember();
				float frac = FractionOf(target);
				if (target != -2 && frac < 0.6f)
				{
					_allyUses[i]--;
					int heal = GD.RandRange(job.AbilityMin, job.AbilityMax);
					if (target == -1)
					{
						GameState.PlayerHp = Mathf.Min(GameState.PlayerMaxHp, GameState.PlayerHp + heal);
					}
					else
					{
						GameState.AllyHp[target] = Mathf.Min(GameState.AllyMaxHp(target), GameState.AllyHp[target] + heal);
					}
					string who = target == -1 ? "dev" : GameState.AllyNames[target];
					Say($"¡Grace Hotfix +{heal} HP a {who}!");
					return;
				}
			}
			AllyOffense(i, name, GameState.AllyAtk(i) - 2, GameState.AllyAtk(i) + 2, "ataque");
			return;
		}
		if (_allyUses[i] > 0)
		{
			_allyUses[i]--;
			AllyOffense(i, name, job.AbilityMin, job.AbilityMax, job.Ability);
			return;
		}
		AllyOffense(i, name, GameState.AllyAtk(i) - 2, GameState.AllyAtk(i) + 2, "ataque");
	}

	void AllyOffense(int i, string name, int dmgMin, int dmgMax, string label)
	{
		int dmg = GD.RandRange(dmgMin, dmgMax);
		_enemyHp = Mathf.Max(0, _enemyHp - dmg);
		RefreshUi();
		FlashEnemy();
		Say($"¡{name} ({label}) pega {dmg} a {_def.Name}!");
	}

	int LowestFractionMember()
	{
		int target = -2;
		float best = 2.0f;
		if (GameState.PlayerHp > 0)
		{
			target = -1;
			best = FractionOf(-1);
		}
		for (int i = 0; i < 3; i++)
		{
			if (GameState.AllyHp[i] <= 0)
			{
				continue;
			}
			float f = FractionOf(i);
			if (f < best)
			{
				best = f;
				target = i;
			}
		}
		return target;
	}

	static float FractionOf(int member)
	{
		if (member == -1)
		{
			return (float)GameState.PlayerHp / GameState.PlayerMaxHp;
		}
		return (float)GameState.AllyHp[member] / GameState.AllyMaxHp(member);
	}

	async void EnemyStrike()
	{
		TurnLog.Add("enemy");
		var alive = new List<int>();
		if (GameState.PlayerHp > 0)
		{
			alive.Add(-1);
		}
		for (int i = 0; i < 3; i++)
		{
			if (GameState.AllyHp[i] > 0)
			{
				alive.Add(i);
			}
		}
		if (alive.Count == 0)
		{
			DoDefeat();
			return;
		}
		int t = alive[GD.RandRange(0, alive.Count - 1)];
		int dmg = GD.RandRange(_def.AtkMin, _def.AtkMax);
		string targetName;
		if (t == -1)
		{
			GameState.PlayerHp = Mathf.Max(0, GameState.PlayerHp - dmg);
			targetName = "dev";
		}
		else
		{
			GameState.AllyHp[t] = Mathf.Max(0, GameState.AllyHp[t] - dmg);
			targetName = GameState.AllyNames[t];
		}
		RefreshUi();
		if (GameState.AllDead())
		{
			DoDefeat();
			return;
		}
		if (GameState.PlayerHp > 0)
		{
			Say($"¡El {_def.Name} te lanza un NullPointer de {dmg} a {targetName}! ¡Tu turno!");
			CurrentPhase = Phase.Player;
			SetMenuEnabled(true);
			_attackButton.GrabFocus();
			return;
		}
		Say($"¡El {_def.Name} golpea a {targetName} ({dmg})! ¡La party sigue!");
		await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
		if (CurrentPhase == Phase.Over)
		{
			return;
		}
		await RunAlliesAndEnemy();
	}

	void DoVictory()
	{
		CurrentPhase = Phase.Over;
		string extra = GameState.RegisterVictory(_def.Xp);
		RefreshUi();
		if (EnemyId == "demonking")
		{
			GameState.DemonKingDefeated = true;
			SetMenuEnabled(false);
		_jobPanel.Visible = false;
		_puzzlePanel.Visible = false;
		_retryButton.Visible = false;
			_endStats.Text = $"Party nivel {GameState.Level} — XP {GameState.Xp}{extra}";
			_endPanel.Visible = true;
			Say("¡El monolito legacy se compila por fin…!");
			return;
		}
		Say($"¡Victoria! Bug fixeado: +{_def.Xp} XP.{extra}");
		ReturnToMap(1.5);
	}

	void DoDefeat()
	{
		CurrentPhase = Phase.Over;
		Say("GAME OVER — el legado te absorbió… pero todo deploy tiene rollback.");
		_retryButton.Visible = true;
		_retryButton.GrabFocus();
	}

	async void ReturnToMap(double delaySeconds)
	{
		await ToSignal(GetTree().CreateTimer(delaySeconds), SceneTreeTimer.SignalName.Timeout);
		// Se vuelve al mapa de origen según el área.
		GetTree().ChangeSceneToFile(GameState.AreaId switch
		{
			1 => DungeonScene,
			2 => TowerScene,
			_ => MainScene,
		});
	}

	void ToggleJobPanel()
	{
		if (CurrentPhase != Phase.Player)
		{
			return;
		}
		if (_jobPanel.Visible)
		{
			_jobPanel.Visible = false;
			_attackButton.GrabFocus();
			return;
		}
		foreach (Node child in _jobPanel.GetChildren())
		{
			child.QueueFree();
		}
		foreach (string id in GameState.UnlockedJobs)
		{
			GameState.JobDef job = GameState.Jobs[id];
			var button = new Button
			{
				Text = $"{job.Name}{(id == GameState.CurrentJob ? " ✓" : "")}",
				CustomMinimumSize = new Vector2(200, 44),
			};
			string captured = id;
			button.Pressed += () => OnJobChosen(captured);
			_jobPanel.AddChild(button);
		}
		_jobPanel.Visible = true;
		if (_jobPanel.GetChildCount() > 0 && _jobPanel.GetChild(0) is Button first)
		{
			first.CallDeferred(Button.MethodName.GrabFocus);
		}
	}

	void RefreshUi()
	{
		GameState.JobDef job = GameState.Jobs[GameState.CurrentJob];
		_enemyHpLabel.Text = $"{_def.Name} HP {_enemyHp}/{_def.MaxHp}";
		_enemyHpBar.Value = _enemyHp;
		_playerInfo.Text = $"DEV {job.Name} — Nivel {GameState.Level}";
		_playerHpLabel.Text = $"HP {GameState.PlayerHp}/{GameState.PlayerMaxHp}";
		_playerHpBar.MaxValue = GameState.PlayerMaxHp;
		_playerHpBar.Value = GameState.PlayerHp;
		_playerSprite.Color = job.Tint;
		for (int i = 0; i < 3; i++)
		{
			int hp = Mathf.Max(0, GameState.AllyHp[i]);
			int max = GameState.AllyMaxHp(i);
			_allyStatus[i].Text = $"{GameState.AllyNames[i]} HP {hp}/{max}";
			_allyBars[i].MaxValue = max;
			_allyBars[i].Value = hp;
		}
		_xpLabel.Text = $"XP {GameState.Xp}";
		_abilityButton.Text = $"✨ {job.Ability} ({_abilityUses})";
		_abilityButton.Disabled = _abilityUses <= 0;
		_retoButton.Disabled = PuzzleUsesLeft <= 0;
	}

	void Say(string text)
	{
		_message.Text = text;
	}

	void SetMenuEnabled(bool enabled)
	{
		_attackButton.Disabled = !enabled;
		_abilityButton.Disabled = !enabled || _abilityUses <= 0;
		_jobButton.Disabled = !enabled;
		_retoButton.Disabled = !enabled || PuzzleUsesLeft <= 0;
		_fleeButton.Disabled = !enabled;
	}

	void FlashEnemy()
	{
		_enemySprite.Modulate = new Color(1.0f, 0.35f, 0.35f);
		Tween tw = CreateTween();
		tw.TweenProperty(_enemySprite, "modulate", new Color(1, 1, 1), 0.25);
	}
}

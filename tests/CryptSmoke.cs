using Godot;

/// <summary>
/// Smoke de la Cripta del Null + quests 5-6 en tres fases (API + mapa):
/// A. Cadena API rápida: Accept("rats")→true; OnKill("nullrat")×3;
///    Accept("boss")→true; OnKill("wraithlord"); Accept("golems")→true;
///    OnKill("golem")×2; Accept("demonking")→true; OnKill("demonking");
///    Accept("crypt")→true; OnKill("segfault")×1 (sin SaveGame).
/// B. AreaId=3 + InDungeon, ChangeSceneToFile Crypt.tscn → Name=="Crypt".
/// C. Vaga este-oeste por la fila 19 (patrón SaveSmoke) hasta un encuentro
///    salvaje, lo gana (ChooseAttack + UseAbility si hay) y verifica que
///    vuelve a "Crypt" (FAIL si Main/Dungeon/Tower); assert QuestLog.Progress
///    de crypt &gt;= 1 (loguea el EnemyId del primer encuentro).
/// D. Fase Boss: completa crypt por API si el salvaje no era segfault,
///    Accept("keeper")→true y camina al Guardián (656, 176) con manejo de
///    batallas (ChooseAttack + UseAbility si hay); al tocar al Keeper pelea
///    hasta la victoria; assert QuestLog.Done contiene "keeper" → PASS.
/// Si la party pierde → FAIL honesto. Timeout 3000 frames → FAIL.
/// Corre con: godot-mono --headless --path . --script res://tests/CryptSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class CryptSmoke : SceneTree
{
	enum Stage { Setup, ToCrypt, Roam, ToKeeper, Done }

	const int TimeoutFrames = 3000;
	readonly Vector2 KeeperPos = new(656, 176);

	Stage _stage = Stage.Setup;
	int _frames;
	bool _setup;
	int _roamFrames;
	string _roamDir = "";
	bool _sawCryptBattle;
	bool _loggedEnemy;
	bool _loggedKeeper;
	readonly System.Collections.Generic.List<Vector2> _legs = new();

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(8);
			GameState.ResetRun();

			if (!QuestLog.Accept("rats"))
			{
				Fail("Accept(rats) devolvió false con cadena limpia.");
				return true;
			}
			QuestLog.OnKill("nullrat");
			QuestLog.OnKill("nullrat");
			QuestLog.OnKill("nullrat");
			if (!QuestLog.Done.Contains("rats"))
			{
				Fail($"Done=[{DoneStr()}] tras 3 nullrats (falta rats).");
				return true;
			}
			if (!QuestLog.Accept("boss"))
			{
				Fail("Accept(boss) devolvió false tras completar rats.");
				return true;
			}
			QuestLog.OnKill("wraithlord");
			if (!QuestLog.Done.Contains("boss"))
			{
				Fail($"Done=[{DoneStr()}] tras wraithlord (falta boss).");
				return true;
			}
			if (!QuestLog.Accept("golems"))
			{
				Fail("Accept(golems) devolvió false tras completar boss.");
				return true;
			}
			QuestLog.OnKill("golem");
			QuestLog.OnKill("golem");
			if (!QuestLog.Done.Contains("golems"))
			{
				Fail($"Done=[{DoneStr()}] tras 2 golems (falta golems).");
				return true;
			}
			if (!QuestLog.Accept("demonking"))
			{
				Fail("Accept(demonking) devolvió false tras completar golems.");
				return true;
			}
			QuestLog.OnKill("demonking");
			if (!QuestLog.Done.Contains("demonking"))
			{
				Fail($"Done=[{DoneStr()}] tras demonking (falta demonking).");
				return true;
			}
			if (!QuestLog.Accept("crypt"))
			{
				Fail("Accept(crypt) devolvió false tras completar demonking.");
				return true;
			}
			QuestLog.OnKill("segfault");
			if (QuestLog.ActiveId != "crypt" || QuestLog.Progress != 1)
			{
				Fail($"tras 1 segfault: ActiveId={QuestLog.ActiveId} Progress={QuestLog.Progress} (esperaba crypt/1).");
				return true;
			}
			GD.Print($"SMOKE: cadena 1-4 + crypt aceptada (1/2, XP={GameState.Xp}). Entrando a la cripta...");

			GameState.AreaId = 3;
			GameState.InDungeon = true;
			ChangeSceneToFile("res://scenes/world/Crypt.tscn");
			_stage = Stage.ToCrypt;
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Quit(1);
			return true;
		}

		if (CurrentScene is Battle wild)
		{
			ReleaseMoves();
			if (_stage == Stage.Roam)
			{
				_sawCryptBattle = true;
				if (!_loggedEnemy)
				{
					_loggedEnemy = true;
					GD.Print($"SMOKE: primer encuentro salvaje en cripta: EnemyId={wild.EnemyId}.");
				}
			}
			if (_stage == Stage.ToKeeper && wild.EnemyId == "keeper" && !_loggedKeeper)
			{
				_loggedKeeper = true;
				GD.Print("SMOKE: ¡toqué al Guardián del Null! Peleando...");
			}
			if (wild.CurrentPhase == Battle.Phase.Over)
			{
				bool lost = wild.GetNodeOrNull<Button>("RetryButton")?.Visible == true;
				if (lost)
				{
					GD.Print("SMOKE FAIL: la party perdió en la cripta.");
					Quit(1);
					return true;
				}
				return false;
			}
			if (wild.CurrentPhase == Battle.Phase.Player)
			{
				if (wild.AbilityUsesLeft > 0)
				{
					wild.UseAbility();
				}
				else
				{
					wild.ChooseAttack();
				}
			}
			return false;
		}

		if (_stage == Stage.ToCrypt && CurrentScene != null && CurrentScene.Name == "Crypt")
		{
			GD.Print("SMOKE: en la cripta. Vagando este-oeste hasta un encuentro salvaje...");
			_roamFrames = 0;
			_roamDir = "";
			_sawCryptBattle = false;
			_loggedEnemy = false;
			_stage = Stage.Roam;
		}
		else if (_stage == Stage.Roam && CurrentScene != null && CurrentScene.Name == "Crypt")
		{
			if (_sawCryptBattle)
			{
				if (QuestLog.Progress < 1)
				{
					GD.Print($"SMOKE FAIL: Progress={QuestLog.Progress} tras salvaje en cripta (esperaba >=1).");
					Quit(1);
					return true;
				}
				GD.Print($"SMOKE: salvaje OK (ActiveId={QuestLog.ActiveId}, Progress={QuestLog.Progress}, Done=[{DoneStr()}]). Fase Boss: al Guardián...");
				while (QuestLog.ActiveId == "crypt" && !QuestLog.Done.Contains("crypt"))
				{
					QuestLog.OnKill("segfault");
				}
				if (!QuestLog.Done.Contains("crypt"))
				{
					GD.Print($"SMOKE FAIL: Done=[{DoneStr()}] tras completar crypt por API (falta crypt).");
					Quit(1);
					return true;
				}
				if (!QuestLog.Accept("keeper"))
				{
					GD.Print($"SMOKE FAIL: Accept(keeper) devolvió false (Done=[{DoneStr()}] ActiveId={QuestLog.ActiveId}).");
					Quit(1);
					return true;
				}
				GD.Print($"SMOKE: quest keeper aceptada (XP={GameState.Xp}). Caminando al Guardián (656, 176)...");
				// Rodeo del portal de salida (656, 560, radio 24): oeste por la
				// fila 19, norte por la col 18 (sin pilares) y este hasta el boss.
				_legs.Clear();
				_legs.Add(new Vector2(592, 624));
				_legs.Add(new Vector2(592, 176));
				_stage = Stage.ToKeeper;
			}
			else
			{
				RoamStep();
			}
		}
		else if (_stage == Stage.ToKeeper && CurrentScene != null && CurrentScene.Name == "Crypt")
		{
			if (QuestLog.Done.Contains("keeper"))
			{
				GD.Print($"SMOKE PASS: cripta + boss Guardián (Done=[{DoneStr()}], XP={GameState.Xp}, Lv={GameState.Level}).");
				_stage = Stage.Done;
				Quit(0);
				return true;
			}
			while (_legs.Count > 0)
			{
				Node2D? p = CurrentScene.GetNodeOrNull<Node2D>("Player");
				if (p != null && p.Position.DistanceTo(_legs[0]) < 24.0f)
				{
					_legs.RemoveAt(0);
					continue;
				}
				break;
			}
			WalkToward(_legs.Count > 0 ? _legs[0] : KeeperPos);
		}
		else if ((_stage == Stage.Roam || _stage == Stage.ToKeeper) && CurrentScene != null
			&& (CurrentScene.Name == "Main" || CurrentScene.Name == "Dungeon" || CurrentScene.Name == "Tower"))
		{
			GD.Print($"SMOKE FAIL: batalla en cripta volvió a {CurrentScene.Name}.");
			Quit(1);
			return true;
		}
		return false;
	}

	static string DoneStr() => string.Join(",", QuestLog.Done);

	void Fail(string msg)
	{
		GD.Print($"SMOKE FAIL: {msg}");
		Quit(1);
	}

	void RoamStep()
	{
		// Ida y vuelta por la fila 19 (pasto, sin pilares ni portales).
		string next = (_roamFrames / 40) % 2 == 0 ? "move_right" : "move_left";
		_roamFrames++;
		if (next != _roamDir)
		{
			if (_roamDir != "")
			{
				Input.ActionRelease(_roamDir);
			}
			Input.ActionPress(next);
			_roamDir = next;
		}
	}

	void WalkToward(Vector2 target)
	{
		Node2D? player = CurrentScene?.GetNodeOrNull<Node2D>("Player");
		if (player == null)
		{
			return;
		}
		Vector2 d = target - player.Position;
		if (d.Length() < 12.0f)
		{
			ReleaseMoves();
			return;
		}
		ReleaseMoves();
		if (Mathf.Abs(d.X) >= Mathf.Abs(d.Y))
		{
			Input.ActionPress(d.X > 0 ? "move_right" : "move_left");
		}
		else
		{
			Input.ActionPress(d.Y > 0 ? "move_down" : "move_up");
		}
	}

	void ReleaseMoves()
	{
		_roamDir = "";
		Input.ActionRelease("move_left");
		Input.ActionRelease("move_right");
		Input.ActionRelease("move_up");
		Input.ActionRelease("move_down");
	}
}

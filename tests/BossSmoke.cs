using Godot;

/// <summary>
/// Smoke del cierre del juego en dos fases:
/// A. Batalla directa vs wraithlord (Wraithlord Legacy, 130 HP, 100 XP):
///    pelea con ChooseAttack en turnos Player (+ UseAbility si hay usos)
///    hasta la victoria; verifica BossDefeated y vuelta a Main.
/// B. Desbloquea backend/devops/frontend + RegisterVictory(100, "wild") x2
///    (→ Lv3); batalla directa vs demonking (Rey Demonio, 180 HP, 250 XP):
///    pelea hasta la victoria; verifica DemonKingDefeated y EndPanel
///    visible; llama OnPlayAgainPressed y verifica Main con XP==0,
///    Level==1, solo freelancer y !SlimeDefeated → PASS.
/// Si la party pierde alguna pelea → FAIL honesto.
/// Timeout 5400 frames → FAIL.
/// Corre con: godot-mono --headless --path . --script res://tests/BossSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class BossSmoke : SceneTree
{
	enum Stage { BossBattle, SetupDemon, DemonBattle, BackToMain, Done }

	const int TimeoutFrames = 5400;

	Stage _stage = Stage.BossBattle;
	int _frames;
	bool _setup;
	bool _demonReady;

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(21);
			GameState.ResetRun();
			GameState.EncounterKind = "boss";
			GameState.SelectedEnemy = "wraithlord";
			ChangeSceneToFile("res://scenes/battle/Battle.tscn");
			GD.Print("SMOKE: fase A, pelea directa contra Wraithlord Legacy...");
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Quit(1);
			return true;
		}

		// Batalla activa: pelear (atacar + habilidad si hay usos).
		if (CurrentScene is Battle battle)
		{
			if (battle.CurrentPhase == Battle.Phase.Over)
			{
				// Derrota (Retry visible) o victoria sin flag: FAIL honesto.
				bool lost = battle.GetNodeOrNull<Button>("RetryButton")?.Visible == true;
				bool won = _stage == Stage.BossBattle
					? GameState.BossDefeated
					: GameState.DemonKingDefeated;
				if (lost || !won)
				{
					GD.Print($"SMOKE FAIL: la party perdió o no hubo victoria (fase {_stage}).");
					Quit(1);
					return true;
				}
				if (_stage == Stage.DemonBattle && !_demonReady)
				{
					_demonReady = true;
					var end = battle.GetNodeOrNull<Control>("EndPanel");
					GD.Print($"SMOKE: Rey Demonio derrotado, EndPanel visible={end?.Visible}. Nueva partida...");
					if (end == null || !end.Visible)
					{
						GD.Print("SMOKE FAIL: EndPanel ausente u oculto tras el Rey Demonio.");
						Quit(1);
						return true;
					}
					battle.OnPlayAgainPressed();
					_stage = Stage.BackToMain;
				}
				return false;
			}
			if (battle.CurrentPhase == Battle.Phase.Player)
			{
				if (battle.AbilityUsesLeft > 0)
				{
					battle.UseAbility();
				}
				else
				{
					battle.ChooseAttack();
				}
			}
			return false;
		}

		switch (_stage)
		{
			case Stage.BossBattle:
				if (CurrentScene != null && CurrentScene.Name == "Main")
				{
					GD.Print($"SMOKE: fase A vuelta en Main (XP={GameState.Xp}, Boss={GameState.BossDefeated}).");
					if (!GameState.BossDefeated)
					{
						GD.Print("SMOKE FAIL: sin BossDefeated tras el wraithlord.");
						Quit(1);
						return true;
					}
					_stage = Stage.SetupDemon;
				}
				break;

			case Stage.SetupDemon:
				GameState.UnlockJob("backend");
				GameState.UnlockJob("devops");
				GameState.UnlockJob("frontend");
				GameState.RegisterVictory(100, "wild");
				GameState.RegisterVictory(100, "wild");
				GD.Print($"SMOKE: fase B lista (nivel={GameState.Level}, XP={GameState.Xp}). Peleando vs Rey Demonio...");
				if (GameState.Level != 3)
				{
					GD.Print($"SMOKE FAIL: nivel {GameState.Level} != 3 antes del Rey Demonio.");
					Quit(1);
					return true;
				}
				GameState.EncounterKind = "demonking";
				GameState.SelectedEnemy = "demonking";
				_demonReady = false;
				ChangeSceneToFile("res://scenes/battle/Battle.tscn");
				_stage = Stage.DemonBattle;
				break;

			case Stage.BackToMain:
				if (CurrentScene != null && CurrentScene.Name == "Main")
				{
					bool clean = GameState.Xp == 0 && GameState.Level == 1
						&& GameState.UnlockedJobs.Count == 1
						&& GameState.UnlockedJobs[0] == "freelancer"
						&& !GameState.SlimeDefeated
						&& !GameState.BossDefeated
						&& !GameState.DemonKingDefeated;
					GD.Print($"SMOKE: nueva partida (XP={GameState.Xp}, nivel={GameState.Level}, jobs=[{string.Join(",", GameState.UnlockedJobs)}]).");
					if (!clean)
					{
						GD.Print("SMOKE FAIL: ResetRun no restauró la run inicial.");
						Quit(1);
						return true;
					}
					GD.Print("SMOKE PASS: boss + Rey Demonio + nueva partida.");
					_stage = Stage.Done;
					Quit(0);
					return true;
				}
				break;
		}
		return false;
	}
}

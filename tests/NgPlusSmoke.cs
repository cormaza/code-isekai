using Godot;

/// <summary>
/// Smoke de NG+: UnlockJob(backend) + RegisterVictory(100, "wild") (→ Lv3),
/// ResetRun(newGamePlus: true) conserva Level/Xp/backend y resetea el resto,
/// batalla directa vs slime con EnemyHpLeft == 46 (36*1.3), victoria peleando
/// hasta Main; luego ResetRun() total + batalla slime con EnemyHpLeft == 36
/// + OnFleePressed (huida vuelve a Main) → PASS.
/// Corre con: godot-mono --headless --path . --script res://tests/NgPlusSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class NgPlusSmoke : SceneTree
{
	enum Stage { NgBattle, NgBack, NormalBattle, FleeBack, Done }

	const int TimeoutFrames = 1800;

	Stage _stage = Stage.NgBattle;
	int _frames;
	bool _setup;
	bool _ngAsserted;
	bool _normalAsserted;
	bool _fleeCalled;

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(15);
			GameState.ResetRun();
			GameState.UnlockJob("backend");
			GameState.RegisterVictory(100, "wild");
			if (GameState.Level != 3)
			{
				GD.Print($"SMOKE FAIL: nivel {GameState.Level} != 3 antes del NG+.");
				Quit(1);
				return true;
			}
			GameState.ResetRun(newGamePlus: true);
			bool keep = GameState.Level == 3 && GameState.Xp == 100
				&& GameState.UnlockedJobs.Contains("backend")
				&& GameState.CurrentJob == "backend"
				&& GameState.NgPlus;
			bool clean = !GameState.SlimeDefeated
				&& !GameState.BossDefeated
				&& !GameState.DemonKingDefeated
				&& QuestLog.Done.Count == 0
				&& QuestLog.ActiveId == ""
				&& GameState.SpawnPoint == null
				&& GameState.AreaId == 0
				&& !GameState.InDungeon
				&& GameState.CalmSteps == 60
				&& GameState.TotalWildWins == 0;
			GD.Print($"SMOKE: NG+ (nivel={GameState.Level}, XP={GameState.Xp}, jobs=[{string.Join(",", GameState.UnlockedJobs)}], NgPlus={GameState.NgPlus}).");
			if (!keep || !clean)
			{
				GD.Print($"SMOKE FAIL: NG+ no conserva/resetea (keep={keep}, clean={clean}).");
				Quit(1);
				return true;
			}
			GameState.SelectedEnemy = "slime";
			GameState.EncounterKind = "wild";
			ChangeSceneToFile("res://scenes/battle/Battle.tscn");
			GD.Print("SMOKE: batalla NG+ vs slime (esperado HP 46)...");
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Quit(1);
			return true;
		}

		if (CurrentScene is Battle battle)
		{
			if (battle.CurrentPhase == Battle.Phase.Over)
			{
				bool lost = battle.GetNodeOrNull<Button>("RetryButton")?.Visible == true;
				if (lost)
				{
					GD.Print($"SMOKE FAIL: la party perdió (fase {_stage}).");
					Quit(1);
					return true;
				}
				return false;
			}
			if (_stage == Stage.NgBattle && !_ngAsserted)
			{
				_ngAsserted = true;
				GD.Print($"SMOKE: slime NG+ HP={battle.EnemyHpLeft} (esperado 46).");
				if (battle.EnemyHpLeft != 46)
				{
					GD.Print($"SMOKE FAIL: HP NG+ {battle.EnemyHpLeft} != 46.");
					Quit(1);
					return true;
				}
			}
			if (_stage == Stage.NormalBattle && !_normalAsserted)
			{
				_normalAsserted = true;
				GD.Print($"SMOKE: slime normal HP={battle.EnemyHpLeft} (esperado 36).");
				if (battle.EnemyHpLeft != 36)
				{
					GD.Print($"SMOKE FAIL: HP normal {battle.EnemyHpLeft} != 36.");
					Quit(1);
					return true;
				}
			}
			if (battle.CurrentPhase == Battle.Phase.Player)
			{
				if (_stage == Stage.NormalBattle && _normalAsserted && !_fleeCalled)
				{
					_fleeCalled = true;
					GD.Print("SMOKE: huyendo del slime normal...");
					battle.OnFleePressed();
					_stage = Stage.FleeBack;
					return false;
				}
				if (_stage == Stage.NgBattle)
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
			}
			return false;
		}

		if (CurrentScene != null && CurrentScene.Name == "Main")
		{
			if (_stage == Stage.NgBattle && _ngAsserted)
			{
				GD.Print("SMOKE: victoria NG+ de vuelta en Main. Reseteando total...");
				GameState.ResetRun();
				bool clean = GameState.Level == 1 && GameState.Xp == 0
					&& !GameState.NgPlus
					&& GameState.UnlockedJobs.Count == 1
					&& GameState.UnlockedJobs[0] == "freelancer";
				if (!clean)
				{
					GD.Print("SMOKE FAIL: ResetRun() total no limpió.");
					Quit(1);
					return true;
				}
				GameState.SelectedEnemy = "slime";
				GameState.EncounterKind = "wild";
				_stage = Stage.NormalBattle;
				ChangeSceneToFile("res://scenes/battle/Battle.tscn");
				return false;
			}
			if (_stage == Stage.FleeBack)
			{
				GD.Print("SMOKE PASS: NG+ escala y resetea.");
				_stage = Stage.Done;
				Quit(0);
				return true;
			}
		}
		return false;
	}
}

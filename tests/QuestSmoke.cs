using Godot;

/// <summary>
/// Smoke de side-quests + SFX en cuatro fases:
/// A. En Main camina al Mentor (560, 368) y doble E → ActiveId == "rats".
/// B. 3 batallas directas vs nullrat (kind "wild", ChooseAttack) → Done
///    contiene "rats" y XP >= 40.
/// C. Vuelve al Mentor, doble E → ActiveId == "boss".
/// D. Batalla directa vs wraithlord (kind "boss", ataque + habilidad) →
///    Done contiene "boss" y BossDefeated → PASS.
/// Si la party pierde alguna pelea → FAIL honesto.
/// Corre con: godot-mono --headless --path . --script res://tests/QuestSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class QuestSmoke : SceneTree
{
	enum Stage { ToMentorA, Battles, ToMentorC, BossFight, Done }

	const int TimeoutFrames = 7200;
	const string BattleScene = "res://scenes/battle/Battle.tscn";
	readonly Vector2 MentorPos = new(560, 368);

	Stage _stage = Stage.ToMentorA;
	int _frames;
	bool _setup;
	int _eStep;
	int _eWait;
	int _ratsDone;
	bool _awaitReturn;
	bool _bossLaunched;

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(9);
			GameState.ResetRun();
			var packed = GD.Load<PackedScene>("res://main.tscn");
			Node main = packed.Instantiate();
			Root.AddChild(main);
			CurrentScene = main;
			GD.Print("SMOKE: fase A, caminando al Senior Dev (560, 368)...");
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Quit(1);
			return true;
		}

		// Batalla activa: pelear según la fase.
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
			if (battle.CurrentPhase == Battle.Phase.Player)
			{
				if ((_stage == Stage.BossFight) && battle.AbilityUsesLeft > 0)
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

		if (CurrentScene == null || CurrentScene.Name != "Main")
		{
			return false;
		}

		switch (_stage)
		{
			case Stage.ToMentorA:
				if (QuestLog.ActiveId == "rats")
				{
					GD.Print("SMOKE: quest rats aceptada. 3 batallas vs NullRat...");
					Input.ActionRelease("interact");
					_eStep = 0;
					_eWait = 0;
					_stage = Stage.Battles;
					break;
				}
				if (NearMentor())
				{
					ReleaseAll();
					if (DoubleE())
					{
						_eStep = 0;
						_eWait = 0;
						Input.ActionRelease("interact");
						if (QuestLog.ActiveId != "rats")
						{
							GD.Print($"SMOKE FAIL: ActiveId={QuestLog.ActiveId} != rats tras doble E.");
							Quit(1);
							return true;
						}
						GD.Print("SMOKE: quest rats aceptada. 3 batallas vs NullRat...");
						_stage = Stage.Battles;
					}
				}
				else
				{
					WalkToward(MentorPos);
				}
				break;

			case Stage.Battles:
				if (_awaitReturn)
				{
					_awaitReturn = false;
					_ratsDone++;
					GD.Print($"SMOKE: nullrat {_ratsDone}/3 (Done=[{string.Join(",", QuestLog.Done)}], XP={GameState.Xp}).");
				}
				if (_ratsDone >= 3)
				{
					if (!QuestLog.Done.Contains("rats") || GameState.Xp < 40)
					{
						GD.Print($"SMOKE FAIL: Done=[{string.Join(",", QuestLog.Done)}] XP={GameState.Xp} tras 3 nullrats.");
						Quit(1);
						return true;
					}
					GD.Print("SMOKE: fase B OK. Volviendo al Senior Dev por la 2da quest...");
					Input.ActionRelease("interact");
					_eStep = 0;
					_eWait = 0;
					_stage = Stage.ToMentorC;
					break;
				}
				GameState.SelectedEnemy = "nullrat";
				GameState.EncounterKind = "wild";
				_awaitReturn = true;
				ChangeSceneToFile(BattleScene);
				break;

			case Stage.ToMentorC:
				if (QuestLog.ActiveId == "boss")
				{
					GD.Print("SMOKE: quest boss aceptada. Peleando vs Wraithlord...");
					Input.ActionRelease("interact");
					_eStep = 0;
					_eWait = 0;
					_stage = Stage.BossFight;
					_bossLaunched = false;
					break;
				}
				if (NearMentor())
				{
					ReleaseAll();
					if (DoubleE())
					{
						_eStep = 0;
						_eWait = 0;
						Input.ActionRelease("interact");
						if (QuestLog.ActiveId != "boss")
						{
							GD.Print($"SMOKE FAIL: ActiveId={QuestLog.ActiveId} != boss tras doble E.");
							Quit(1);
							return true;
						}
						GD.Print("SMOKE: quest boss aceptada. Peleando vs Wraithlord...");
						_stage = Stage.BossFight;
						_bossLaunched = false;
					}
				}
				else
				{
					WalkToward(MentorPos);
				}
				break;

			case Stage.BossFight:
				if (!_bossLaunched)
				{
					GameState.SelectedEnemy = "wraithlord";
					GameState.EncounterKind = "boss";
					_bossLaunched = true;
					ChangeSceneToFile(BattleScene);
					break;
				}
				if (!QuestLog.Done.Contains("boss") || !GameState.BossDefeated)
				{
					GD.Print($"SMOKE FAIL: Done=[{string.Join(",", QuestLog.Done)}] Boss={GameState.BossDefeated} tras wraithlord.");
					Quit(1);
					return true;
				}
				GD.Print("SMOKE PASS: quests + sfx.");
				_stage = Stage.Done;
				Quit(0);
				return true;
		}
		return false;
	}

	bool NearMentor()
	{
		Node2D? player = CurrentScene?.GetNodeOrNull<Node2D>("World/Player");
		if (player == null)
		{
			return false;
		}
		return player.Position.DistanceTo(MentorPos) < 40.0f;
	}

	bool DoubleE()
	{
		_eWait--;
		if (_eWait > 0)
		{
			return false;
		}
		switch (_eStep)
		{
			case 0:
				Input.ActionPress("interact");
				_eWait = 15;
				_eStep = 1;
				break;
			case 1:
				Input.ActionRelease("interact");
				_eWait = 15;
				_eStep = 2;
				break;
			case 2:
				Input.ActionPress("interact");
				_eWait = 15;
				_eStep = 3;
				break;
			case 3:
				Input.ActionRelease("interact");
				_eWait = 15;
				_eStep = 4;
				break;
			default:
				_eStep = 5;
				return true;
		}
		return false;
	}

	void WalkToward(Vector2 target)
	{
		Node2D? player = CurrentScene?.GetNodeOrNull<Node2D>("World/Player");
		if (player == null)
		{
			return;
		}
		Vector2 d = target - player.Position;
		if (d.Length() < 30.0f)
		{
			ReleaseAll();
			return;
		}
		ReleaseAll();
		if (Mathf.Abs(d.X) >= Mathf.Abs(d.Y))
		{
			Input.ActionPress(d.X > 0 ? "move_right" : "move_left");
		}
		else
		{
			Input.ActionPress(d.Y > 0 ? "move_down" : "move_up");
		}
	}

	void ReleaseAll()
	{
		Input.ActionRelease("move_left");
		Input.ActionRelease("move_right");
		Input.ActionRelease("move_up");
		Input.ActionRelease("move_down");
	}
}

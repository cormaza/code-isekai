using Godot;

/// <summary>
/// Smoke de jobs en tres fases:
/// A. Desbloquea backend por código, carga la batalla vs NullRat, usa
///    Fireball (one-shot: 22-30 vs 22 HP) y verifica la victoria.
/// B. En el mapa navega al cristal frontend (272, 496), peleando con
///    Atacar si salta un encuentro salvaje; verifica desbloqueo,
///    auto-equip y cura full (MaxHp 70 = 50 + 20).
/// C. Presiona J dos veces y verifica el ciclo frontend → freelancer → backend.
/// Corre con: godot-mono --headless --path . --script res://tests/JobsSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class JobsSmoke : SceneTree
{
	enum Stage { DirectBattle, ToCrystal, Cycle, Done }

	const int TimeoutFrames = 5400;
	readonly Vector2 CrystalPos = new(272, 496);

	Stage _stage = Stage.DirectBattle;
	int _frames;
	bool _setup;
	bool _abilityUsed;
	int _cycleStep;
	int _cycleWait;

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(99);
			GameState.UnlockJob("backend");
			GameState.SelectedEnemy = "nullrat";
			GameState.FixedEncounter = false;
			var packed = GD.Load<PackedScene>("res://scenes/battle/Battle.tscn");
			Node battle = packed.Instantiate();
			Root.AddChild(battle);
			CurrentScene = battle;
			GD.Print("SMOKE: fase A, Fireball de Backend Mage vs NullRat...");
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Quit(1);
			return true;
		}

		// Si hay batalla activa y estamos yendo al cristal, pelear con Atacar.
		if (_stage == Stage.ToCrystal && CurrentScene is Battle wild)
		{
			if (wild.CurrentPhase == Battle.Phase.Player)
			{
				wild.ChooseAttack();
			}
			return false;
		}

		switch (_stage)
		{
			case Stage.DirectBattle:
				if (CurrentScene != null && CurrentScene.Name == "Main")
				{
					GD.Print($"SMOKE: fase A OK (XP={GameState.Xp}). Yendo al cristal frontend...");
					_stage = Stage.ToCrystal;
				}
				else if (CurrentScene is Battle direct && direct.CurrentPhase == Battle.Phase.Player)
				{
					if (!_abilityUsed && direct.AbilityUsesLeft > 0)
					{
						_abilityUsed = true;
						int before = direct.EnemyHpLeft;
						direct.UseAbility();
						GD.Print($"SMOKE: Fireball usada (HP enemigo {before} -> {direct.EnemyHpLeft})");
					}
					else if (_abilityUsed)
					{
						direct.ChooseAttack();
					}
				}
				break;

			case Stage.ToCrystal:
				if (CurrentScene != null && CurrentScene.Name == "Main"
					&& GameState.UnlockedJobs.Contains("frontend"))
				{
					Node2D? p = CurrentScene.GetNodeOrNull<Node2D>("World/Player");
					GD.Print($"SMOKE: cristal OK, job={GameState.CurrentJob} HP={GameState.PlayerHp}/{GameState.PlayerMaxHp}");
					if (GameState.CurrentJob == "frontend" && GameState.PlayerMaxHp == 70
						&& GameState.PlayerHp == 70 && p != null)
					{
						ReleaseAll();
						_stage = Stage.Cycle;
					}
					else
					{
						GD.Print("SMOKE FAIL: auto-equip o cura del cristal mal.");
						Quit(1);
						return true;
					}
				}
				else if (CurrentScene != null && CurrentScene.Name == "Main")
				{
					WalkToward(CrystalPos);
				}
				break;

			case Stage.Cycle:
				if (CurrentScene is Battle)
				{
					break; // gracia post-cristal cubre; si igual salta, se espera
				}
				_cycleWait--;
				if (_cycleWait <= 0)
				{
					if (_cycleStep == 0)
					{
						Input.ActionPress("cycle_job");
						_cycleWait = 5;
						_cycleStep = 1;
					}
					else if (_cycleStep == 1)
					{
						Input.ActionRelease("cycle_job");
						_cycleWait = 10;
						_cycleStep = 2;
					}
					else if (_cycleStep == 2)
					{
						GD.Print($"SMOKE: tras 1xJ job={GameState.CurrentJob}");
						if (GameState.CurrentJob != "freelancer")
						{
							GD.Print("SMOKE FAIL: el ciclo no pasó a freelancer.");
							Quit(1);
							return true;
						}
						Input.ActionPress("cycle_job");
						_cycleWait = 5;
						_cycleStep = 3;
					}
					else if (_cycleStep == 3)
					{
						Input.ActionRelease("cycle_job");
						_cycleWait = 10;
						_cycleStep = 4;
					}
					else
					{
						GD.Print($"SMOKE: tras 2xJ job={GameState.CurrentJob}");
						if (GameState.CurrentJob != "backend")
						{
							GD.Print("SMOKE FAIL: el ciclo no pasó a backend.");
							Quit(1);
							return true;
						}
						GD.Print("SMOKE PASS: jobs desbloquean, equipan y ciclan.");
						_stage = Stage.Done;
						Quit(0);
						return true;
					}
				}
				break;
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

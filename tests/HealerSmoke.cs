using Godot;

/// <summary>
/// Smoke de la clériga sanadora (mapa real):
/// Setup: load main.tscn; GameState.PlayerHp=10; GameState.AllyHp[0]=5.
/// WalkToward a (464, 368) por el camino de tierra (seguro, sin salvajes);
/// a dist&lt;34: E (press, esperar 10 frames, release, esperar 10) ×2 con
/// re-fetch del player; assert PlayerHp==PlayerMaxHp y
/// AllyHp[0]==AllyMaxHp(0) → PASS.
/// Si timeout o no cura → FAIL.
/// Corre con: godot-mono --headless --path . --script res://tests/HealerSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class HealerSmoke : SceneTree
{
	enum Stage { ToCleric, Interact, Done }

	const int TimeoutFrames = 900;
	readonly Vector2 ClericPos = new(464, 368);

	Stage _stage = Stage.ToCleric;
	int _frames;
	bool _setup;
	int _eStep;
	int _eWait;

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(12);
			GameState.ResetRun();
			var packed = GD.Load<PackedScene>("res://main.tscn");
			Node main = packed.Instantiate();
			Root.AddChild(main);
			CurrentScene = main;
			GameState.PlayerHp = 10;
			GameState.AllyHp[0] = 5;
			GD.Print($"SMOKE: yendo a la clériga (464, 368) con party herida (HP={GameState.PlayerHp}/{GameState.PlayerMaxHp}, Ally0={GameState.AllyHp[0]}/{GameState.AllyMaxHp(0)})...");
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Quit(1);
			return true;
		}

		if (CurrentScene == null || CurrentScene.Name != "Main")
		{
			return false;
		}

		// Si una batalla salvaje nos sacó del mapa (no debería: camino seguro),
		// huir no es posible desde el smoke: FAIL honesto.
		if (CurrentScene is Battle)
		{
			GD.Print("SMOKE FAIL: encuentro salvaje yendo a la clériga (el camino debería ser seguro).");
			Quit(1);
			return true;
		}

		switch (_stage)
		{
			case Stage.ToCleric:
				if (NearCleric())
				{
					ReleaseAll();
					GD.Print("SMOKE: junto a la clériga. Doble E...");
					_eStep = 0;
					_eWait = 0;
					_stage = Stage.Interact;
				}
				else
				{
					WalkToward(ClericPos);
				}
				break;

			case Stage.Interact:
				// Re-fetch del player cada frame: si nos movimos, volver a acercarse.
				if (!NearCleric())
				{
					Input.ActionRelease("interact");
					_eStep = 0;
					_eWait = 0;
					_stage = Stage.ToCleric;
					break;
				}
				if (PressE())
				{
					Input.ActionRelease("interact");
					bool healed = GameState.PlayerHp == GameState.PlayerMaxHp
						&& GameState.AllyHp[0] == GameState.AllyMaxHp(0);
					if (healed)
					{
						GD.Print($"SMOKE PASS: la clériga cura a la party (HP={GameState.PlayerHp}/{GameState.PlayerMaxHp}, Ally0={GameState.AllyHp[0]}/{GameState.AllyMaxHp(0)}).");
						_stage = Stage.Done;
						Quit(0);
						return true;
					}
					GD.Print($"SMOKE FAIL: sin cura tras doble E (HP={GameState.PlayerHp}/{GameState.PlayerMaxHp}, Ally0={GameState.AllyHp[0]}/{GameState.AllyMaxHp(0)}).");
					Quit(1);
					return true;
				}
				break;
		}
		return false;
	}

	bool NearCleric()
	{
		Node2D? player = CurrentScene?.GetNodeOrNull<Node2D>("World/Player");
		if (player == null)
		{
			return false;
		}
		return player.Position.DistanceTo(ClericPos) < 34.0f;
	}

	/// <summary>E doble: press, 10 frames, release, 10 frames, ×2. True al terminar.</summary>
	bool PressE()
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
				_eWait = 10;
				_eStep = 1;
				break;
			case 1:
				Input.ActionRelease("interact");
				_eWait = 10;
				_eStep = 2;
				break;
			case 2:
				Input.ActionPress("interact");
				_eWait = 10;
				_eStep = 3;
				break;
			case 3:
				Input.ActionRelease("interact");
				_eWait = 10;
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

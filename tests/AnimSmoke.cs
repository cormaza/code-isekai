using Godot;

/// <summary>
/// Smoke de animación de caminata en 2 frames:
/// mantiene move_right 60 frames de física y cuenta cambios de
/// Player.StepIndex y Ally0.StepIndex (frame A/B cada 0.18s);
/// suelta y sigue hasta el frame 90. PASS si ambos cambiaron al
/// menos una vez respecto al inicial. Sin pelear: si la escena
/// deja de ser el mapa → FAIL.
/// Corre con: godot-mono --headless --path . --script res://tests/AnimSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class AnimSmoke : SceneTree
{
	const int ReleaseFrame = 60;
	const int EndFrame = 90;
	const int TimeoutFrames = 900;

	int _frames;
	bool _setup;
	Player? _player;
	Ally? _ally;
	int _initialPlayer = -1;
	int _initialAlly = -1;
	int _lastPlayer = -1;
	int _lastAlly = -1;
	int _playerChanges;
	int _allyChanges;

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(6);
			var packed = GD.Load<PackedScene>("res://main.tscn");
			Node current = packed.Instantiate();
			Root.AddChild(current);
			_player = current.GetNodeOrNull<Player>("World/Player");
			_ally = current.GetNodeOrNull<Ally>("World/Ally0");
			if (_player == null || _ally == null)
			{
				GD.Print("SMOKE FAIL: no se encontró World/Player o World/Ally0.");
				Quit(1);
				return true;
			}
			_initialPlayer = _player.StepIndex;
			_initialAlly = _ally.StepIndex;
			_lastPlayer = _initialPlayer;
			_lastAlly = _initialAlly;
			GD.Print($"SMOKE anim: inicial player={_initialPlayer} ally0={_initialAlly}");
			Input.ActionPress("move_right");
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Input.ActionRelease("move_right");
			Quit(1);
			return true;
		}

		if (!IsInstanceValid(_player) || !IsInstanceValid(_ally))
		{
			GD.Print("SMOKE FAIL: se perdió el mapa (¿pelea?). Sin pelear.");
			Input.ActionRelease("move_right");
			Quit(1);
			return true;
		}

		int ps = _player!.StepIndex;
		int as_ = _ally!.StepIndex;
		if (ps != _lastPlayer)
		{
			_playerChanges++;
			_lastPlayer = ps;
		}
		if (as_ != _lastAlly)
		{
			_allyChanges++;
			_lastAlly = as_;
		}

		if (_frames == ReleaseFrame)
		{
			GD.Print($"SMOKE anim: frame {ReleaseFrame} player={ps} (cambios={_playerChanges}) ally0={as_} (cambios={_allyChanges})");
			Input.ActionRelease("move_right");
		}

		if (_frames >= EndFrame)
		{
			Input.ActionRelease("move_right");
			GD.Print($"SMOKE anim: frame {EndFrame} player={ps} (cambios={_playerChanges}) ally0={as_} (cambios={_allyChanges})");
			if (_playerChanges > 0 && _allyChanges > 0)
			{
				GD.Print("SMOKE PASS: caminar anima 2 frames.");
				Quit(0);
			}
			else
			{
				GD.Print("SMOKE FAIL: StepIndex no cambió (player o ally0 quieto en un frame).");
				Quit(1);
			}
			return true;
		}
		return false;
	}
}

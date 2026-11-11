using Godot;

/// <summary>
/// Smoke test headless en dos fases:
/// 1. Mantiene move_right 120 frames de física: el Player debe avanzar.
/// 2. Sigue 90 frames más contra el muro este: el muro debe frenarlo
///    (sin colisión llegaría a x=1352; el borde interior del muro está en x=1248).
/// Corre con: godot-mono --headless --path . --script res://tests/MovementSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class MovementSmoke : SceneTree
{
	const int WalkFrames = 120;
	const int PushFrames = 90;
	const float MinExpectedDeltaX = 100.0f;
	const float WallInnerEdgeX = 1248.0f;

	int _frames;
	bool _setup;
	Node2D? _player;
	Vector2 _start;
	Vector2 _afterWalk;

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			Setup();
			if (_player == null)
			{
				GD.Print("SMOKE FAIL: no se encontró World/Player.");
				Quit(1);
				return true;
			}
		}

		_frames++;
		if (_frames == WalkFrames)
		{
			_afterWalk = _player!.Position;
			float dx = _afterWalk.X - _start.X;
			GD.Print($"SMOKE walk: {_afterWalk} (dx={dx})");
			if (dx < MinExpectedDeltaX)
			{
				GD.Print("SMOKE FAIL: el player no avanzó lo esperado.");
				Input.ActionRelease("move_right");
				Quit(1);
				return true;
			}
		}
		if (_frames >= WalkFrames + PushFrames)
		{
			Finish();
			return true;
		}
		return false;
	}

	void Setup()
	{
		var packed = GD.Load<PackedScene>("res://main.tscn");
		Node current = packed.Instantiate();
		Root.AddChild(current);
		_player = current.GetNodeOrNull<Node2D>("World/Player");
		if (_player != null)
		{
			_start = _player.Position;
			GD.Print($"SMOKE start: {_start}");
		}
		Input.ActionPress("move_right");
	}

	void Finish()
	{
		Input.ActionRelease("move_right");
		Vector2 end = _player!.Position;
		GD.Print($"SMOKE push: {end}");
		// Sin muro estaría en ~1352; el muro lo debe frenar antes de 1248.
		if (end.X < WallInnerEdgeX && end.X > _afterWalk.X)
		{
			GD.Print("SMOKE PASS: caminó y el muro lo frenó.");
			Quit(0);
		}
		else
		{
			GD.Print("SMOKE FAIL: atravesó el muro o no llegó a él.");
			Quit(1);
		}
	}
}

using Godot;

/// <summary>
/// Smoke test de encuentro + batalla: carga el mapa, camina al norte hasta
/// tocar al Slime-Bug, pelea con ChooseAttack() hasta la victoria y verifica
/// que vuelve al mapa con el enemigo derrotado y el player con vida.
/// Corre con: godot-mono --headless --path . --script res://tests/EncounterSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class EncounterSmoke : SceneTree
{
	enum Stage { ToSlime, Battle }

	const int TimeoutFrames = 1800;

	Stage _stage = Stage.ToSlime;
	int _frames;
	bool _setup;

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			var packed = GD.Load<PackedScene>("res://main.tscn");
			Node main = packed.Instantiate();
			Root.AddChild(main);
			CurrentScene = main;
			GD.Print("SMOKE: en el mapa, caminando al norte...");
			Input.ActionPress("move_up");
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout sin terminar la batalla.");
			Quit(1);
			return true;
		}

		if (_stage == Stage.ToSlime)
		{
			if (CurrentScene != null && CurrentScene.Name == "Battle")
			{
				_stage = Stage.Battle;
				Input.ActionRelease("move_up");
				GD.Print("SMOKE: ¡encuentro! Peleando...");
			}
			return false;
		}

		// Stage.Battle
		if (CurrentScene != null && CurrentScene.Name == "Main")
		{
			Node2D? player = CurrentScene.GetNodeOrNull<Node2D>("World/Player");
			if (GameState.SlimeDefeated && player != null && GameState.PlayerHp > 0)
			{
				GD.Print($"SMOKE PASS: victoria, de vuelta en el mapa con HP {GameState.PlayerHp}.");
				Quit(0);
			}
			else
			{
				GD.Print("SMOKE FAIL: volvió al mapa sin victoria registrada.");
				Quit(1);
			}
			return true;
		}

		if (CurrentScene is Battle battle && battle.CurrentPhase == Battle.Phase.Player)
		{
			battle.ChooseAttack();
		}
		return false;
	}
}

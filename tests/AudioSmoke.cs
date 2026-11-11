using Godot;

/// <summary>
/// Smoke de audio real en dos fases:
/// A. Carga Title.tscn como CurrentScene (ChangeSceneToFile, como
///    PartySmoke: prohibido Root.AddChild + CurrentScene) y verifica que
///    hay un AudioStreamPlayer con Stream != null (title_loop en loop).
/// B. ChangeSceneToFile Battle.tscn (SelectedEnemy="slime") y verifica el
///    player de batalla con Stream != null (battle_loop en loop); pelea
///    1 ataque (ChooseAttack en Player) y deja asentar la ronda: sin
///    derrota (sin Retry visible) → no hay save que borrar → Quit(0)
///    PASS ("SMOKE PASS: audio real suena").
/// Timeout 900 frames → FAIL.
/// Corre con: godot-mono --headless --path . --script res://tests/AudioSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class AudioSmoke : SceneTree
{
	enum Stage { Title, Battle, Done }

	const int TimeoutFrames = 900;
	const int SettleFrames = 240;

	Stage _stage = Stage.Title;
	int _frames;
	bool _setup;
	bool _attacked;
	int _settleFrames;

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(7);
			GameState.ResetRun();
			GameState.SelectedEnemy = "slime";
			GameState.EncounterKind = "wild";
			ChangeSceneToFile("res://scenes/ui/Title.tscn");
			GD.Print("SMOKE: fase A, título con música...");
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Quit(1);
			return true;
		}

		if (_stage == Stage.Title)
		{
			if (CurrentScene != null && CurrentScene.Name == "Title")
			{
				if (!HasStreamPlayer(CurrentScene))
				{
					GD.Print("SMOKE FAIL: título sin AudioStreamPlayer con Stream.");
					Quit(1);
					return true;
				}
				GD.Print("SMOKE: título suena (title_loop). Fase B, batalla vs slime...");
				GameState.SelectedEnemy = "slime";
				GameState.EncounterKind = "wild";
				ChangeSceneToFile("res://scenes/battle/Battle.tscn");
				_stage = Stage.Battle;
			}
			return false;
		}

		if (CurrentScene is Battle battle)
		{
			if (battle.GetNodeOrNull<Button>("RetryButton")?.Visible == true)
			{
				GD.Print("SMOKE FAIL: derrota contra el slime.");
				Quit(1);
				return true;
			}
			if (!_attacked && battle.CurrentPhase == Battle.Phase.Player)
			{
				if (!HasStreamPlayer(battle))
				{
					GD.Print("SMOKE FAIL: batalla sin AudioStreamPlayer con Stream.");
					Quit(1);
					return true;
				}
				GD.Print("SMOKE: batalla suena (battle_loop). Atacando una vez...");
				battle.ChooseAttack();
				_attacked = true;
				return false;
			}
			if (_attacked)
			{
				_settleFrames++;
				if (_settleFrames >= SettleFrames)
				{
					GD.Print("SMOKE PASS: audio real suena.");
					_stage = Stage.Done;
					Quit(0);
					return true;
				}
			}
			return false;
		}

		// La ronda del ataque pudo basta para ganar: volver a Main también es PASS.
		if (_stage == Stage.Battle && _attacked
			&& CurrentScene != null && CurrentScene.Name == "Main")
		{
			GD.Print("SMOKE PASS: audio real suena.");
			_stage = Stage.Done;
			Quit(0);
			return true;
		}
		return false;
	}

	/// <summary>Busca en profundidad un AudioStreamPlayer con Stream != null.</summary>
	static bool HasStreamPlayer(Node node)
	{
		if (node is AudioStreamPlayer player && player.Stream != null)
		{
			return true;
		}
		foreach (Node child in node.GetChildren())
		{
			if (HasStreamPlayer(child))
			{
				return true;
			}
		}
		return false;
	}
}

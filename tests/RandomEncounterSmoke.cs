using Godot;

/// <summary>
/// Smoke de encuentros aleatorios en dos fases:
/// A. Carga la batalla directo con SelectedEnemy="nullrat" y la gana
///    (cubre al segundo enemigo de forma determinista).
/// B. Vuelve al mapa, vaga por el pasto con semilla fija hasta que salta
///    un encuentro salvaje, lo gana y verifica TotalWildWins.
/// Corre con: godot-mono --headless --path . --script res://tests/RandomEncounterSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class RandomEncounterSmoke : SceneTree
{
	enum Stage { DirectBattle, Roam, WildBattle, Done }

	const int TimeoutFrames = 3600;

	Stage _stage = Stage.DirectBattle;
	int _frames;
	bool _setup;
	int _roamFrames;
	int _wildWinsAtStart;

	readonly string[] _roamActions = { "move_up", "move_left", "move_down", "move_right" };
	string _currentRoam = "";

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(1234);
			GameState.SelectedEnemy = "nullrat";
			GameState.FixedEncounter = false;
			var packed = GD.Load<PackedScene>("res://scenes/battle/Battle.tscn");
			Node battle = packed.Instantiate();
			Root.AddChild(battle);
			CurrentScene = battle;
			GD.Print("SMOKE: fase A, pelea directa contra NullRat...");
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Quit(1);
			return true;
		}

		switch (_stage)
		{
			case Stage.DirectBattle:
				if (CurrentScene != null && CurrentScene.Name == "Main")
				{
					GD.Print($"SMOKE: fase A OK (XP={GameState.Xp}, wild={GameState.TotalWildWins}). Vagando por el pasto...");
					_wildWinsAtStart = GameState.TotalWildWins;
					_stage = Stage.Roam;
				}
				else if (CurrentScene is Battle direct && direct.CurrentPhase == Battle.Phase.Player)
				{
					GD.Print($"SMOKE: enemigo directo = {direct.EnemyId}");
					direct.ChooseAttack();
				}
				break;

			case Stage.Roam:
				if (CurrentScene != null && CurrentScene.Name == "Battle")
				{
					ReleaseRoam();
					_stage = Stage.WildBattle;
					GD.Print("SMOKE: ¡encuentro salvaje!");
				}
				else
				{
					RoamStep();
				}
				break;

			case Stage.WildBattle:
				if (CurrentScene != null && CurrentScene.Name == "Main"
					&& GameState.TotalWildWins > _wildWinsAtStart)
				{
					GD.Print($"SMOKE PASS: encuentro salvaje ganado (wild={GameState.TotalWildWins}, HP={GameState.PlayerHp}).");
					_stage = Stage.Done;
					Quit(0);
					return true;
				}
				if (CurrentScene is Battle wild)
				{
					if (_frames % 60 == 0)
					{
						GD.Print($"SMOKE: salvaje = {wild.EnemyId}, fase={wild.CurrentPhase}");
					}
					if (wild.CurrentPhase == Battle.Phase.Player)
					{
						wild.ChooseAttack();
					}
				}
				break;
		}
		return false;
	}

	void RoamStep()
	{
		// Cuadrado 40x40 frames alrededor del spawn (pasto, sin bosques).
		string next = _roamActions[(_roamFrames / 40) % 4];
		_roamFrames++;
		if (next != _currentRoam)
		{
			if (_currentRoam != "")
			{
				Input.ActionRelease(_currentRoam);
			}
			Input.ActionPress(next);
			_currentRoam = next;
		}
	}

	void ReleaseRoam()
	{
		if (_currentRoam != "")
		{
			Input.ActionRelease(_currentRoam);
			_currentRoam = "";
		}
	}
}

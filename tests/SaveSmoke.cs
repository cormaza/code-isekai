using Godot;

/// <summary>
/// Smoke de guardado + Torre del Stack en dos fases (sin pelear):
/// A. Wipe; UnlockJob("backend"); RegisterVictory(20, "wild") (→ Lv2);
///    Write(1, (100,200)); ResetRun (base: XP 0, Lv1, solo freelancer);
///    Read → true y restaurado (Lv2, XP 20, backend, AreaId 1, spawn 100,200).
/// B. AreaId=2 + InDungeon=true, ChangeSceneToFile Tower → Name=="Tower".
/// C. Vaga este-oeste por la fila 19 hasta un encuentro salvaje, lo gana y
///    verifica que vuelve a Tower (no a Main/Dungeon); Wipe final → PASS.
/// Timeout 1800 frames → FAIL.
/// Corre con: godot-mono --headless --path . --script res://tests/SaveSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class SaveSmoke : SceneTree
{
	enum Stage { Setup, ToTower, Roam, Done }

	const int TimeoutFrames = 1800;

	Stage _stage = Stage.Setup;
	int _frames;
	bool _setup;
	int _roamFrames;
	string _roamDir = "";
	bool _sawTowerBattle;

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(3);
			SaveGame.Wipe();

			// Fase A: progresar, guardar, resetear y restaurar.
			GameState.ResetRun();
			GameState.UnlockJob("backend");
			GameState.RegisterVictory(20, "wild");
			SaveGame.Write(1, new Vector2(100.0f, 200.0f));
			GameState.ResetRun();
			if (GameState.Xp != 0 || GameState.Level != 1
				|| GameState.UnlockedJobs.Count != 1
				|| GameState.UnlockedJobs[0] != "freelancer")
			{
				GD.Print("SMOKE FAIL: ResetRun no dejó la base (XP 0, Lv1, solo freelancer).");
				Quit(1);
				return true;
			}
			if (!SaveGame.Read())
			{
				GD.Print("SMOKE FAIL: Read devolvió false con save válido.");
				Quit(1);
				return true;
			}
			bool restored = GameState.Level == 2 && GameState.Xp == 20
				&& GameState.UnlockedJobs.Contains("backend")
				&& GameState.AreaId == 1
				&& GameState.SpawnPoint.HasValue
				&& GameState.SpawnPoint.Value == new Vector2(100.0f, 200.0f);
			GD.Print($"SMOKE: fase A restaurada (nivel={GameState.Level}, XP={GameState.Xp}, area={GameState.AreaId}, spawn={GameState.SpawnPoint}).");
			if (!restored)
			{
				GD.Print("SMOKE FAIL: el estado restaurado no coincide (Lv2, XP 20, backend, AreaId 1, spawn 100,200).");
				Quit(1);
				return true;
			}
			GD.Print("SMOKE: fase A OK. Fase B, entrando a la Torre del Stack...");

			// Fase B: viajar a la torre (el World autoguardará al entrar).
			GameState.AreaId = 2;
			GameState.InDungeon = true;
			ChangeSceneToFile("res://scenes/world/Tower.tscn");
			_stage = Stage.ToTower;
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Quit(1);
			return true;
		}

		if (CurrentScene is Battle wild)
		{
			ReleaseMoves();
			if (_stage == Stage.Roam)
			{
				_sawTowerBattle = true;
			}
			if (wild.CurrentPhase == Battle.Phase.Player)
			{
				wild.ChooseAttack();
			}
			return false;
		}

		if (_stage == Stage.ToTower && CurrentScene != null && CurrentScene.Name == "Tower")
		{
			GD.Print("SMOKE: en la torre. Vagando este-oeste hasta un encuentro salvaje...");
			_roamFrames = 0;
			_roamDir = "";
			_sawTowerBattle = false;
			_stage = Stage.Roam;
		}
		else if (_stage == Stage.Roam && CurrentScene != null && CurrentScene.Name == "Tower")
		{
			if (_sawTowerBattle)
			{
				GD.Print("SMOKE PASS: batalla en torre volvió a Tower + save/load OK.");
				SaveGame.Wipe();
				_stage = Stage.Done;
				Quit(0);
				return true;
			}
			RoamStep();
		}
		else if (_stage == Stage.Roam && CurrentScene != null
			&& (CurrentScene.Name == "Main" || CurrentScene.Name == "Dungeon"))
		{
			GD.Print($"SMOKE FAIL: batalla en torre volvió a {CurrentScene.Name}.");
			Quit(1);
			return true;
		}
		return false;
	}

	void RoamStep()
	{
		// Ida y vuelta por la fila 19 (pasto, sin pilares ni portales).
		string next = (_roamFrames / 40) % 2 == 0 ? "move_right" : "move_left";
		_roamFrames++;
		if (next != _roamDir)
		{
			if (_roamDir != "")
			{
				Input.ActionRelease(_roamDir);
			}
			Input.ActionPress(next);
			_roamDir = next;
		}
	}

	void ReleaseMoves()
	{
		_roamDir = "";
		Input.ActionRelease("move_left");
		Input.ActionRelease("move_right");
		Input.ActionRelease("move_up");
		Input.ActionRelease("move_down");
	}
}

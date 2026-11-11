using Godot;

/// <summary>
/// Smoke de persistencia de quests + cadena completa de 4 quests (API pura,
/// sin escenas: QuestLog/SaveGame no necesitan mapa):
/// Accept("rats")→true; OnKill("nullrat")×3 → Done rats; Accept("boss")→true;
/// OnKill("wraithlord") → Done boss; Accept("golems")→true; OnKill("golem")×1;
/// SaveGame.Write(2, (300,400)); ResetRun(); assert base (ActiveId "" y Done
/// vacío); Read()→true; assert ActiveId=="golems", Progress==1, Done=[rats,boss],
/// AreaId y SpawnPoint; OnKill("golem") → Done golems; Accept("demonking")→true;
/// OnKill("demonking") → Done demonking; Wipe() → PASS.
/// Asserts de XP relativos (solo XP>0 tras recompensas). FAIL honesto.
/// Corre con: godot-mono --headless --path . --script res://tests/QuestSaveSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class QuestSaveSmoke : SceneTree
{
	const int TimeoutFrames = 1800;

	int _frames;
	bool _setup;

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(4);
			Run();
			return true;
		}
		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Quit(1);
			return true;
		}
		return false;
	}

	void Run()
	{
		SaveGame.Wipe();
		GameState.ResetRun();

		if (!QuestLog.Accept("rats"))
		{
			Fail("Accept(rats) devolvió false con cadena limpia.");
			return;
		}
		QuestLog.OnKill("nullrat");
		QuestLog.OnKill("nullrat");
		QuestLog.OnKill("nullrat");
		if (!QuestLog.Done.Contains("rats"))
		{
			Fail($"Done=[{DoneStr()}] tras 3 nullrats (falta rats).");
			return;
		}
		if (GameState.Xp <= 0)
		{
			Fail($"XP={GameState.Xp} tras completar rats (esperaba >0).");
			return;
		}
		GD.Print($"SMOKE: rats OK (Done=[{DoneStr()}], XP={GameState.Xp}).");

		if (!QuestLog.Accept("boss"))
		{
			Fail("Accept(boss) devolvió false tras completar rats.");
			return;
		}
		QuestLog.OnKill("wraithlord");
		if (!QuestLog.Done.Contains("boss"))
		{
			Fail($"Done=[{DoneStr()}] tras wraithlord (falta boss).");
			return;
		}
		if (GameState.Xp <= 0)
		{
			Fail($"XP={GameState.Xp} tras completar boss (esperaba >0).");
			return;
		}
		GD.Print($"SMOKE: boss OK (Done=[{DoneStr()}], XP={GameState.Xp}).");

		if (!QuestLog.Accept("golems"))
		{
			Fail("Accept(golems) devolvió false tras completar boss.");
			return;
		}
		QuestLog.OnKill("golem");
		if (QuestLog.ActiveId != "golems" || QuestLog.Progress != 1 || QuestLog.Done.Contains("golems"))
		{
			Fail($"tras 1 golem: ActiveId={QuestLog.ActiveId} Progress={QuestLog.Progress} Done=[{DoneStr()}] (esperaba golems/1/sin golems).");
			return;
		}
		GD.Print("SMOKE: golems a medias (1/2). Guardando...");

		SaveGame.Write(2, new Vector2(300.0f, 400.0f));
		GameState.ResetRun();
		if (QuestLog.ActiveId != "" || QuestLog.Done.Count != 0)
		{
			Fail($"tras ResetRun: ActiveId={QuestLog.ActiveId} Done=[{DoneStr()}] (esperaba base limpia).");
			return;
		}
		if (!SaveGame.Read())
		{
			Fail("Read devolvió false con save válido.");
			return;
		}
		bool questsOk = QuestLog.ActiveId == "golems" && QuestLog.Progress == 1
			&& QuestLog.Done.Count == 2
			&& QuestLog.Done.Contains("rats") && QuestLog.Done.Contains("boss");
		bool placeOk = GameState.AreaId == 2
			&& GameState.SpawnPoint.HasValue
			&& GameState.SpawnPoint.Value == new Vector2(300.0f, 400.0f);
		if (!questsOk || !placeOk)
		{
			Fail($"restaurado: ActiveId={QuestLog.ActiveId} Progress={QuestLog.Progress} Done=[{DoneStr()}] Area={GameState.AreaId} Spawn={GameState.SpawnPoint} (esperaba golems/1/[rats,boss]/2/(300,400)).");
			return;
		}
		GD.Print("SMOKE: save/load de quests OK (golems 1/2 + [rats,boss] + área 2).");

		QuestLog.OnKill("golem");
		if (!QuestLog.Done.Contains("golems"))
		{
			Fail($"Done=[{DoneStr()}] tras 2do golem (falta golems).");
			return;
		}
		if (GameState.Xp <= 0)
		{
			Fail($"XP={GameState.Xp} tras completar golems (esperaba >0).");
			return;
		}
		GD.Print($"SMOKE: golems OK (Done=[{DoneStr()}], XP={GameState.Xp}).");

		if (!QuestLog.Accept("demonking"))
		{
			Fail("Accept(demonking) devolvió false tras completar golems.");
			return;
		}
		QuestLog.OnKill("demonking");
		if (!QuestLog.Done.Contains("demonking"))
		{
			Fail($"Done=[{DoneStr()}] tras demonking (falta demonking).");
			return;
		}
		if (GameState.Xp <= 0)
		{
			Fail($"XP={GameState.Xp} tras completar demonking (esperaba >0).");
			return;
		}
		if (QuestLog.NextOffer() != "")
		{
			Fail($"NextOffer={QuestLog.NextOffer()} con todo completo (esperaba \"\").");
			return;
		}
		GD.Print($"SMOKE: demonking OK (Done=[{DoneStr()}], XP={GameState.Xp}).");

		SaveGame.Wipe();
		GD.Print("SMOKE PASS: quests persisten y la cadena completa funciona.");
		Quit(0);
	}

	static string DoneStr() => string.Join(",", QuestLog.Done);

	void Fail(string msg)
	{
		GD.Print($"SMOKE FAIL: {msg}");
		SaveGame.Wipe();
		Quit(1);
	}
}

using Godot;
using System.Collections.Generic;

/// <summary>
/// Smoke de la party de 4 + mazmorra en tres fases:
/// A. Batalla directa vs wraith (Legacy Wraith, 44 HP, 30 XP): ataca con
///    ChooseAttack en turnos Player; al volver a Main verifica que el
///    TurnLog tenga ally0/1/2 y que XP >= 30.
/// B. En Main camina derecha 60 frames y verifica la fila india:
///    d0 &lt; d1 &lt; d2, cada una &gt; 30 y &lt; 500.
/// D. Pelea directa dentro de la mazmorra y verifica que la victoria
///    vuelve a Dungeon (no al overworld); luego WalkToward al ExitPortal
///    (656, 560) y al volver a Main con !InDungeon → PASS.
/// Timeout 7200 frames → FAIL.
/// Corre con: godot-mono --headless --path . --script res://tests/PartySmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class PartySmoke : SceneTree
{
	enum Stage { DirectBattle, WalkRight, ToDungeon, DungeonBattle, ToExit, Done }

	const int TimeoutFrames = 7200;
	readonly Vector2 EntryPortalPos = new(656, 96);
	readonly Vector2 ExitPortalPos = new(656, 560);

	Stage _stage = Stage.DirectBattle;
	int _frames;
	bool _setup;
	int _walkFrames;
	bool _dungeonBattleSeen;
	List<string> _logSnapshot = new();

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(11);
			GameState.SelectedEnemy = "wraith";
			GameState.FixedEncounter = false;
			ChangeSceneToFile("res://scenes/battle/Battle.tscn");
			GD.Print("SMOKE: fase A, pelea directa contra Legacy Wraith...");
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Quit(1);
			return true;
		}

		// Batalla activa: pelear con Atacar en turnos Player.
		if (CurrentScene is Battle battle)
		{
			ReleaseAll(); // el player no debe derivar mientras pelea
			if (_stage == Stage.DungeonBattle)
			{
				_dungeonBattleSeen = true;
			}
			_logSnapshot = new List<string>(battle.TurnLog);
			if (battle.CurrentPhase == Battle.Phase.Player)
			{
				battle.ChooseAttack();
			}
			return false;
		}

		switch (_stage)
		{
			case Stage.DirectBattle:
				if (CurrentScene != null && CurrentScene.Name == "Main")
				{
					bool hasAllies = _logSnapshot.Contains("ally0")
						&& _logSnapshot.Contains("ally1")
						&& _logSnapshot.Contains("ally2");
					GD.Print($"SMOKE: fase A vuelta en Main (XP={GameState.Xp}, log=[{string.Join(",", _logSnapshot)}])");
					if (!hasAllies || GameState.Xp < 30)
					{
						GD.Print("SMOKE FAIL: TurnLog sin ally0/1/2 o XP < 30.");
						Quit(1);
						return true;
					}
					GD.Print("SMOKE: fase A OK. Fase B, caminar derecha 60 frames...");
					ReleaseAll();
					Input.ActionPress("move_right");
					_walkFrames = 0;
					_stage = Stage.WalkRight;
				}
				break;

			case Stage.WalkRight:
				if (CurrentScene == null || CurrentScene.Name != "Main")
				{
					break; // Gracia/dirt: no debería haber batalla, se espera.
				}
				_walkFrames++;
				if (_walkFrames < 60)
				{
					break;
				}
				ReleaseAll();
				if (!CheckFormation())
				{
					Quit(1);
					return true;
				}
				GD.Print("SMOKE: fase B OK. Fase C, yendo a la mazmorra...");
				_stage = Stage.ToDungeon;
				break;

			case Stage.ToDungeon:
				if (CurrentScene != null && CurrentScene.Name == "Dungeon")
				{
					ReleaseAll();
					if (!GameState.InDungeon)
					{
						GD.Print("SMOKE FAIL: en Dungeon pero InDungeon=false.");
						Quit(1);
						return true;
					}
					GD.Print("SMOKE: en la mazmorra. Pelea directa para probar el regreso...");
					ReleaseAll();
					GameState.SelectedEnemy = "nullrat";
					GameState.FixedEncounter = false;
					_dungeonBattleSeen = false;
					ChangeSceneToFile("res://scenes/battle/Battle.tscn");
					_stage = Stage.DungeonBattle;
				}
				else if (CurrentScene != null && CurrentScene.Name == "Main")
				{
					WalkToward(EntryPortalPos);
				}
				break;

			case Stage.DungeonBattle:
				if (CurrentScene != null && CurrentScene.Name == "Dungeon" && _dungeonBattleSeen)
				{
					if (!GameState.InDungeon)
					{
						GD.Print("SMOKE FAIL: volvió a Dungeon con InDungeon=false.");
						Quit(1);
						return true;
					}
					ReleaseAll();
					GD.Print("SMOKE: batalla en mazmorra volvió a Dungeon. Yendo al portal de salida...");
					_stage = Stage.ToExit;
				}
				else if (CurrentScene != null && CurrentScene.Name == "Main")
				{
					GD.Print("SMOKE FAIL: batalla en mazmorra volvió a Main.");
					Quit(1);
					return true;
				}
				break;

			case Stage.ToExit:
				if (CurrentScene != null && CurrentScene.Name == "Main")
				{
					if (GameState.InDungeon)
					{
						// Rebote por batalla en la mazmorra: volver a entrar.
						GD.Print("SMOKE: rebote a Main con InDungeon, re-entrando...");
						_stage = Stage.ToDungeon;
						break;
					}
					ReleaseAll();
					GD.Print("SMOKE PASS: party + mazmorra + wraith OK.");
					_stage = Stage.Done;
					Quit(0);
					return true;
				}
				else if (CurrentScene != null && CurrentScene.Name == "Dungeon")
				{
					WalkToward(ExitPortalPos);
				}
				break;
		}
		return false;
	}

	bool CheckFormation()
	{
		Node2D? player = GetMapNode("Player");
		Node2D? a0 = GetMapNode("Ally0");
		Node2D? a1 = GetMapNode("Ally1");
		Node2D? a2 = GetMapNode("Ally2");
		if (player == null || a0 == null || a1 == null || a2 == null)
		{
			GD.Print("SMOKE FAIL: falta Player o aliados en el mapa.");
			return false;
		}
		float d0 = player.Position.DistanceTo(a0.Position);
		float d1 = player.Position.DistanceTo(a1.Position);
		float d2 = player.Position.DistanceTo(a2.Position);
		GD.Print($"SMOKE: formación d0={d0} d1={d1} d2={d2}");
		if (!(d0 < d1 && d1 < d2 && d0 > 30.0f && d2 < 500.0f && d1 > 30.0f && d0 < 500.0f && d1 < 500.0f && d2 > 30.0f))
		{
			GD.Print("SMOKE FAIL: formación desordenada o fuera de rango.");
			return false;
		}
		return true;
	}

	void WalkToward(Vector2 target, float radius = 30.0f)
	{
		Node2D? player = GetMapNode("Player");
		if (player == null)
		{
			return;
		}
		Vector2 d = target - player.Position;
		if (d.Length() < 4.0f)
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

	/// <summary>Re-fetch del nodo: cambia tras cada batalla (Main↔Battle↔Dungeon).</summary>
	Node2D? GetMapNode(string name)
	{
		if (CurrentScene == null)
		{
			return null;
		}
		return CurrentScene.GetNodeOrNull<Node2D>($"World/{name}")
			?? CurrentScene.GetNodeOrNull<Node2D>(name);
	}

	void ReleaseAll()
	{
		Input.ActionRelease("move_left");
		Input.ActionRelease("move_right");
		Input.ActionRelease("move_up");
		Input.ActionRelease("move_down");
	}
}

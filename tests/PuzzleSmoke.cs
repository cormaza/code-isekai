using Godot;
using System.Linq;

/// <summary>
/// Smoke del comando Reto en dos fases:
/// A. Batalla directa vs slime (36 HP): en turno Player abre el reto con
///    OpenPuzzle(), responde bien con Battle.Puzzles[id].Answer y verifica
///    true, daño 25-35 (EnemyHpLeft &lt;= 11; si fue one-shot la victoria
///    inmediata se tolera) y PuzzleUsesLeft == 0. Si sigue en batalla,
///    ChooseAttack hasta la victoria → Main, con XP relativo +20.
/// B. Batalla directa vs nullrat con los aliados caídos (para que el
///    contraataque caiga sí o sí sobre el player): responde mal con
///    "zzz_nada_que_ver", verifica false y, tras el contraataque,
///    PlayerHp &lt; HP capturado. Luego pelea normal hasta la victoria
///    → Main → PASS.
/// GameState es static y persiste entre fases del mismo proceso: los
/// asserts de XP son relativos y la fase B no asume nivel ni HP.
/// Timeout 3600 frames → FAIL.
/// Corre con: godot-mono --headless --path . --script res://tests/PuzzleSmoke.cs
/// Sale con código 0 (PASS) o 1 (FAIL).
/// </summary>
public partial class PuzzleSmoke : SceneTree
{
	enum Stage { PhaseA, PhaseB, Done }

	const int TimeoutFrames = 3600;

	Stage _stage = Stage.PhaseA;
	int _frames;
	bool _setup;
	bool _aPuzzleDone;
	int _xpStartA;
	bool _bPuzzleDone;
	bool _bPunished;
	int _bCapturedHp;

	public override bool _PhysicsProcess(double delta)
	{
		if (!_setup)
		{
			_setup = true;
			GD.Seed(5);
			_xpStartA = GameState.Xp;
			GameState.SelectedEnemy = "slime";
			GameState.FixedEncounter = false;
			ChangeSceneToFile("res://scenes/battle/Battle.tscn");
			GD.Print("SMOKE: fase A, reto correcto vs slime...");
		}

		_frames++;
		if (_frames > TimeoutFrames)
		{
			GD.Print("SMOKE FAIL: timeout.");
			Quit(1);
			return true;
		}

		if (CurrentScene is Battle battle)
		{
			if (_stage == Stage.PhaseA)
			{
				PhaseABattle(battle);
			}
			else if (_stage == Stage.PhaseB)
			{
				PhaseBBattle(battle);
			}
			return false;
		}

		if (CurrentScene != null && CurrentScene.Name == "Main")
		{
			if (_stage == Stage.PhaseA)
			{
				GD.Print($"SMOKE: fase A vuelta en Main (XP={GameState.Xp}).");
				if (GameState.Xp < _xpStartA + 20)
				{
					GD.Print("SMOKE FAIL: XP no subió +20 tras la victoria con reto.");
					Quit(1);
					return true;
				}
				GD.Print("SMOKE: fase A OK. Fase B, reto incorrecto vs nullrat...");
				GameState.SelectedEnemy = "nullrat";
				GameState.FixedEncounter = false;
				GameState.AllyHp[0] = 0;
				GameState.AllyHp[1] = 0;
				GameState.AllyHp[2] = 0;
				_stage = Stage.PhaseB;
				ChangeSceneToFile("res://scenes/battle/Battle.tscn");
			}
			else if (_stage == Stage.PhaseB)
			{
				if (!_bPunished)
				{
					GD.Print("SMOKE FAIL: volvió a Main sin registrar el castigo del reto.");
					Quit(1);
					return true;
				}
				GD.Print("SMOKE PASS: puzzle premia y castiga.");
				_stage = Stage.Done;
				Quit(0);
				return true;
			}
		}
		return false;
	}

	void PhaseABattle(Battle battle)
	{
		if (battle.CurrentPhase != Battle.Phase.Player)
		{
			return;
		}
		if (!_aPuzzleDone)
		{
			_aPuzzleDone = true;
			battle.OpenPuzzle();
			string id = battle.PuzzleId;
			if (string.IsNullOrEmpty(id) || !Battle.Puzzles.ContainsKey(id))
			{
				GD.Print("SMOKE FAIL: OpenPuzzle no dejó PuzzleId válido.");
				Quit(1);
				return;
			}
			bool ok = battle.SubmitPuzzleAnswer(Battle.Puzzles[id].Answer);
			GD.Print($"SMOKE: fase A puzzle '{id}' → {ok}, EnemyHpLeft={battle.EnemyHpLeft}.");
			if (!ok)
			{
				GD.Print("SMOKE FAIL: la respuesta correcta devolvió false.");
				Quit(1);
				return;
			}
			if (battle.EnemyHpLeft > 36 - 25)
			{
				GD.Print("SMOKE FAIL: el reto correcto pegó menos de 25.");
				Quit(1);
				return;
			}
			if (battle.PuzzleUsesLeft != 0)
			{
				GD.Print("SMOKE FAIL: el reto no consumió su único uso.");
				Quit(1);
				return;
			}
			return;
		}
		battle.ChooseAttack();
	}

	void PhaseBBattle(Battle battle)
	{
		if (battle.CurrentPhase != Battle.Phase.Player)
		{
			return;
		}
		if (!_bPuzzleDone)
		{
			_bPuzzleDone = true;
			_bCapturedHp = GameState.PlayerHp;
			battle.OpenPuzzle();
			bool ok = battle.SubmitPuzzleAnswer("zzz_nada_que_ver");
			GD.Print($"SMOKE: fase B puzzle incorrecto → {ok}, HP era {_bCapturedHp}.");
			if (ok)
			{
				GD.Print("SMOKE FAIL: la respuesta basura devolvió true.");
				Quit(1);
			}
			return;
		}
		if (!_bPunished)
		{
			if (!battle.TurnLog.Contains("enemy"))
			{
				return;
			}
			_bPunished = true;
			GD.Print($"SMOKE: contraataque recibido (HP {_bCapturedHp} → {GameState.PlayerHp}).");
			if (!(GameState.PlayerHp < _bCapturedHp))
			{
				GD.Print("SMOKE FAIL: el bug no contraatacó al player.");
				Quit(1);
				return;
			}
		}
		battle.ChooseAttack();
	}
}

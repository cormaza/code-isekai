using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Estado global de la run (static: sobrevive a los cambios de escena
/// dentro de la misma ejecución, sin necesidad de autoload).
/// Stats base + jobs + party de 4 + encuentro actual + spawn + mazmorra.
/// </summary>
public static class GameState
{
	public struct JobDef
	{
		public string Name;
		public string Ability;
		public int HpMod;
		public int AtkMod;
		public int AbilityMin;
		public int AbilityMax;
		public bool AbilityHeals;
		public Color Tint;
		public string Blurb;
	}

	public static readonly Dictionary<string, JobDef> Jobs = new()
	{
		["freelancer"] = new JobDef
		{
			Name = "Freelancer", Ability = "Café", HpMod = 0, AtkMod = 0,
			AbilityMin = 20, AbilityMax = 20, AbilityHeals = true,
			Tint = new Color(0.95f, 0.60f, 0.25f), Blurb = "Hace de todo, especialista en nada",
		},
		["frontend"] = new JobDef
		{
			Name = "Frontend Knight", Ability = "Shield Bash", HpMod = 20, AtkMod = 2,
			AbilityMin = 18, AbilityMax = 24, AbilityHeals = false,
			Tint = new Color(0.30f, 0.60f, 1.00f), Blurb = "Tanquea pixels y deadlines",
		},
		["backend"] = new JobDef
		{
			Name = "Backend Mage", Ability = "Fireball", HpMod = -10, AtkMod = 0,
			AbilityMin = 22, AbilityMax = 30, AbilityHeals = false,
			Tint = new Color(0.70f, 0.40f, 1.00f), Blurb = "Cañón de cristal: pega fuerte, cae rápido",
		},
		["devops"] = new JobDef
		{
			Name = "DevOps Alchemist", Ability = "Hotfix", HpMod = 10, AtkMod = -2,
			AbilityMin = 35, AbilityMax = 35, AbilityHeals = true,
			Tint = new Color(0.30f, 0.85f, 0.45f), Blurb = "Todo rollback tiene solución",
		},
	};

	// Party: el player + 3 aliados con jobs fijos.
	public static readonly string[] AllyNames = { "Tux", "Ada", "Grace" };
	public static readonly string[] AllyJobs = { "frontend", "backend", "devops" };
	public static int[] AllyHp = { 70, 40, 60 };

	public static int PlayerMaxHp = 50;
	public static int PlayerHp = 50;
	public static int Atk = 12;
	public static int Level = 1;
	public static int Xp = 0;
	public static bool SlimeDefeated = false;

	public static List<string> UnlockedJobs = new() { "freelancer" };
	public static string CurrentJob = "freelancer";
	public static HashSet<string> CollectedCrystals = new();

	// Encuentro actual: id de enemigo y si es el slime fijo del mapa.
	public static string SelectedEnemy = "slime";
	public static bool FixedEncounter = false;

	// Viajes: spawn al cambiar de escena, flag de mazmorra, gracia y wins.
	public static Vector2? SpawnPoint;
	public static bool InDungeon = false;
	public static int CalmSteps = 60;
	public static int TotalWildWins = 0;

	public static int JobMaxHp(string jobId) =>
		(Level == 2 ? 60 : 50) + Jobs[jobId].HpMod;

	public static int JobAtk(string jobId) =>
		(Level == 2 ? 14 : 12) + Jobs[jobId].AtkMod;

	public static int AllyMaxHp(int i) => JobMaxHp(AllyJobs[i]);
	public static int AllyAtk(int i) => JobAtk(AllyJobs[i]);

	public static bool AllDead() =>
		PlayerHp <= 0 && AllyHp.All(hp => hp <= 0);

	public static void HealParty()
	{
		PlayerHp = PlayerMaxHp;
		for (int i = 0; i < AllyHp.Length; i++)
		{
			AllyHp[i] = AllyMaxHp(i);
		}
	}

	public static void ResetHp() => HealParty();

	public static void ApplyJob(string id)
	{
		if (!Jobs.ContainsKey(id) || !UnlockedJobs.Contains(id))
		{
			return;
		}
		CurrentJob = id;
		RecomputeStats();
	}

	public static void CycleJob()
	{
		int i = UnlockedJobs.IndexOf(CurrentJob);
		ApplyJob(UnlockedJobs[(i + 1) % UnlockedJobs.Count]);
	}

	/// <summary>Desbloquea (si es nuevo), equipa y cura full party. True si era nuevo.</summary>
	public static bool UnlockJob(string id)
	{
		if (!Jobs.ContainsKey(id))
		{
			return false;
		}
		bool isNew = !UnlockedJobs.Contains(id);
		if (isNew)
		{
			UnlockedJobs.Add(id);
		}
		CurrentJob = id;
		RecomputeStats();
		HealParty();
		return isNew;
	}

	/// <summary>Registra una victoria. Devuelve mensaje extra (level up) o vacío.</summary>
	public static string RegisterVictory(int xp, bool fixedSlime)
	{
		if (fixedSlime)
		{
			SlimeDefeated = true;
		}
		else
		{
			TotalWildWins++;
		}
		Xp += xp;
		CalmSteps = 120;
		if (Level == 1 && Xp >= 20)
		{
			Level = 2;
			RecomputeStats();
			HealParty();
			return " ¡La party subió a nivel 2! (HP restaurado)";
		}
		return "";
	}

	static void RecomputeStats()
	{
		PlayerMaxHp = JobMaxHp(CurrentJob);
		Atk = JobAtk(CurrentJob);
		PlayerHp = Mathf.Min(PlayerHp, PlayerMaxHp);
		for (int i = 0; i < AllyHp.Length; i++)
		{
			AllyHp[i] = Mathf.Min(AllyHp[i], AllyMaxHp(i));
		}
	}
}

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

	public struct EnemyDef
	{
		public string Name;
		public int MaxHp;
		public int AtkMin;
		public int AtkMax;
		public int Xp;
		public Color Tint;
		public float Scale;
	}

	public static readonly Dictionary<string, EnemyDef> Enemies = new()
	{
		["slime"] = new EnemyDef
		{
			Name = "Slime-Bug", MaxHp = 30, AtkMin = 6, AtkMax = 10, Xp = 20,
			Tint = new Color(0.35f, 0.85f, 0.40f), Scale = 1.0f,
		},
		["nullrat"] = new EnemyDef
		{
			Name = "NullRat", MaxHp = 22, AtkMin = 5, AtkMax = 8, Xp = 12,
			Tint = new Color(0.60f, 0.58f, 0.72f), Scale = 0.8f,
		},
		["wraith"] = new EnemyDef
		{
			Name = "Legacy Wraith", MaxHp = 44, AtkMin = 8, AtkMax = 13, Xp = 30,
			Tint = new Color(0.70f, 0.20f, 0.30f), Scale = 1.1f,
		},
		["wraithlord"] = new EnemyDef
		{
			Name = "Wraithlord Legacy", MaxHp = 130, AtkMin = 12, AtkMax = 18, Xp = 100,
			Tint = new Color(0.45f, 0.08f, 0.12f), Scale = 1.4f,
		},
		["demonking"] = new EnemyDef
		{
			Name = "Rey Demonio", MaxHp = 180, AtkMin = 15, AtkMax = 22, Xp = 250,
			Tint = new Color(0.15f, 0.05f, 0.25f), Scale = 1.6f,
		},
	["golem"] = new EnemyDef
		{
			Name = "Stack Golem", MaxHp = 60, AtkMin = 10, AtkMax = 14, Xp = 40,
			Tint = new Color(0.45f, 0.50f, 0.60f), Scale = 1.25f,
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
	public static bool BossDefeated = false;
	public static bool DemonKingDefeated = false;

	public static List<string> UnlockedJobs = new() { "freelancer" };
	public static string CurrentJob = "freelancer";
	public static HashSet<string> CollectedCrystals = new();

	// Encuentro actual: id de enemigo y clase ("wild"|"slime"|"boss"|"demonking").
	public static string SelectedEnemy = "slime";
	public static string EncounterKind = "wild";

	/// <summary>Compat: los smokes viejos lo setean en false (= "wild").</summary>
	[System.Obsolete("Usar EncounterKind.")]
	public static bool FixedEncounter
	{
		get => EncounterKind != "wild";
		set { if (!value)
		{
			EncounterKind = "wild";
		}
		else if (EncounterKind == "wild")
		{
			EncounterKind = "slime";
		} }
	}

	// Viajes: spawn al cambiar de escena, flag de mazmorra, gracia y wins.
	public static Vector2? SpawnPoint;
	public static bool InDungeon = false;
	public static int AreaId = 0;
	public static int CalmSteps = 60;
	public static int TotalWildWins = 0;

	public static int JobMaxHp(string jobId) =>
		(50 + (Level - 1) * 10) + Jobs[jobId].HpMod;

	public static int JobAtk(string jobId) =>
		(12 + (Level - 1) * 2) + Jobs[jobId].AtkMod;

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
	public static string RegisterVictory(int xp) => RegisterVictory(xp, EncounterKind);

	/// <summary>Variante con clase explícita (smokes): no toca EncounterKind.</summary>
	public static string RegisterVictory(int xp, string kind)
	{
		if (kind == "slime")
		{
			SlimeDefeated = true;
		}
		else if (kind == "boss")
		{
			BossDefeated = true;
		}
		else if (kind == "wild")
		{
			TotalWildWins++;
		}
		// demonking: nada acá, lo marca Battle al mostrar el final.
		Xp += xp;
		CalmSteps = 120;
		int newLevel = 1;
		foreach (var (threshold, level) in LevelThresholds)
		{
			if (Xp >= threshold)
			{
				newLevel = level;
			}
		}
		if (newLevel > Level)
		{
			Level = newLevel;
			RecomputeStats();
			HealParty();
			return $" ¡La party subió a nivel {Level}! (HP restaurado)";
		}
		return "";
	}

	static readonly (int Xp, int Level)[] LevelThresholds = { (20, 2), (100, 3) };

	/// <summary>Restaura TODA la run a su estado inicial (nueva partida).</summary>
	public static void ResetRun()
	{
		Level = 1;
		Xp = 0;
		SlimeDefeated = false;
		BossDefeated = false;
		DemonKingDefeated = false;
		UnlockedJobs = new() { "freelancer" };
		CurrentJob = "freelancer";
		CollectedCrystals = new();
		PlayerMaxHp = 50;
		PlayerHp = 50;
		Atk = 12;
		AllyHp = new[] { 70, 40, 60 };
		SelectedEnemy = "slime";
		EncounterKind = "wild";
		SpawnPoint = null;
		InDungeon = false;
		AreaId = 0;
		CalmSteps = 60;
		TotalWildWins = 0;
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

using Godot;
using Array = Godot.Collections.Array;
using Dictionary = Godot.Collections.Dictionary;

/// <summary>
/// Guardado en disco (user://code_isekai_save.json): escribe el estado de
/// la run (nivel, XP, HP, jobs, cristales, flags, área, posición y quests:
/// active_id, progress y done) y lo restaura. Read() no toca el estado si
/// no hay archivo o el JSON es inválido.
/// </summary>
public static class SaveGame
{
	public const string Path = "user://code_isekai_save.json";
	public const int Version = 1;

	public static bool Has() => Godot.FileAccess.FileExists(Path);

	public static void Write(int areaId, Vector2 pos)
	{
		var allyHp = new Array();
		foreach (int hp in GameState.AllyHp)
		{
			allyHp.Add(hp);
		}
		var unlocked = new Array();
		foreach (string id in GameState.UnlockedJobs)
		{
			unlocked.Add(id);
		}
		var doneQuests = new Array();
		foreach (string id in QuestLog.Done)
		{
			doneQuests.Add(id);
		}
		var collected = new Array();
		foreach (string id in GameState.CollectedCrystals)
		{
			collected.Add(id);
		}
		var data = new Dictionary
		{
			["level"] = GameState.Level,
			["xp"] = GameState.Xp,
			["player_hp"] = GameState.PlayerHp,
			["ally_hp"] = allyHp,
			["unlocked"] = unlocked,
			["collected"] = collected,
			["slime"] = GameState.SlimeDefeated,
			["boss"] = GameState.BossDefeated,
			["demon"] = GameState.DemonKingDefeated,
			["current_job"] = GameState.CurrentJob,
			["area"] = areaId,
			["pos_x"] = pos.X,
			["pos_y"] = pos.Y,
			["total_wild"] = GameState.TotalWildWins,
			["version"] = Version,
			["ngplus"] = GameState.NgPlus,
			["active_id"] = QuestLog.ActiveId,
			["progress"] = QuestLog.Progress,
			["done"] = doneQuests,
		};
		using var f = Godot.FileAccess.Open(Path, Godot.FileAccess.ModeFlags.Write);
		if (f != null)
		{
			f.StoreString(Json.Stringify(data));
		}
	}

	public static bool Read()
	{
		if (!Godot.FileAccess.FileExists(Path))
		{
			return false;
		}
		using var f = Godot.FileAccess.Open(Path, Godot.FileAccess.ModeFlags.Read);
		if (f == null)
		{
			return false;
		}
		Variant parsed = Json.ParseString(f.GetAsText());
		if (parsed.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		var d = (Dictionary)parsed;
		string[] required =
		{
			"level", "xp", "player_hp", "ally_hp", "unlocked", "collected",
			"slime", "boss", "demon", "current_job", "area", "pos_x", "pos_y", "total_wild",
		};
		foreach (string k in required)
		{
			if (!d.ContainsKey(k))
			{
				return false;
			}
		}
		try
		{
			int level = StrInt(d["level"]);
			int xp = StrInt(d["xp"]);
			int playerHp = StrInt(d["player_hp"]);
			var allyArr = (Array)d["ally_hp"];
			var unlockedArr = (Array)d["unlocked"];
			var collectedArr = (Array)d["collected"];
			bool slime = StrBool(d["slime"]);
			bool boss = StrBool(d["boss"]);
			bool demon = StrBool(d["demon"]);
			string currentJob = d["current_job"].ToString();
			int area = StrInt(d["area"]);
			float posX = StrFloat(d["pos_x"]);
			float posY = StrFloat(d["pos_y"]);
			int totalWild = StrInt(d["total_wild"]);
			if (!GameState.Jobs.ContainsKey(currentJob))
			{
				return false;
			}

			GameState.Level = level;
			GameState.Xp = xp;
			GameState.UnlockedJobs = new System.Collections.Generic.List<string>();
			foreach (Variant v in unlockedArr)
			{
				string id = v.ToString();
				if (GameState.Jobs.ContainsKey(id) && !GameState.UnlockedJobs.Contains(id))
				{
					GameState.UnlockedJobs.Add(id);
				}
			}
			if (GameState.UnlockedJobs.Count == 0)
			{
				GameState.UnlockedJobs.Add("freelancer");
			}
			GameState.CurrentJob = GameState.UnlockedJobs.Contains(currentJob)
				? currentJob
				: GameState.UnlockedJobs[0];
			GameState.PlayerMaxHp = GameState.JobMaxHp(GameState.CurrentJob);
			GameState.Atk = GameState.JobAtk(GameState.CurrentJob);
			GameState.PlayerHp = Mathf.Min(playerHp, GameState.PlayerMaxHp);
			var allyHp = new int[3];
			for (int i = 0; i < 3; i++)
			{
				int v = i < allyArr.Count ? StrInt(allyArr[i]) : GameState.AllyMaxHp(i);
				allyHp[i] = Mathf.Min(v, GameState.AllyMaxHp(i));
			}
			GameState.AllyHp = allyHp;
			GameState.CollectedCrystals = new System.Collections.Generic.HashSet<string>();
			foreach (Variant v in collectedArr)
			{
				GameState.CollectedCrystals.Add(v.ToString());
			}
			GameState.SlimeDefeated = slime;
			GameState.BossDefeated = boss;
			GameState.DemonKingDefeated = demon;
			GameState.TotalWildWins = totalWild;
			GameState.NgPlus = d.ContainsKey("ngplus") ? StrBool(d["ngplus"]) : false;
			GameState.AreaId = area;
			string questActive = d.ContainsKey("active_id") ? d["active_id"].ToString() : "";
			if (!QuestLog.Quests.ContainsKey(questActive))
			{
				questActive = "";
			}
			int questProgress = d.ContainsKey("progress") ? StrInt(d["progress"]) : 0;
			var questDone = new System.Collections.Generic.HashSet<string>();
			if (d.ContainsKey("done") && d["done"].VariantType == Variant.Type.Array)
			{
				foreach (Variant v in (Array)d["done"])
				{
					string id = v.ToString();
					if (QuestLog.Quests.ContainsKey(id))
					{
						questDone.Add(id);
					}
				}
			}
			if (questActive != "" && questDone.Contains(questActive))
			{
				questActive = "";
			}
			QuestLog.ActiveId = questActive;
			QuestLog.Progress = Mathf.Max(0, questProgress);
			QuestLog.Done = questDone;
			GameState.InDungeon = area > 0;
			GameState.SpawnPoint = new Vector2(posX, posY);
			GameState.CalmSteps = 120;
			QuestLog.RefreshTracker();
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static void Wipe()
	{
		if (Godot.FileAccess.FileExists(Path))
		{
			DirAccess.RemoveAbsolute(Path);
		}
	}

	// OJO: Json.ParseString devuelve todo número como double ("2.0"),
	// así que el int se parsea pasando por double.
	static int StrInt(Variant v) =>
		(int)double.Parse(v.ToString(), System.Globalization.CultureInfo.InvariantCulture);

	static float StrFloat(Variant v) =>
		float.Parse(v.ToString(), System.Globalization.CultureInfo.InvariantCulture);

	static bool StrBool(Variant v) => bool.Parse(v.ToString());
}

using Godot;
using System.Collections.Generic;

/// <summary>
/// Side-quests del Senior Dev (2 encadenadas). Static como GameState.
/// rats → boss. Progreso por kills en Battle.DoVictory vía OnKill.
/// </summary>
public static class QuestLog
{
	public struct QuestDef
	{
		public string Title;
		public string Target;
		public int Need;
		public int RewardXp;
		public string Offer;
		public string Thanks;
	}

	public static readonly Dictionary<string, QuestDef> Quests = new()
	{
		["rats"] = new QuestDef
		{
			Title = "Plaga de NullRats",
			Target = "nullrat",
			Need = 3,
			RewardXp = 40,
			Offer = "Los NullRats rompen todo. ¿Cazás 3?",
			Thanks = "¡Gracias!",
		},
		["boss"] = new QuestDef
		{
			Title = "El Wraithlord",
			Target = "wraithlord",
			Need = 1,
			RewardXp = 80,
			Offer = "El Wraithlord nos tira el server. ¿Lo bajás?",
			Thanks = "¡El server respira!",
		},
	};

	public static string ActiveId = "";
	public static int Progress;
	public static HashSet<string> Done = new();

	public static void Reset()
	{
		ActiveId = "";
		Progress = 0;
		Done = new();
	}

	/// <summary>Id de la quest ofrecida ahora, o "" si no hay oferta.</summary>
	public static string NextOffer()
	{
		if (ActiveId != "")
		{
			return "";
		}
		if (!Done.Contains("rats"))
		{
			return "rats";
		}
		if (Done.Contains("rats") && !Done.Contains("boss"))
		{
			return "boss";
		}
		return "";
	}

	/// <summary>Acepta solo si es la quest ofrecida. True si aceptó.</summary>
	public static bool Accept(string id)
	{
		if (id != NextOffer() || !Quests.ContainsKey(id))
		{
			return false;
		}
		ActiveId = id;
		Progress = 0;
		return true;
	}

	/// <summary>
	/// Registra un kill. Si completa la activa: marca Done, limpia la
	/// activa, da XP + cura full y devuelve el mensaje de quest completa.
	/// Si no, refresca el tracker y devuelve "".
	/// </summary>
	public static string OnKill(string enemyId)
	{
		if (ActiveId == "" || !Quests.TryGetValue(ActiveId, out QuestDef q))
		{
			return "";
		}
		if (enemyId != q.Target)
		{
			return "";
		}
		Progress++;
		if (Progress >= q.Need)
		{
			Done.Add(ActiveId);
			ActiveId = "";
			string levelMsg = GameState.AddXp(q.RewardXp);
			GameState.HealParty();
			RefreshTracker();
			return $" ¡Quest completa: {q.Title} (+{q.RewardXp} XP)!{levelMsg}";
		}
		RefreshTracker();
		return "";
	}

	/// <summary>Actualiza el label del grupo "quest_tracker" según el estado.</summary>
	public static void RefreshTracker()
	{
		Node? node = Engine.GetMainLoop() is SceneTree tree
			? tree.GetFirstNodeInGroup("quest_tracker")
			: null;
		if (node is not Label label)
		{
			return;
		}
		if (ActiveId != "" && Quests.TryGetValue(ActiveId, out QuestDef q))
		{
			label.Text = $"📜 {q.Title}: {Progress}/{q.Need} {q.Target}";
		}
		else if (Done.Contains("rats") && Done.Contains("boss"))
		{
			label.Text = "📜 Todo hecho. ¡Al Rey Demonio!";
		}
		else
		{
			label.Text = "📜 Hablá con el Senior Dev (oeste del spawn)";
		}
	}
}

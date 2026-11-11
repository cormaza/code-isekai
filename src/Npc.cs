using Godot;

/// <summary>
/// Senior Dev: NPC de side-quests en el overworld. Primera E abre la
/// oferta (o el texto final si no hay oferta); segunda E acepta y cierra.
/// El panel vive en el HUD del Main (grupo "dialog_panel").
/// </summary>
public partial class Npc : Node2D
{
	[Export]
	public string DisplayName = "Senior Dev";

	bool _showingOffer;
	bool _eHeld;

	public override void _Ready()
	{
		if (GetNodeOrNull<Sprite2D>("Sprite") is Sprite2D sprite)
		{
			sprite.Texture = PixelArt.Mentor();
			sprite.Scale = new Vector2(2.0f, 2.0f);
		}
		var nameLabel = GetNodeOrNull<Label>("NameLabel");
		if (nameLabel != null)
		{
			nameLabel.Text = DisplayName;
		}
		QuestLog.RefreshTracker();
	}

	public override void _Process(double delta)
	{
		// Flanco de subida sobre el estado presionado (ventana = toda la
		// duración del hold), robusto frente a IsActionJustPressed de 1 frame.
		bool pressedNow = Input.IsActionPressed("interact");
		if (!pressedNow)
		{
			_eHeld = false;
			return;
		}
		if (_eHeld)
		{
			return;
		}
		_eHeld = true;
		HandleInteract();
	}

	void HandleInteract()
	{
		Node2D? player = GetParent().GetNodeOrNull<Node2D>("Player");
		if (player == null)
		{
			return;
		}
		if (Position.DistanceTo(player.Position) >= 56.0f)
		{
			return;
		}
		Node? panelNode = GetTree().GetFirstNodeInGroup("dialog_panel");
		if (panelNode is not PanelContainer panel)
		{
			return;
		}
		var speaker = panel.GetNodeOrNull<Label>("VBox/SpeakerLabel");
		var text = panel.GetNodeOrNull<Label>("VBox/TextLabel");
		var hint = panel.GetNodeOrNull<Label>("VBox/HintLabel");
		if (speaker == null || text == null || hint == null)
		{
			return;
		}
		if (!panel.Visible)
		{
			string offer = QuestLog.NextOffer();
			speaker.Text = DisplayName;
			if (offer != "" && QuestLog.Quests.TryGetValue(offer, out QuestLog.QuestDef q))
			{
				text.Text = q.Offer;
				hint.Text = "E: aceptar";
				_showingOffer = true;
			}
			else if (QuestLog.ActiveId != "" && QuestLog.Quests.TryGetValue(QuestLog.ActiveId, out QuestLog.QuestDef active))
			{
				text.Text = $"Ya tenés activa: {active.Title} ({QuestLog.Progress}/{active.Need} {active.Target}). ¡A cazar!";
				hint.Text = "E: continuar";
				_showingOffer = false;
			}
			else if (QuestLog.ActiveId == "" && QuestLog.NextOffer() == "")
			{
				text.Text = "¡El server respira! Todo hecho. ¡Al Rey Demonio!";
				hint.Text = "E: continuar";
				_showingOffer = false;
			}
			else
			{
				text.Text = "Sin quests por ahora. ¡A codear!";
				hint.Text = "E: continuar";
				_showingOffer = false;
			}
			panel.Visible = true;
			return;
		}
		// Panel abierto: segunda E acepta (si había oferta) y cierra.
		if (_showingOffer)
		{
			string offer = QuestLog.NextOffer();
			if (offer != "" && QuestLog.Accept(offer) && QuestLog.Quests.TryGetValue(offer, out QuestLog.QuestDef q))
			{
				Toast($"¡Quest aceptada: {q.Title}!");
			}
			QuestLog.RefreshTracker();
		}
		_showingOffer = false;
		panel.Visible = false;
	}

	static void Toast(string text)
	{
		Node? toast = Engine.GetMainLoop() is SceneTree tree
			? tree.GetFirstNodeInGroup("toast")
			: null;
		if (toast is not Label label)
		{
			return;
		}
		label.Text = text;
		label.Modulate = new Color(1, 1, 1, 1);
		Tween tw = label.CreateTween();
		tw.TweenInterval(2.5);
		tw.TweenProperty(label, "modulate:a", 0.0, 1.0);
	}
}

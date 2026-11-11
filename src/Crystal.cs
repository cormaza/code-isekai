using Godot;

/// <summary>
/// Cristal de job: al tocarlo desbloquea y equipa el job, cura full y avisa
/// con un toast. Ya recolectados desaparecen (persisten en GameState).
/// job_id y crystal_id se asignan por instancia en la escena del mundo.
/// </summary>
public partial class Crystal : Area2D
{
	[Export]
	public string JobId = "frontend";

	[Export]
	public string CrystalId = "crystal_frontend";

	float _time;
	Sprite2D _gem = null!;
	const float BaseScale = 3.0f;

	public override void _Ready()
	{
		if (GameState.CollectedCrystals.Contains(CrystalId))
		{
			QueueFree();
			return;
		}
		_gem = GetNode<Sprite2D>("Gem");
		if (GameState.Jobs.TryGetValue(JobId, out GameState.JobDef job))
		{
			_gem.Texture = PixelArt.Gem(job.Tint);
		}
		_gem.Scale = new Vector2(BaseScale, BaseScale);
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		if (_gem == null)
		{
			return;
		}
		_time += (float)delta;
		float s = BaseScale * (1.0f + Mathf.Sin(_time * 4.0f) * 0.08f);
		_gem.Scale = new Vector2(s, s);
	}

	void OnBodyEntered(Node2D body)
	{
		if (body is not Player || GameState.CollectedCrystals.Contains(CrystalId))
		{
			return;
		}
		GameState.CollectedCrystals.Add(CrystalId);
		GameState.UnlockJob(JobId);
		GameState.CalmSteps = 120;
		Sfx.Play(this, "pickup");
		Toast($"¡Cristal {JobId}! Job desbloqueado: {GameState.Jobs[JobId].Name} (J: cambiar)");
		QueueFree();
	}

	static void Toast(string text)
	{
		// El label vive en el HUD del Main y sobrevive al cristal.
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

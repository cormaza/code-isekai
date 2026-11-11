using Godot;

/// <summary>
/// Héroe del isekai. Movimiento 4 direcciones estilo d-pad (FF5/SNES):
/// si se presionan dos ejes a la vez, gana el dominante, sin diagonales.
/// Se mueve solo con MoveAndSlide desde _PhysicsProcess.
/// J cambia al siguiente job desbloqueado (tinta el sprite y el HUD).
/// </summary>
public partial class Player : CharacterBody2D
{
	[Export]
	public float Speed = 200.0f;

	Polygon2D _body = null!;

	public override void _Ready()
	{
		_body = GetNode<Polygon2D>("Body");
		RefreshJobVisual();
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("cycle_job"))
		{
			GameState.CycleJob();
			RefreshJobVisual();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		if (dir != Vector2.Zero)
		{
			if (Mathf.Abs(dir.X) >= Mathf.Abs(dir.Y))
			{
				dir = new Vector2(Mathf.Sign(dir.X), 0.0f);
			}
			else
			{
				dir = new Vector2(0.0f, Mathf.Sign(dir.Y));
			}
		}

		Velocity = dir * Speed;
		MoveAndSlide();
	}

	public void RefreshJobVisual()
	{
		if (_body == null || !GameState.Jobs.TryGetValue(GameState.CurrentJob, out GameState.JobDef job))
		{
			return;
		}
		_body.Color = job.Tint;
		Node? label = GetTree().GetFirstNodeInGroup("job_label");
		if (label is Label jobLabel)
		{
			jobLabel.Text = $"{job.Name} — {job.Ability} (J: cambiar)";
		}
	}
}

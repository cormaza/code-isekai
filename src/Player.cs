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

	Sprite2D _body = null!;
	Texture2D[] _texStep = System.Array.Empty<Texture2D>();
	float _stepTimer;

	/// <summary>Frame de caminata actual: 0 = piernas separadas, 1 = juntas.</summary>
	public int StepIndex { get; private set; }

	public override void _Ready()
	{
		_body = GetNode<Sprite2D>("Body");
		_body.Scale = new Vector2(2.0f, 2.0f);
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
		if (dir != Vector2.Zero)
		{
			_stepTimer += (float)delta;
			if (_stepTimer >= 0.18f)
			{
				_stepTimer -= 0.18f;
				StepIndex = 1 - StepIndex;
				if (_texStep.Length == 2)
				{
					_body.Texture = _texStep[StepIndex];
				}
			}
		}
		else
		{
			_stepTimer = 0.0f;
			if (StepIndex != 0)
			{
				StepIndex = 0;
				if (_texStep.Length == 2)
				{
					_body.Texture = _texStep[0];
				}
			}
		}
	}

	public void RefreshJobVisual()
	{
		if (_body == null || !GameState.Jobs.TryGetValue(GameState.CurrentJob, out GameState.JobDef job))
		{
			return;
		}
		_texStep = new Texture2D[] { PixelArt.Hero(job.Tint, false), PixelArt.Hero(job.Tint, true) };
		StepIndex = 0;
		_stepTimer = 0.0f;
		_body.Texture = _texStep[0];
		Node? label = GetTree().GetFirstNodeInGroup("job_label");
		if (label is Label jobLabel)
		{
			jobLabel.Text = $"{job.Name} — {job.Ability} (J: cambiar)";
		}
	}
}

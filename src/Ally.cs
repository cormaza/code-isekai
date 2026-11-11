using Godot;

/// <summary>
/// Aliado de la party: sigue al líder en fila india usando el trail que
/// registra World (así dobla las esquinas sin atravesar muros).
/// JobIndex 0..2 → Tux/Ada/Grace (jobs fijos).
/// </summary>
public partial class Ally : Node2D
{
	[Export]
	public int JobIndex = 0;

	Sprite2D _body = null!;
	Texture2D[] _texStep = System.Array.Empty<Texture2D>();
	float _stepTimer;

	/// <summary>Frame de caminata actual: 0 = piernas separadas, 1 = juntas.</summary>
	public int StepIndex { get; private set; }

	public override void _Ready()
	{
		if (JobIndex < 0 || JobIndex >= GameState.AllyJobs.Length)
		{
			JobIndex = 0;
		}
		string jobId = GameState.AllyJobs[JobIndex];
		_body = GetNode<Sprite2D>("Body");
		_body.Scale = new Vector2(2.0f, 2.0f);
		if (GameState.Jobs.TryGetValue(jobId, out GameState.JobDef job))
		{
			_texStep = new Texture2D[] { PixelArt.Hero(job.Tint, false), PixelArt.Hero(job.Tint, true) };
			_body.Texture = _texStep[0];
		}
		StepIndex = 0;
		_stepTimer = 0.0f;
		GetNode<Label>("NameLabel").Text = GameState.AllyNames[JobIndex];
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GetParent() is not World world)
		{
			return;
		}
		Vector2 target = world.GetTrailPoint(JobIndex);
		Vector2 d = target - Position;
		if (d.Length() > 4.0f)
		{
			Position += d.Normalized() * Mathf.Min(200.0f * (float)delta, d.Length());
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
}

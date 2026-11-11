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

	public override void _Ready()
	{
		if (JobIndex < 0 || JobIndex >= GameState.AllyJobs.Length)
		{
			JobIndex = 0;
		}
		string jobId = GameState.AllyJobs[JobIndex];
		if (GameState.Jobs.TryGetValue(jobId, out GameState.JobDef job))
		{
			GetNode<Polygon2D>("Body").Color = job.Tint;
		}
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
		}
	}
}

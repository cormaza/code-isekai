using Godot;

/// <summary>
/// Slime-Bug salvaje del camino norte. Flota en el lugar; si el Player lo
/// toca, dispara la batalla por turnos (una sola vez).
/// </summary>
public partial class SlimeBug : Area2D
{
	float _time;
	float _baseY;
	bool _triggered;

	public override void _Ready()
	{
		_baseY = Position.Y;
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;
		Position = new Vector2(Position.X, _baseY + Mathf.Sin(_time * 3.0f) * 4.0f);
	}

	void OnBodyEntered(Node2D body)
	{
		if (_triggered || GameState.SlimeDefeated)
		{
			return;
		}
		if (body is Player)
		{
			_triggered = true;
			GameState.SelectedEnemy = "slime";
			GameState.FixedEncounter = true;
			GameState.SpawnPoint = body.Position;
			// Diferido: cambiar de escena dentro del callback físico
			// (body_entered) no está permitido.
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/battle/Battle.tscn");
		}
	}
}

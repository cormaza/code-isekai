using Godot;

/// <summary>
/// Slime-Bug salvaje del camino norte (y boss de la mazmorra / Guardián
/// de la cripta según EnemyId). Flota en el lugar; si el Player lo toca,
/// dispara la batalla por turnos (una sola vez). Tinte y escala salen de
/// GameState.Enemies[EnemyId]. Kind: "boss" para wraithlord/boss,
/// "slime" para slime y "wild" para el resto (keeper: XP + TotalWildWins).
/// </summary>
public partial class SlimeBug : Area2D
{
	[Export]
	public string EnemyId = "slime";

	float _time;
	float _baseY;
	bool _triggered;
	Sprite2D? _sprite;
	Vector2 _baseScale = Vector2.One;

	public override void _Ready()
	{
		_baseY = Position.Y;
		if (!GameState.Enemies.TryGetValue(EnemyId, out GameState.EnemyDef def))
		{
			def = GameState.Enemies["slime"];
		}
		if (GetNodeOrNull<Sprite2D>("Sprite") is Sprite2D sprite)
		{
			sprite.Texture = PixelArt.Enemy(EnemyId, def.Tint);
			sprite.Modulate = def.Tint;
			sprite.Scale = new Vector2(def.Scale * 3.0f, def.Scale * 3.0f);
			_sprite = sprite;
			_baseScale = sprite.Scale;
		}
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;
		Position = new Vector2(Position.X, _baseY + Mathf.Sin(_time * 3.0f) * 4.0f);
		if (_sprite != null)
		{
			// Squash-and-stretch: solo visual (el Area2D/colisión no se toca).
			float s = Mathf.Sin(_time * 3.0f);
			_sprite.Scale = _baseScale * new Vector2(1.0f + 0.08f * s, 1.0f - 0.08f * s);
		}
	}

	void OnBodyEntered(Node2D body)
	{
		if (_triggered)
		{
			return;
		}
		if (EnemyId == "slime" && GameState.SlimeDefeated)
		{
			return;
		}
		if (IsBoss() && GameState.BossDefeated)
		{
			return;
		}
		if (body is Player)
		{
			_triggered = true;
			GameState.SelectedEnemy = EnemyId;
			GameState.EncounterKind = IsBoss() ? "boss" : (EnemyId == "slime" ? "slime" : "wild");
			GameState.SpawnPoint = body.Position;
			// Diferido: cambiar de escena dentro del callback físico
			// (body_entered) no está permitido.
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/battle/Battle.tscn");
		}
	}

	bool IsBoss() => EnemyId == "wraithlord" || EnemyId == "boss";
}

using Godot;

/// <summary>
/// Portal entre mapas: al tocarlo setea spawn, flag de mazmorra y cambia
/// de escena (diferido, porque body_entered es callback físico).
/// </summary>
public partial class Portal : Area2D
{
	[Export]
	public string TargetScene = "res://scenes/world/Dungeon.tscn";

	[Export]
	public Vector2 Spawn = new(656, 624);

	[Export]
	public bool GoDungeon = true;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	void OnBodyEntered(Node2D body)
	{
		if (body is not Player)
		{
			return;
		}
		GameState.InDungeon = GoDungeon;
		GameState.SpawnPoint = Spawn;
		GameState.CalmSteps = 120;
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, TargetScene);
	}
}

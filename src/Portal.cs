using Godot;

/// <summary>
/// Portal entre mapas: al tocarlo setea spawn, flag de mazmorra y cambia
/// de escena (diferido, porque body_entered es callback físico).
/// Con BattleInstead dispara una batalla (Rey Demonio) en vez de viajar;
/// con RequiredBoss el viaje/batalla exige al Wraithlord derrotado.
/// </summary>
public partial class Portal : Area2D
{
	[Export]
	public string TargetScene = "res://scenes/world/Dungeon.tscn";

	[Export]
	public Vector2 Spawn = new(656, 624);

	[Export]
	public bool GoDungeon = true;

	[Export]
	public int AreaId = 1;

	[Export]
	public bool BattleInstead = false;

	[Export]
	public string BattleEnemy = "demonking";

	[Export]
	public bool RequiredBoss = false;

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
		if (RequiredBoss && !GameState.BossDefeated)
		{
			Toast("Sellado: el Wraithlord de la mazmorra lo protege…");
			return;
		}
		if (BattleInstead)
		{
			GameState.SelectedEnemy = BattleEnemy;
			GameState.EncounterKind = BattleEnemy;
			GameState.SpawnPoint = body.Position;
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/battle/Battle.tscn");
			return;
		}
		GameState.AreaId = AreaId;
		GameState.InDungeon = AreaId > 0;
		GameState.SpawnPoint = Spawn;
		GameState.CalmSteps = 120;
		Sfx.Play(this, "portal");
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, TargetScene);
	}

	static void Toast(string text)
	{
		// El label vive en el HUD del Main y sobrevive al portal.
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

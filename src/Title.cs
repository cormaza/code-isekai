using Godot;

/// <summary>
/// Pantalla de título: "CODE ISEKAI" + continuar (si hay save) o nueva partida.
/// Continuar restaura el save y viaja al mapa del área guardada (el SpawnPoint
/// lo setea SaveGame.Read); nueva partida borra el save y resetea la run.
/// </summary>
public partial class Title : Control
{
	const string MainScene = "res://main.tscn";
	const string DungeonScene = "res://scenes/world/Dungeon.tscn";
	const string TowerScene = "res://scenes/world/Tower.tscn";

	Button _continueButton = null!;
	Button _newButton = null!;

	public override void _Ready()
	{
		_continueButton = GetNode<Button>("Center/VBox/ContinueButton");
		_newButton = GetNode<Button>("Center/VBox/NewButton");
		_continueButton.Visible = SaveGame.Has();
		(_continueButton.Visible ? _continueButton : _newButton)
			.CallDeferred(Button.MethodName.GrabFocus);
	}

	public void OnContinuePressed()
	{
		if (!SaveGame.Read())
		{
			return;
		}
		string scene = GameState.AreaId switch
		{
			1 => DungeonScene,
			2 => TowerScene,
			_ => MainScene,
		};
		GetTree().ChangeSceneToFile(scene);
	}

	public void OnNewPressed()
	{
		SaveGame.Wipe();
		GameState.ResetRun();
		GetTree().ChangeSceneToFile(MainScene);
	}
}

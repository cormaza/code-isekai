using Godot;

/// <summary>
/// Pantalla de título: "CODE ISEKAI" + continuar (si hay save) o nueva partida.
/// Continuar restaura el save y viaja al mapa del área guardada (el SpawnPoint
/// lo setea SaveGame.Read); nueva partida borra el save y resetea la run.
/// Música: title_loop.wav en loop (Forward, 0..fin) a -18 dB desde _Ready.
/// Funciona con driver Dummy (headless sin errores).
/// </summary>
public partial class Title : Control
{
	const string MainScene = "res://main.tscn";
	const string DungeonScene = "res://scenes/world/Dungeon.tscn";
	const string TowerScene = "res://scenes/world/Tower.tscn";
	const string CryptScene = "res://scenes/world/Crypt.tscn";
	const string TitleMusic = "res://assets/audio/title_loop.wav";

	Button _continueButton = null!;
	Button _newButton = null!;
	AudioStreamPlayer? _music;

	public override void _Ready()
	{
		_continueButton = GetNode<Button>("Center/VBox/ContinueButton");
		_newButton = GetNode<Button>("Center/VBox/NewButton");
		_continueButton.Visible = SaveGame.Has();
		(_continueButton.Visible ? _continueButton : _newButton)
			.CallDeferred(Button.MethodName.GrabFocus);
		StartMusic();
	}

	/// <summary>Loop del título: carga el WAV, loopea 0..fin y suena a -18 dB.</summary>
	void StartMusic()
	{
		if (_music != null)
		{
			return;
		}
		AudioStreamWav? stream = MusicUtil.LoadLooped(TitleMusic);
		if (stream == null)
		{
			return;
		}
		_music = new AudioStreamPlayer
		{
			Stream = stream,
			VolumeDb = -18.0f,
		};
		AddChild(_music);
		_music.Play();
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
			3 => CryptScene,
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

/// <summary>
/// Carga los WAVs de música/victoria directo del archivo (FileAccess +
/// parseo del header RIFF), sin pasar por el importador del editor: así
/// suenan también en headless/Dummy en un checkout fresco. Espera PCM
/// 16-bit mono (lo que genera tools/make_audio.py); loop 0..fin en modo
/// Forward para los loops. Devuelve null si el archivo falta o es inválido.
/// </summary>
public static class MusicUtil
{
	/// <summary>WAV en loop 0..fin (música de título/batalla).</summary>
	public static AudioStreamWav? LoadLooped(string path)
	{
		AudioStreamWav? stream = LoadWav16Mono(path);
		if (stream == null)
		{
			return null;
		}
		stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
		stream.LoopBegin = 0;
		stream.LoopEnd = stream.Data.Length / 2;
		return stream;
	}

	/// <summary>WAV one-shot (fanfarria de victoria).</summary>
	public static AudioStreamWav? LoadOneShot(string path) => LoadWav16Mono(path);

	static AudioStreamWav? LoadWav16Mono(string path)
	{
		if (!Godot.FileAccess.FileExists(path))
		{
			GD.Print($"Música ausente (sin audio): {path}");
			return null;
		}
		byte[] bytes = Godot.FileAccess.GetFileAsBytes(path);
		if (bytes.Length < 44 || bytes[0] != (byte)'R' || bytes[1] != (byte)'I'
			|| bytes[2] != (byte)'F' || bytes[3] != (byte)'F')
		{
			GD.Print($"Música inválida (sin audio): {path}");
			return null;
		}
		// Recorre los chunks hasta el payload "data" (el fmt puede variar).
		int pos = 12;
		int dataStart = -1;
		int dataLen = 0;
		while (pos + 8 <= bytes.Length)
		{
			string id = System.Text.Encoding.ASCII.GetString(bytes, pos, 4);
			int len = System.BitConverter.ToInt32(bytes, pos + 4);
			if (len < 0 || pos + 8 + len > bytes.Length)
			{
				break;
			}
			if (id == "data")
			{
				dataStart = pos + 8;
				dataLen = len;
				break;
			}
			pos += 8 + len;
		}
		if (dataStart < 0 || dataLen <= 0)
		{
			GD.Print($"Música inválida (sin audio): {path}");
			return null;
		}
		byte[] pcm = new byte[dataLen];
		System.Buffer.BlockCopy(bytes, dataStart, pcm, 0, dataLen);
		return new AudioStreamWav
		{
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = 22050,
			Data = pcm,
		};
	}
}

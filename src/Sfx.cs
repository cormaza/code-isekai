using Godot;

/// <summary>
/// Efectos de sonido procedurales sin assets: tonos sine/square con decay
/// generados en runtime como AudioStreamWav 16-bit a 22050 Hz.
/// Funciona con el driver Dummy (headless): sin errores de audio.
/// </summary>
public static class Sfx
{
	const int Rate = 22050;
	static readonly System.Collections.Generic.Dictionary<string, AudioStreamWav> _cache = new();

	/// <summary>Reproduce un efecto hijo de Root que se libera al terminar.</summary>
	public static void Play(Node node, string name)
	{
		AudioStreamWav? stream = Get(name);
		if (stream == null || node.GetTree() == null)
		{
			return;
		}
		var player = new AudioStreamPlayer
		{
			Stream = stream,
			VolumeDb = -12.0f,
		};
		node.GetTree().Root.AddChild(player);
		player.Finished += player.QueueFree;
		player.Play();
	}

	static AudioStreamWav? Get(string name)
	{
		if (_cache.TryGetValue(name, out AudioStreamWav? cached))
		{
			return cached;
		}
		AudioStreamWav? built = name switch
		{
			"hit" => MakeTones(new (float, float)[] { (180, 0.08f), (90, 0.12f) }, false),
			"victory" => MakeTones(new (float, float)[] { (523, 0.12f), (659, 0.12f), (784, 0.2f) }, false),
			"defeat" => MakeTones(new (float, float)[] { (392, 0.2f), (311, 0.2f), (233, 0.35f) }, false),
			"pickup" => MakeTones(new (float, float)[] { (880, 0.08f), (1320, 0.15f) }, false),
			"ding" => MakeTones(new (float, float)[] { (1046, 0.1f), (1568, 0.18f) }, false),
			"buzz" => MakeTones(new (float, float)[] { (140, 0.25f) }, true),
			"portal" => MakeRamp(200, 1200, 0.3f),
			_ => null,
		};
		if (built != null)
		{
			_cache[name] = built;
		}
		return built;
	}

	static AudioStreamWav MakeTones((float Freq, float Dur)[] tones, bool square)
	{
		var samples = new System.Collections.Generic.List<short>();
		foreach ((float freq, float dur) in tones)
		{
			int n = (int)(dur * Rate);
			for (int i = 0; i < n; i++)
			{
				float t = (float)i / n;
				float s = Mathf.Sin(Mathf.Tau * freq * i / Rate);
				if (square)
				{
					s = Mathf.Sign(s);
				}
				s *= 1.0f - t;
				samples.Add((short)(s * 14000.0f));
			}
		}
		return Pack(samples);
	}

	static AudioStreamWav MakeRamp(float from, float to, float dur)
	{
		int n = (int)(dur * Rate);
		var samples = new System.Collections.Generic.List<short>(n);
		float phase = 0.0f;
		for (int i = 0; i < n; i++)
		{
			float t = (float)i / n;
			float freq = from + (to - from) * t;
			phase += Mathf.Tau * freq / Rate;
			float s = Mathf.Sin(phase) * (1.0f - t);
			samples.Add((short)(s * 14000.0f));
		}
		return Pack(samples);
	}

	static AudioStreamWav Pack(System.Collections.Generic.List<short> samples)
	{
		var data = new byte[samples.Count * 2];
		for (int i = 0; i < samples.Count; i++)
		{
			data[i * 2] = (byte)(samples[i] & 0xFF);
			data[i * 2 + 1] = (byte)((samples[i] >> 8) & 0xFF);
		}
		return new AudioStreamWav
		{
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = Rate,
			Data = data,
		};
	}
}

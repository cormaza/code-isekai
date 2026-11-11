using Godot;
using System.Collections.Generic;

/// <summary>
/// Sprites pixel-art propios generados en código (sin assets externos).
/// Cada factory devuelve un ImageTexture desde un mapa de strings donde
/// cada caracter es un píxel (' ' = transparente) + una paleta.
/// El filtro nearest ya es global (project.godot), así que se ven
/// nítidos escalados x2-x4. Para agregar un sprite: dibujá las filas
/// con caracteres, declará la paleta y exponé una factory.
/// </summary>
public static class PixelArt
{
	/// <summary>
	/// Construye una textura desde filas de caracteres. Todas las filas
	/// se rellenan a la más larga con transparente (robusto a desalineos).
	/// </summary>
	public static ImageTexture Sprite(string[] rows, Dictionary<char, Color> palette)
	{
		int w = 1;
		foreach (string r in rows)
		{
			w = Mathf.Max(w, r.Length);
		}
		var img = Image.Create(w, rows.Length, false, Image.Format.Rgba8);
		img.Fill(new Color(0, 0, 0, 0));
		for (int y = 0; y < rows.Length; y++)
		{
			for (int x = 0; x < rows[y].Length; x++)
			{
				char c = rows[y][x];
				if (c == ' ')
				{
					continue;
				}
				if (palette.TryGetValue(c, out Color col))
				{
					img.SetPixel(x, y, col);
				}
			}
		}
		return ImageTexture.CreateFromImage(img);
	}

	/// <summary>Héroe 14x18: pelo oscuro, cara, ojos, túnica=tint, pantalón azul, botas.
	/// step=false = piernas separadas (frame A, quieto); step=true = piernas
	/// juntas bajo el cuerpo (frame B, paso). Solo cambian las 6 filas de
	/// piernas/pantalón/botas (columnas 5-8 en vez de 3-5/8-10).</summary>
	public static ImageTexture Hero(Color tunic) => Hero(tunic, false);

	public static ImageTexture Hero(Color tunic, bool step)
	{
		var palette = new Dictionary<char, Color>
		{
			['h'] = new Color(0.23f, 0.16f, 0.11f),
			['s'] = new Color(0.94f, 0.78f, 0.59f),
			['e'] = new Color(0.08f, 0.08f, 0.11f),
			['t'] = tunic,
			['k'] = new Color(0.14f, 0.14f, 0.18f),
			['p'] = new Color(0.17f, 0.25f, 0.56f),
			['b'] = new Color(0.29f, 0.18f, 0.09f),
		};
		var rows = new List<string>
		{
			"....hhhhhh....",
			"...hhhhhhhh...",
			"...hhhhhhhh...",
			"...hssssssh...",
			"...hsessehs...",
			"...hssssssh...",
			"....ssssss....",
			".....ssss.....",
			"...tttttttt...",
			"..tttttttttt..",
			"..tttkkkkttt..",
			"...tttttttt...",
		};
		if (!step)
		{
			rows.Add("...ppp..ppp...");
			rows.Add("...ppp..ppp...");
			rows.Add("...ppp..ppp...");
			rows.Add("...bbb..bbb...");
			rows.Add("...bbb..bbb...");
			rows.Add("..bbbb..bbbb..");
		}
		else
		{
			rows.Add(".....pppp.....");
			rows.Add(".....pppp.....");
			rows.Add(".....pppp.....");
			rows.Add(".....bbbb.....");
			rows.Add(".....bbbb.....");
			rows.Add("....bbbbbb....");
		}
		return Sprite(rows.ToArray(), palette);
	}

	/// <summary>Slime 14x12: blob, panza clara, ojos blancos con pupilas.</summary>
	public static ImageTexture Slime(Color tint)
	{
		var palette = new Dictionary<char, Color>
		{
			['b'] = tint.Lerp(Colors.White, 0.7f),
			['v'] = tint.Lerp(Colors.White, 0.88f),
			['w'] = Colors.White,
			['k'] = new Color(0.08f, 0.08f, 0.11f),
		};
		return Sprite(new[]
		{
			".....bbbb.....",
			"...bbbbbbbb...",
			"..bbbbbbbbbb..",
			".bbbbbbbbbbbb.",
			".bbbbbbbbbbbb.",
			".bbwwbbbwwbb..",
			".bbwkwbbbwkwb.",
			".bbbbbbbbbbbb.",
			".bbvvvvvvvvbb.",
			"..bvvvvvvvvb..",
			"...bbbbbbbb...",
			".....bbbb.....",
		}, palette);
	}

	/// <summary>Wraith 12x16: capucha, ojos amarillos brillantes, base rasgada transparente.</summary>
	public static ImageTexture Wraith(Color tint)
	{
		var palette = new Dictionary<char, Color>
		{
			['h'] = tint.Lerp(Colors.White, 0.55f),
			['k'] = new Color(0.05f, 0.05f, 0.09f),
			['y'] = new Color(1.0f, 0.91f, 0.29f),
		};
		return Sprite(new[]
		{
			"...hhhhhh...",
			"..hhhhhhhh..",
			".hhhhhhhhhh.",
			".hhhhhhhhhh.",
			".hhhkkkkhhh.",
			".hhhkyykhhh.",
			".hhhkkkkhhh.",
			"..hhkkkkhh..",
			"..hhhhhhhh..",
			"..hhhhhhhh..",
			"..hhhhhhhh..",
			"..hhh..hhh..",
			"..hh....hh..",
			"..h......h..",
			"..h...h...h..",
			".....h......",
		}, palette);
	}

	/// <summary>Golem 14x14: roca, grietas oscuras, núcleo y ojos cyan.</summary>
	public static ImageTexture Golem(Color tint)
	{
		var palette = new Dictionary<char, Color>
		{
			['r'] = tint.Lerp(Colors.White, 0.45f),
			['d'] = new Color(0.11f, 0.13f, 0.16f),
			['c'] = new Color(0.33f, 0.96f, 1.0f),
		};
		return Sprite(new[]
		{
			"...rrrrrrrr...",
			"..rrrrrrrrrr..",
			"..rrdrrrrdrr..",
			"..rrrrrrrrrr..",
			"..rrccrrccrr..",
			"..rrccrrccrr..",
			"..rrrrrrrrrr..",
			"...rrrrrrrr...",
			"..rrrrrrrrrr..",
			"..rrdrrrrdrr..",
			"..rrrccccrrr..",
			"..rrrccccrrr..",
			"..rrrrrrrrrr..",
			"...rr....rr...",
		}, palette);
	}

	/// <summary>Rey Demonio 16x18: cuerpo oscuro, ojos rojos, cuernos claros.</summary>
	public static ImageTexture DemonKing(Color tint)
	{
		var palette = new Dictionary<char, Color>
		{
			['b'] = tint.Lerp(Colors.White, 0.35f),
			['r'] = new Color(1.0f, 0.16f, 0.16f),
			['h'] = new Color(0.91f, 0.86f, 0.75f),
			['k'] = new Color(0.04f, 0.04f, 0.07f),
			['g'] = new Color(1.0f, 0.81f, 0.29f),
			['w'] = new Color(0.95f, 0.95f, 0.97f),
		};
		return Sprite(new[]
		{
			".hh..........hh.",
			".hhh........hhh.",
			"..hhhbbbbbbhhh..",
			"..hhbbbbbbbbhh..",
			"...bbbbbbbbbb...",
			"...bbrrbbrrbb...",
			"...bbbbbbbbbb...",
			"...bbkkkkkkbb...",
			"...bbbbbbbbbb...",
			"....bbbbbbbb....",
			"....bbbbbbbb....",
			"..bbbbbbbbbbbb..",
			"..bbbggggggbbb..",
			"..bbbbbbbbbbbb..",
			"..bbbbbbbbbbbb..",
			"..bbb......bbb..",
			"..bbb......bbb..",
			"..www......www..",
		}, palette);
	}

	/// <summary>Gema 10x14: cristal con brillo blanco, color=tint.</summary>
	public static ImageTexture Gem(Color tint)
	{
		var palette = new Dictionary<char, Color>
		{
			['g'] = tint,
			['w'] = Colors.White,
		};
		return Sprite(new[]
		{
			"...gggg...",
			"..gggggg..",
			"..gwgggg..",
			"..ggwggg..",
			"...gggg...",
			"...gggg...",
			"...gggg...",
			"...gggg...",
			"...gggg...",
			"....ggg...",
			"....ggg...",
			".....gg....",
			".....gg....",
			".....g.....",
		}, palette);
	}

	/// <summary>Mentor 12x18: Senior Dev de túnica gris, barba blanca y báculo dorado.</summary>
	public static ImageTexture Mentor()
	{
		var palette = new Dictionary<char, Color>
		{
			['r'] = new Color(0.42f, 0.42f, 0.47f),
			['s'] = new Color(0.94f, 0.78f, 0.59f),
			['e'] = new Color(0.08f, 0.08f, 0.11f),
			['w'] = new Color(0.95f, 0.95f, 0.95f),
			['b'] = new Color(0.42f, 0.27f, 0.14f),
			['o'] = new Color(1.0f, 0.81f, 0.29f),
		};
		return Sprite(new[]
		{
			"....rrrr....",
			"...rrrrrr...",
			"..rrrrrrrr..",
			"..rrssssrr..",
			"..rrseesrro.",
			"..rrssssrrb.",
			"..rrwwwwrrb.",
			"..rwwwwwwrb.",
			"..rwwwwwwrb.",
			"..rwwwwwwrb.",
			"..rrwwwwrrb.",
			"..rrrrrrrrb.",
			"..rrrrrrrrb.",
			"..rrrrrrrrb.",
			"..rrrrrrrrb.",
			"..rrrrrrrrb.",
			"..rrrrrrrrb.",
			"...rrrrrr.b.",
		}, palette);
	}

	/// <summary>Serpiente 16x12: cuerpo naranja, colmillos blancos, ojos rojos.</summary>
	public static ImageTexture Segfault()
	{
		var palette = new Dictionary<char, Color>
		{
			['o'] = new Color(0.85f, 0.45f, 0.15f),
			['d'] = new Color(0.45f, 0.22f, 0.07f),
			['w'] = Colors.White,
			['r'] = new Color(1.0f, 0.16f, 0.16f),
			['k'] = new Color(0.08f, 0.05f, 0.03f),
		};
		return Sprite(new[]
		{
			"................",
			"....oooooooo....",
			"...ooddddddoo...",
			"..ooddrrrrddoo..",
			"..oodrrrrrrdoo..",
			"...ooddddddoo...",
			"....oooooooo....",
			"..doooooooooood.",
			".dooooooooooooood",
			".doowoooooowood..",
			"..doooooooooood..",
			"...dddddddddd....",
		}, palette);
	}

	/// <summary>Sprite de enemigo por id (tint de GameState.Enemies).</summary>
	public static ImageTexture Enemy(string enemyId, Color tint) => enemyId switch
	{
		"wraith" or "wraithlord" => Wraith(tint),
		"golem" => Golem(tint),
		"demonking" => DemonKing(tint),
		"segfault" => Segfault(),
		_ => Slime(tint),
	};
}

using Godot;
using System.Collections.Generic;

/// <summary>
/// Mapa top-down: dos TileMapLayer (Ground sin colisión, Walls con
/// colisión) sobre un TileSet generado en runtime — 3 tiles de 32px.
/// Overworld (AreaId 0): pasto + caminos de tierra + bosques + puerta del Rey Demonio.
/// Mazmorra (AreaId 1): paleta oscura, sin caminos, pilares.
/// Torre del Stack (AreaId 2): paleta azulada, sin caminos, pilares propios.
/// Cripta del Null (AreaId 3): paleta verdosa oscura, sin caminos, pilares propios.
/// Registra el trail del Player para que los Ally lo sigan en fila india.
/// </summary>
public partial class World : Node2D
{
	public const int MapWidth = 40;
	public const int MapHeight = 23;
	public const int TileSize = 32;

	static readonly Vector2I GrassCell = new(0, 0);
	static readonly Vector2I DirtCell = new(1, 0);
	static readonly Vector2I WallCell = new(2, 0);

	// Encuentros aleatorios sobre pasto (los caminos de tierra son seguros).
	// Overworld: cada 64px, 15%. Mazmorra: cada 48px, 25%. Torre: cada 48px, 28%.
	// Cripta: cada 48px, 30%.
	const float RollEveryPx = 64.0f;
	const float EncounterChance = 0.15f;
	const float DungeonRollEveryPx = 48.0f;
	const float DungeonEncounterChance = 0.25f;
	const float TowerRollEveryPx = 48.0f;
	const float TowerEncounterChance = 0.28f;
	const float CryptRollEveryPx = 48.0f;
	const float CryptEncounterChance = 0.30f;

	// Trail del líder: índice 0 = posición más nueva. Los aliados leen
	// GetTrailPoint(i) → (i+1)*14 ≈ 112px de separación en fila india.
	readonly List<Vector2> _trail = new();

	Node2D? _player;
	Vector2 _lastPos;
	float _sinceRoll;

	/// <summary>Punto del trail para el aliado i (0..2). Lista vacía → player.</summary>
	public Vector2 GetTrailPoint(int i)
	{
		if (_trail.Count == 0)
		{
			return _player != null ? _player.Position : Vector2.Zero;
		}
		int idx = Mathf.Min((i + 1) * 14, _trail.Count - 1);
		return _trail[idx];
	}

	public override void _Ready()
	{
		// AreaId manda; InDungeon queda como reflejo (los smokes lo assertean).
		GameState.InDungeon = GameState.AreaId > 0;
		int area = GameState.AreaId;
		bool dungeon = area > 0;
		bool tower = area == 2;
		bool crypt = area == 3;
		var tileSet = BuildTileSet(dungeon, tower, crypt);
		var ground = GetNode<TileMapLayer>("Ground");
		var walls = GetNode<TileMapLayer>("Walls");
		ground.TileSet = tileSet;
		walls.TileSet = tileSet;

		int sourceId = tileSet.GetSourceId(0);
		PaintGround(ground, sourceId, dungeon);
		PaintWalls(walls, sourceId, dungeon, tower, crypt);
		if (GameState.SlimeDefeated)
		{
			GetNodeOrNull("SlimeBug")?.QueueFree();
		}
		_player = GetNodeOrNull<Node2D>("Player");
		if (_player != null)
		{
			if (GameState.SpawnPoint.HasValue)
			{
				_player.Position = GameState.SpawnPoint.Value;
				GameState.SpawnPoint = null;
			}
			_lastPos = _player.Position;
			// Pre-llenar: 81 puntos desde spawn+(-80*8,0) hasta spawn.
			_trail.Clear();
			for (int k = 0; k < 81; k++)
			{
				_trail.Add(_player.Position + new Vector2(-8.0f * k, 0.0f));
			}
		}
		string areaName = crypt ? "cripta" : (tower ? "torre" : (dungeon ? "mazmorra" : "overworld"));
		GD.Print($"Mundo listo ({areaName}): {MapWidth}x{MapHeight} tiles de {TileSize}px.");
		// Autoguardado al entrar a cada mapa (área + posición del player).
		if (_player != null)
		{
			SaveGame.Write(area, _player.Position);
		}
	}

	static TileSet BuildTileSet(bool dungeon, bool tower = false, bool crypt = false)
	{
		var tileSet = new TileSet
		{
			TileShape = TileSet.TileShapeEnum.Square,
			TileSize = new Vector2I(TileSize, TileSize),
		};
		tileSet.AddPhysicsLayer();
		tileSet.SetPhysicsLayerCollisionLayer(0, 1);
		tileSet.SetPhysicsLayerCollisionMask(0, 1);

		var atlas = new TileSetAtlasSource
		{
			Texture = MakeTilesTexture(dungeon, tower, crypt),
			TextureRegionSize = new Vector2I(TileSize, TileSize),
		};
		atlas.CreateTile(GrassCell);
		atlas.CreateTile(DirtCell);
		atlas.CreateTile(WallCell);

		// OJO: el source debe pertenecer al TileSet ANTES de tocar TileData,
		// porque la physics layer vive en el TileSet, no en el atlas.
		tileSet.AddSource(atlas);

		TileData wallData = atlas.GetTileData(WallCell, 0);
		wallData.AddCollisionPolygon(0);
		float h = TileSize / 2.0f;
		wallData.SetCollisionPolygonPoints(0, 0, new Vector2[]
		{
			new(-h, -h), new(h, -h), new(h, h), new(-h, h),
		});

		return tileSet;
	}

	static ImageTexture MakeTilesTexture(bool dungeon, bool tower = false, bool crypt = false)
	{
		var img = Image.Create(TileSize * 3, TileSize, false, Image.Format.Rgb8);
		if (crypt)
		{
			PaintTile(img, 0, new Color(0.08f, 0.14f, 0.10f)); // piso cripta
			PaintTile(img, 1, new Color(0.06f, 0.11f, 0.08f)); // tierra (sin uso)
			PaintTile(img, 2, new Color(0.04f, 0.07f, 0.05f)); // muro cripta
		}
		else if (tower)
		{
			PaintTile(img, 0, new Color(0.10f, 0.12f, 0.25f)); // piso torre
			PaintTile(img, 1, new Color(0.08f, 0.10f, 0.20f)); // tierra (sin uso)
			PaintTile(img, 2, new Color(0.05f, 0.06f, 0.12f)); // muro torre
		}
		else if (dungeon)
		{
			PaintTile(img, 0, new Color(0.13f, 0.13f, 0.22f)); // piso mazmorra
			PaintTile(img, 1, new Color(0.10f, 0.10f, 0.18f)); // tierra (sin uso)
			PaintTile(img, 2, new Color(0.08f, 0.08f, 0.14f)); // muro mazmorra
		}
		else
		{
			PaintTile(img, 0, new Color(0.24f, 0.55f, 0.28f)); // pasto
			PaintTile(img, 1, new Color(0.55f, 0.42f, 0.27f)); // tierra
			PaintTile(img, 2, new Color(0.13f, 0.32f, 0.20f)); // muro/bosque
		}
		return ImageTexture.CreateFromImage(img);
	}

	static void PaintTile(Image image, int tileIndex, Color baseColor)
	{
		int x0 = tileIndex * TileSize;
		for (int y = 0; y < TileSize; y++)
		{
			for (int x = 0; x < TileSize; x++)
			{
				// Moteado determinista para que no sea un plano liso.
				float v = ((x * 7 + y * 13 + tileIndex * 5) % 7 == 0) ? -0.04f : 0.03f;
				image.SetPixel(x0 + x, y, baseColor + new Color(v, v, v));
			}
		}
	}

	static void PaintGround(TileMapLayer ground, int sourceId, bool dungeon)
	{
		for (int y = 0; y < MapHeight; y++)
		{
			for (int x = 0; x < MapWidth; x++)
			{
				ground.SetCell(new Vector2I(x, y), sourceId, GrassCell);
			}
		}
		if (dungeon)
		{
			return; // La mazmorra no tiene caminos: todo el piso es peligroso.
		}

		// Camino de tierra en cruz + plaza central.
		for (int x = 1; x < MapWidth - 1; x++)
		{
			ground.SetCell(new Vector2I(x, 10), sourceId, DirtCell);
			ground.SetCell(new Vector2I(x, 11), sourceId, DirtCell);
			ground.SetCell(new Vector2I(x, 12), sourceId, DirtCell);
		}
		for (int y = 1; y < MapHeight - 1; y++)
		{
			ground.SetCell(new Vector2I(19, y), sourceId, DirtCell);
			ground.SetCell(new Vector2I(20, y), sourceId, DirtCell);
		}
	}

	static void PaintWalls(TileMapLayer walls, int sourceId, bool dungeon, bool tower = false, bool crypt = false)
	{
		// Anillo borde del mapa.
		for (int x = 0; x < MapWidth; x++)
		{
			walls.SetCell(new Vector2I(x, 0), sourceId, WallCell);
			walls.SetCell(new Vector2I(x, MapHeight - 1), sourceId, WallCell);
		}
		for (int y = 0; y < MapHeight; y++)
		{
			walls.SetCell(new Vector2I(0, y), sourceId, WallCell);
			walls.SetCell(new Vector2I(MapWidth - 1, y), sourceId, WallCell);
		}

		if (crypt)
		{
			// Cripta del Null: pilares propios (corredores col 19-20, fila 11,
			// spawn (656,624) y salida (656,560) libres). Sin puerta del Rey Demonio.
			Vector2I[] pillars =
			{
				new(5, 5), new(11, 7), new(26, 5), new(31, 7), new(8, 14),
				new(14, 18), new(26, 15), new(33, 17), new(5, 19),
				new(17, 12),
			};
			foreach (Vector2I cell in pillars)
			{
				walls.SetCell(cell, sourceId, WallCell);
			}
			return;
		}
		if (tower)
		{
			// Torre del Stack: pilares propios (corredores col 19-20, fila 11,
			// spawn (656,624) y salida libres). Sin puerta del Rey Demonio.
		Vector2I[] pillars =
			{
				new(5, 4), new(11, 6), new(26, 4), new(31, 6), new(8, 13),
				new(14, 17), new(26, 14), new(33, 16), new(5, 18),
				new(17, 10), new(22, 16),
			};
			foreach (Vector2I cell in pillars)
			{
				walls.SetCell(cell, sourceId, WallCell);
			}
			return;
		}
		if (dungeon)
		{
			// Pilares sueltos (1 celda) + anillo. Sin puerta del Rey Demonio.
			Vector2I[] pillars =
			{
				new(5, 4), new(11, 6), new(26, 4), new(31, 6), new(8, 13),
				new(14, 17), new(26, 14), new(33, 16), new(5, 18),
			};
			foreach (Vector2I cell in pillars)
			{
				walls.SetCell(cell, sourceId, WallCell);
			}
			return;
		}

		// Bosques (bloques 2x2) fuera del camino y del spawn central.
		Vector2I[] forests =
		{
			new(5, 4), new(11, 15), new(26, 5), new(31, 14), new(8, 8), new(33, 8),
		};
		foreach (Vector2I origin in forests)
		{
			walls.SetCell(origin, sourceId, WallCell);
			walls.SetCell(origin + new Vector2I(1, 0), sourceId, WallCell);
			walls.SetCell(origin + new Vector2I(0, 1), sourceId, WallCell);
			walls.SetCell(origin + new Vector2I(1, 1), sourceId, WallCell);
		}

		// Puerta del Rey Demonio al norte (placeholder visual).
		walls.SetCell(new Vector2I(19, 1), sourceId, WallCell);
		walls.SetCell(new Vector2I(20, 1), sourceId, WallCell);
	}

	// Proceso físico: registra el trail y acumula pasos sobre pasto.
	public override void _PhysicsProcess(double delta)
	{
		_player ??= GetNodeOrNull<Node2D>("Player");
		if (_player == null)
		{
			return;
		}
		// Trail: si el player se movió >8px del último punto, insertar (cap 400).
		if (_trail.Count == 0 || _player.Position.DistanceTo(_trail[0]) > 8.0f)
		{
			_trail.Insert(0, _player.Position);
			if (_trail.Count > 400)
			{
				_trail.RemoveRange(400, _trail.Count - 400);
			}
		}
		float moved = _lastPos.DistanceTo(_player.Position);
		_lastPos = _player.Position;
		if (moved <= 0.001f || !IsGrass(_player.Position))
		{
			return;
		}
		if (GameState.CalmSteps > 0)
		{
			GameState.CalmSteps -= (int)moved;
			return;
		}
		float rollEvery = GameState.AreaId == 0 ? RollEveryPx : DungeonRollEveryPx;
		float chance = GameState.AreaId switch
		{
			3 => CryptEncounterChance,
			2 => TowerEncounterChance,
			1 => DungeonEncounterChance,
			_ => EncounterChance,
		};
		_sinceRoll += moved;
		while (_sinceRoll >= rollEvery)
		{
			_sinceRoll -= rollEvery;
			if (GD.Randf() < chance)
			{
				TriggerWild();
				return;
			}
		}
	}

	bool IsGrass(Vector2 globalPos)
	{
		var ground = GetNode<TileMapLayer>("Ground");
		Vector2I cell = ground.LocalToMap(ground.ToLocal(globalPos));
		return ground.GetCellAtlasCoords(cell) == GrassCell;
	}

	void TriggerWild()
	{
		if (GameState.AreaId == 3)
		{
			// Cripta: 50% Serpiente Segfault, 30% Legacy Wraith, 20% Wraithlord.
			float r = GD.Randf();
			GameState.SelectedEnemy = r < 0.5f ? "segfault" : (r < 0.8f ? "wraith" : "wraithlord");
		}
		else if (GameState.AreaId == 2)
		{
			// Torre: 50% Stack Golem, 30% Legacy Wraith, 20% Wraithlord.
			float r = GD.Randf();
			GameState.SelectedEnemy = r < 0.5f ? "golem" : (r < 0.8f ? "wraith" : "wraithlord");
		}
		else if (GameState.InDungeon)
		{
			// Mazmorra: 40% NullRat, 25% Slime-Bug salvaje, 35% Legacy Wraith.
			float r = GD.Randf();
			GameState.SelectedEnemy = r < 0.4f ? "nullrat" : (r < 0.65f ? "slime" : "wraith");
		}
		else
		{
			// 65% NullRat, 35% Slime-Bug salvaje (el fijo del mapa es aparte).
			GameState.SelectedEnemy = GD.Randf() < 0.65f ? "nullrat" : "slime";
		}
		GameState.FixedEncounter = false;
		GameState.SpawnPoint = _player.Position;
		// Diferido por si el trigger llegara dentro de un callback físico.
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/battle/Battle.tscn");
	}
}

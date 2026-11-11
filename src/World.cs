using Godot;
using System.Collections.Generic;

/// <summary>
/// Mapa top-down: dos TileMapLayer (Ground sin colisión, Walls con
/// colisión) sobre un TileSet generado en runtime — 3 tiles de 32px.
/// Overworld: pasto + caminos de tierra + bosques + puerta del Rey Demonio.
/// Mazmorra (GameState.InDungeon): paleta oscura, sin caminos, pilares.
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
	// Overworld: cada 64px, 15%. Mazmorra: cada 48px, 25%.
	const float RollEveryPx = 64.0f;
	const float EncounterChance = 0.15f;
	const float DungeonRollEveryPx = 48.0f;
	const float DungeonEncounterChance = 0.25f;

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
		bool dungeon = GameState.InDungeon;
		var tileSet = BuildTileSet(dungeon);
		var ground = GetNode<TileMapLayer>("Ground");
		var walls = GetNode<TileMapLayer>("Walls");
		ground.TileSet = tileSet;
		walls.TileSet = tileSet;

		int sourceId = tileSet.GetSourceId(0);
		PaintGround(ground, sourceId, dungeon);
		PaintWalls(walls, sourceId, dungeon);
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
		GD.Print($"Mundo listo ({(dungeon ? "mazmorra" : "overworld")}): {MapWidth}x{MapHeight} tiles de {TileSize}px.");
	}

	static TileSet BuildTileSet(bool dungeon)
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
			Texture = MakeTilesTexture(dungeon),
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

	static ImageTexture MakeTilesTexture(bool dungeon)
	{
		var img = Image.Create(TileSize * 3, TileSize, false, Image.Format.Rgb8);
		if (dungeon)
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

	static void PaintWalls(TileMapLayer walls, int sourceId, bool dungeon)
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
		float rollEvery = GameState.InDungeon ? DungeonRollEveryPx : RollEveryPx;
		float chance = GameState.InDungeon ? DungeonEncounterChance : EncounterChance;
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
		if (GameState.InDungeon)
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

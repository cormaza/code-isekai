# Code Isekai — Programming RPG

RPG para developers de software que llegan a un isekai de magia y fantasía.
Resuelven retos de desarrollo para subir skills, características y niveles,
al estilo Final Fantasy V (SNES), hasta derrotar al Rey Demonio.

## Requisitos (Linux)

- Godot con .NET: `godot-mono` 4.7.x (este proyecto usa 4.7.2)
- .NET SDK 8+ (probado con 10.0.x)

Verificá:

```bash
godot-mono --version
dotnet --version
```

## Correrlo

```bash
# 1. Compilar C#
dotnet build

# 2a. Jugar directo (headless / CI smoke)
godot-mono --headless --path . --quit-after 60

# 2b. Abrir editor
godot-mono --path . --editor

# 2c. Dentro del editor: botón Build (arriba a la derecha) y Play (F5)
```

> En Godot 4 el "mono" clásico ya no existe: `godot-mono` es el build con .NET.
> No necesitás el comando `mono` instalado.

## Estructura

```text
project.godot          Config (nombre, escena principal, GL Compatibility, filtro nearest,
                     acciones move_* con WASD + flechas)
ProgrammingRpg.csproj  Proyecto C# (Godot.NET.Sdk 4.7.2, net10.0)
main.tscn              Escena raíz (instancia World + HUD)
scenes/world/World.tscn           Mapa (Ground + Walls TileMapLayer, TileSet en runtime)
scenes/world/Dungeon.tscn         Mazmorra norte (paleta oscura, pilares, otro pool)
scenes/party/Ally.tscn            Aliado seguidor (Tux/Ada/Grace, fila india por trail)
scenes/party/Portal.tscn          Portal overworld ↔ mazmorra (spawn + InDungeon)
scenes/player/Player.tscn  Heroe CharacterBody2D 4 direcciones + Camera2D
scenes/enemy/SlimeBug.tscn   Slime-Bug salvaje (Area2D que dispara la batalla)
scenes/battle/Battle.tscn    Pelea por turnos (Atacar/Habilidad/Reto/Job/Huir, HP, XP)
scenes/pickup/Crystal.tscn  Cristal de job (desbloquea + equipa + cura)
src/Main.cs            Script raiz / futuro GameManager
src/World.cs           TileSet + mapa 40x23 + trail de la party + encuentros (overworld/mazmorra)
src/Player.cs          Movimiento d-pad + J (cambia job) + tinta por job
src/GameState.cs       Stats + 4 jobs, desbloqueos, encuentro actual, gracia
src/Crystal.cs         Lógica del cristal (toast vía grupo "toast")
src/SlimeBug.cs        Slime fijo del norte + encuentro al tocar al Player
src/Battle.cs          Turnos de la party vs slime/nullrat/wraith, dano, reto de código, huida, victoria/derrota, level up
src/World.cs           TileSet + mapa 40x23 + encuentros aleatorios en pasto
tests/MovementSmoke.cs Smoke headless: camina y verifica choque contra muro
tests/EncounterSmoke.cs Smoke headless: encuentro fijo, pelea y vuelta al mapa
tests/RandomEncounterSmoke.cs Smoke: batalla vs NullRat + encuentro salvaje en pasto
tests/JobsSmoke.cs     Smoke: Fireball, cristal frontend, ciclo con J
tests/PartySmoke.cs    Smoke: wraith directo, formación en fila india, ida y vuelta a la mazmorra
tests/PuzzleSmoke.cs   Smoke: reto correcto premia (25-35) e incorrecto castiga (contraataque)
icon.svg               Ícono del proyecto
docs/concept.md        Visión del juego (jobs, batalla, progresión)
```

## Verificar movimiento (headless)

```bash
godot-mono --headless --path . --script res://tests/MovementSmoke.cs
# SMOKE PASS: caminó y el muro lo frenó. (exit 0)
```

## Verificar party + mazmorra (headless)

```bash
godot-mono --headless --path . --script res://tests/PartySmoke.cs
# SMOKE PASS: party + mazmorra + wraith OK. (exit 0)
```

## Verificar reto de código (headless)

```bash
godot-mono --headless --path . --script res://tests/PuzzleSmoke.cs
# SMOKE PASS: puzzle premia y castiga. (exit 0)
```

## Concepto corto

- **Fantasía:** dev moderno cae en mundo mágico; la "magia" son sus skills de ingeniería.
- **Progresión FF5:** jobs (Frontend Knight, Backend Mage, DevOps Alchemist…), cristales que desbloquean roles, party de 4 (Tux/Ada/Grace con IA simple).
- **Retos = combate:** bugs, deploys, refactors e incidentes son encuentros; resolverlos da XP y loot.
- **Mazmorra norte:** portal al norte del mapa, paleta oscura y el Legacy Wraith (44 HP, 30 XP) además de NullRat/Slime.
- **Reto = daño/castigo:** el comando 🧩 Reto abre un puzzle de código al azar
  (offbyone, loop, http, strictnull, sql), 1 uso por batalla. Se responde por
  teclado y con Enter; correcta = 25-35 de daño ("¡conocimiento aplicado!");
  incorrecta = el bug contraataca y el turno pasa al enemigo.
- **Rey Demonio:** jefe final = Legacy Monolith / outage total.

> Próximo paso sugerido: boss de la mazmorra y camino al Rey Demonio.

Leé `docs/concept.md` antes de agregar sistemas nuevos.

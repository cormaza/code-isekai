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

# 2a. Jugar directo (headless / CI smoke, 30 frames desde el título)
godot-mono --headless --path . --quit-after 30

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
scenes/ui/Title.tscn              Pantalla de título (CODE ISEKAI, Continuar/Nueva partida)
scenes/world/World.tscn           Mapa (Ground + Walls TileMapLayer, TileSet en runtime)
scenes/world/Dungeon.tscn         Mazmorra norte (paleta oscura, pilares, otro pool)
scenes/world/Tower.tscn           Torre del Stack (paleta azulada, pilares propios, golems)
scenes/party/Ally.tscn            Aliado seguidor (Tux/Ada/Grace, fila india por trail)
scenes/party/Portal.tscn          Portal overworld ↔ mazmorra (spawn + InDungeon)
scenes/player/Player.tscn  Heroe CharacterBody2D 4 direcciones + Camera2D
scenes/enemy/SlimeBug.tscn   Slime-Bug salvaje (Area2D que dispara la batalla)
scenes/npc/Mentor.tscn      Senior Dev (túnica gris + barba) al oeste del spawn (560, 368)
scenes/battle/Battle.tscn    Pelea por turnos (Atacar/Habilidad/Reto/Job/Huir, HP, XP)
scenes/pickup/Crystal.tscn  Cristal de job (desbloquea + equipa + cura)
src/Main.cs            Script raiz / futuro GameManager
src/World.cs           TileSet + mapa 40x23 + trail de la party + encuentros (overworld/mazmorra)
src/Player.cs          Movimiento d-pad + J (cambia job) + tinta por job
src/GameState.cs       Stats + 4 jobs, desbloqueos, encuentro actual, gracia,
                     tabla de enemigos (slime/nullrat/wraith/golem/wraithlord/demonking),
                     AreaId (0 overworld, 1 mazmorra, 2 torre), niveles 1-3,
                     flags de boss/Rey Demonio, ResetRun
src/SaveGame.cs        Guardado en disco (user://code_isekai_save.json): Write/Read/Has/Wipe (incluye quests: active_id, progress, done)
src/Title.cs           Lógica del título (continuar según AreaId, nueva partida)
src/Crystal.cs         Lógica del cristal (toast vía grupo "toast")
src/SlimeBug.cs        Slime fijo del norte + boss de la mazmorra (EnemyId) al tocar al Player
src/Portal.cs          Portal overworld ↔ mazmorra + DemonPortal (batalla final, sellado con boss)
src/Battle.cs          Turnos de la party vs slime/nullrat/wraith/wraithlord/Rey Demonio,
                     dano, reto de código, huida, victoria/derrota, level up,
                     final del juego (EndPanel + nueva partida), SFX y progreso de quests
src/QuestLog.cs        Side-quests del Senior Dev (rats → boss → golems → demonking): oferta, aceptada,
                     progreso por kills, recompensa XP + cura, tracker del HUD
src/Npc.cs             Senior Dev: primera E abre la oferta, segunda E acepta y cierra
src/Sfx.cs             Efectos procedurales sin assets (WAV 16-bit a 22050 Hz, Dummy-safe)
src/PixelArt.cs        Sprites pixel-art propios generados en código (Hero/Slime/Wraith/Golem/DemonKing/Gem/Mentor + Enemy por id), sin assets
src/World.cs           TileSet + mapa 40x23 + encuentros aleatorios en pasto
tests/MovementSmoke.cs Smoke headless: camina y verifica choque contra muro
tests/EncounterSmoke.cs Smoke headless: encuentro fijo, pelea y vuelta al mapa
tests/RandomEncounterSmoke.cs Smoke: batalla vs NullRat + encuentro salvaje en pasto
tests/JobsSmoke.cs     Smoke: Fireball, cristal frontend, ciclo con J
tests/PartySmoke.cs    Smoke: wraith directo, formación en fila india, ida y vuelta a la mazmorra
tests/SaveSmoke.cs     Smoke: save/load (Lv2 + backend + AreaId 1 + spawn) y carga de la torre
tests/PuzzleSmoke.cs   Smoke: reto correcto premia (25-35) e incorrecto castiga (contraataque)
tests/BossSmoke.cs     Smoke: boss de la mazmorra, Rey Demonio y nueva partida
tests/QuestSmoke.cs    Smoke: 2 quests encadenadas + SFX
tests/QuestSaveSmoke.cs Smoke: 4 quests + persistencia en save (golems a medias → ResetRun → Read → cadena completa)
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

## Verificar guardado + torre (headless)

```bash
godot-mono --headless --path . --script res://tests/SaveSmoke.cs
# SMOKE PASS: save/load + torre. (exit 0)
```

## Verificar quests + SFX (headless)

```bash
godot-mono --headless --path . --script res://tests/QuestSmoke.cs
# SMOKE PASS: quests + sfx. (exit 0)
```

## Concepto corto

- **Fantasía:** dev moderno cae en mundo mágico; la "magia" son sus skills de ingeniería.
- **Progresión FF5:** jobs (Frontend Knight, Backend Mage, DevOps Alchemist…), cristales que desbloquean roles, party de 4 (Tux/Ada/Grace con IA simple).
- **Retos = combate:** bugs, deploys, refactors e incidentes son encuentros; resolverlos da XP y loot.
- **Mazmorra norte:** portal al norte del mapa, paleta oscura y el Legacy Wraith (44 HP, 30 XP) además de NullRat/Slime.
- **Boss de la mazmorra:** el Wraithlord Legacy (130 HP, atk 12-18, 100 XP) espera
  al fondo (656, 176). Vencerlo desbloquea el portal del Rey Demonio.
- **Portal sellado:** el DemonPortal del overworld (752, 96) exige al Wraithlord
  derrotado; si no, avisa "Sellado: el Wraithlord de la mazmorra lo protege…"
  y no viaja.
- **Side-quests (Senior Dev):** NPC al oeste del spawn (560, 368, camino
  seguro). Acercate (<56px) y pulsá **E**: la primera E muestra la oferta,
  la segunda la acepta (toast + tracker). Tracker del HUD:
  "📜 {quest}: {progreso}/{necesario} {objetivo}". 2 quests encadenadas:
  **Plaga de NullRats** (cazar 3 nullrats, +40 XP) → **El Wraithlord**
  (bajar 1 wraithlord, +80 XP) → **Torre Estruendosa** (limpiar 2 Stack
  Golems, +60 XP) → **El Rey Demonio** (derrotar 1 demonking, +150 XP);
  cada quest completa cura full a la party. Las quests persisten en el save
  (quest activa + progreso + completadas) y se restauran al continuar.
  Sin oferta muestra el estado (quest activa o "Todo hecho. ¡Al Rey Demonio!").
- **Sprites propios (sin assets):** `src/PixelArt.cs` genera los píxeles en
  código (mapas de strings + paleta, ' ' = transparente, filtro nearest global).
  Héroe con la túnica del job (mapa y batalla, sigue cambios con J y Job en
  batalla), enemigos por id (slime/wraith/golem/demonking, con el tint de
  `GameState.Enemies`; FlashEnemy vuelve al tint, no a blanco), gema del color
  del job (con pulso), Mentor con barba y báculo. Sin polígonos placeholder.
  22050 Hz en runtime (sine con decay; buzz square; portal con rampa
  200→1200 Hz), a -12 dB, hijo de Root que se libera al terminar.
  Funciona con driver Dummy (headless sin errores). Suenan en:
  hit (cada golpe en batalla), victory / defeat, ding (reto correcto) /
  buzz (reto incorrecto), pickup (cristal) y portal (viaje).
- **Rey Demonio:** jefe final = Rey Demonio (180 HP, atk 15-22, 250 XP).
  Vencerlo muestra "¡REY DEMONIO DERROTADO!" (nivel y XP) con
  ↻ Nueva partida (ResetRun: todo a inicial). Huir del Rey Demonio se permite.
- **Nivel 3:** umbrales 20 XP → 2, 100 XP → 3; HP/atk base 50/12 +10/+2 por nivel.
- **Reto = daño/castigo:** el comando 🧩 Reto abre un puzzle de código al azar
  (offbyone, loop, http, strictnull, sql), 1 uso por batalla. Se responde por
  teclado y con Enter; correcta = 25-35 de daño ("¡conocimiento aplicado!");
  incorrecta = el bug contraataca y el turno pasa al enemigo.
- **Rey Demonio:** jefe final = Legacy Monolith / outage total.
- **Título:** la escena principal es `scenes/ui/Title.tscn` (CODE ISEKAI):
  ▶ Continuar solo si hay save (viaja al mapa del área guardada) y
  ✦ Nueva partida (borra el save + ResetRun). Foco inicial en el botón visible.
- **Autoguardado:** al entrar a cada mapa, `World` guarda área + posición del
  player (nivel, XP, HP, jobs, cristales, flags, TotalWildWins). Incluye todo
  lo necesario para continuar donde estabas.
- **Torre del Stack (área 2):** portal en la mazmorra (1008, 624) →
  `scenes/world/Tower.tscn` (spawn 656, 624); salida (656, 560) → mazmorra
  (spawn 1008, 560). Paleta azulada, 11 pilares, pool propio:
  50% Stack Golem (60 HP, atk 10-14, 40 XP) / 30% Legacy Wraith / 20% Wraithlord,
  cada 48px al 28%.
- **Puzzles nuevos (8 total):** recursion → caso base; bigo → o(1); push → push.

> Próximo paso sugerido: animaciones de caminar/atacar + balance + audio real opcional.

Leé `docs/concept.md` antes de agregar sistemas nuevos.

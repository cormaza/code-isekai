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
scenes/world/World.tscn           Mapa (Ground + Walls TileMapLayer, TileSet en runtime) + Mili la Clériga (464, 368, cura full con E)
scenes/world/Dungeon.tscn         Mazmorra norte (paleta oscura, pilares, otro pool)
scenes/world/Tower.tscn           Torre del Stack (paleta azulada, pilares propios, golems)
scenes/world/Crypt.tscn         Cripta del Null (paleta verdosa oscura, pilares propios, segfaults) + Junior Dev (560, 560) + Guardián del Null (656, 176)
scenes/party/Ally.tscn            Aliado seguidor (Tux/Ada/Grace, fila india por trail)
scenes/party/Portal.tscn          Portal overworld ↔ mazmorra (spawn + InDungeon)
scenes/player/Player.tscn  Heroe CharacterBody2D 4 direcciones + Camera2D
scenes/enemy/SlimeBug.tscn   Slime-Bug salvaje (Area2D que dispara la batalla) + boss de mazmorra (wraithlord) y Guardián de la cripta (keeper)
scenes/npc/Mentor.tscn      Senior Dev (túnica gris + barba) al oeste del spawn (560, 368) + variantes Junior (cripta) y Clériga (Mili, cura)
scenes/battle/Battle.tscn    Pelea por turnos (Atacar/Habilidad/Reto/Job/Huir, HP, XP)
scenes/pickup/Crystal.tscn  Cristal de job (desbloquea + equipa + cura)
src/Main.cs            Script raiz / futuro GameManager
src/World.cs           TileSet + mapa 40x23 + trail de la party + encuentros (overworld/mazmorra)
src/Player.cs          Movimiento d-pad + J (cambia job) + tinta por job + caminata 2 frames (0.18s/paso)
src/GameState.cs       Stats + 4 jobs, desbloqueos, encuentro actual, gracia,
                     tabla de enemigos (slime/nullrat/wraith/golem/segfault/wraithlord/keeper/demonking),
                     AreaId (0 overworld, 1 mazmorra, 2 torre, 3 cripta), niveles 1-5,
                     flags de boss/Rey Demonio, ResetRun
src/SaveGame.cs        Guardado en disco (user://code_isekai_save.json): Write/Read/Has/Wipe (incluye quests: active_id, progress, done)
src/Title.cs           Lógica del título (continuar según AreaId, nueva partida, música title_loop)
src/Crystal.cs         Lógica del cristal (toast vía grupo "toast")
src/SlimeBug.cs        Slime fijo del norte + boss de la mazmorra (EnemyId) y Guardián de la cripta (keeper, kind wild) al tocar al Player + squash-and-stretch
src/Portal.cs          Portal overworld ↔ mazmorra + DemonPortal (batalla final, sellado con boss)
src/Battle.cs          Turnos de la party vs slime/nullrat/wraith/wraithlord/Rey Demonio,
                     dano, reto de código, huida, victoria/derrota, level up,
                     final del juego (EndPanel + nueva partida), SFX, música battle_loop/victory,
                     lunge de ataque (tween X 36px) + bob idle (solo Y)
src/QuestLog.cs        Side-quests (rats → boss → golems → demonking → crypt → keeper): oferta, aceptada,
                     progreso por kills, recompensa XP + cura, tracker del HUD
src/Npc.cs             Senior Dev (+ Junior Dev en la cripta + Mili la Clériga que cura en el overworld vía exports DisplayName/SpriteKind/Heals): primera E abre la oferta, segunda E acepta y cierra
src/Sfx.cs             Efectos procedurales sin assets (WAV 16-bit a 22050 Hz, Dummy-safe)
src/PixelArt.cs        Sprites pixel-art propios generados en código (Hero 2 frames/Slime/Wraith/Golem/Segfault/DemonKing/Keeper/Gem/Mentor/Junior/Cleric + Enemy por id), sin assets
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
tests/QuestSaveSmoke.cs Smoke: 6 quests + persistencia en save (golems a medias → ResetRun → Read → cadena completa hasta keeper, NextOffer()=="")
tests/CryptSmoke.cs    Smoke: cripta (AreaId 3) + quests 5-6 (cadena API 1-4 → crypt 1/2 → salvaje en mapa → vuelta a Crypt) + boss Guardián (keeper → Done keeper)
tests/HealerSmoke.cs   Smoke: Mili la Clériga cura a la party (HP 10 → MaxHp con doble E en 464, 368)
tests/AnimSmoke.cs     Smoke: caminar anima 2 frames (Player.StepIndex + Ally0.StepIndex)
tests/AudioSmoke.cs    Smoke: música real en título y batalla + 1 ataque sin ERROR
tools/make_audio.py    Generador chiptune (solo stdlib): battle/title/victory WAVs 22050 Hz mono 16-bit
assets/audio/          battle_loop.wav (24s) + title_loop.wav (16s) + victory.wav (3s)
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

## Verificar cripta + Guardián (headless)

```bash
godot-mono --headless --path . --script res://tests/CryptSmoke.cs
# SMOKE PASS: cripta + boss Guardián. (exit 0)
```

## Verificar clériga sanadora (headless)

```bash
godot-mono --headless --path . --script res://tests/HealerSmoke.cs
# SMOKE PASS: la clériga cura a la party. (exit 0)
```

## Concepto corto

- **Fantasía:** dev moderno cae en mundo mágico; la "magia" son sus skills de ingeniería.
- **Progresión FF5:** jobs (Frontend Knight, Backend Mage, DevOps Alchemist…), cristales que desbloquean roles, party de 4 (Tux/Ada/Grace con IA simple).
- **Retos = combate:** bugs, deploys, refactors e incidentes son encuentros; resolverlos da XP y loot.
- **Mazmorra norte:** portal al norte del mapa, paleta oscura y el Legacy Wraith (55 HP, 30 XP) además de NullRat/Slime.
- **Boss de la mazmorra:** el Wraithlord Legacy (160 HP, atk 14-20, 100 XP) espera
  al fondo (656, 176). Vencerlo desbloquea el portal del Rey Demonio.
- **Boss de la cripta:** el Guardián del Null (140 HP, atk 13-19, 120 XP, escala 1.45)
  aguarda al norte de la cripta (656, 176, corredor col 19-20 fila 5, libre de pilares).
  Es kind "wild" (XP + TotalWildWins, sin flags) y cierra la quest 6.
- **Portal sellado:** el DemonPortal del overworld (752, 96) exige al Wraithlord
  derrotado; si no, avisa "Sellado: el Wraithlord de la mazmorra lo protege…"
  y no viaja.
- **NPCs con sprites propios:** Senior Dev (túnica gris + barba, overworld 560, 368:
  da las 6 quests), Junior Dev (túnica teal sin barba, cripta 560, 560: mismo diálogo
  de quests) y Mili la Clériga (túnica blanca + capucha rosa, overworld 464, 368,
  camino seguro: cada E cura full a la party con toast "¡Party curada! La clériga sonríe."
  y texto "Que el deploy te acompañe.", sin tocar QuestLog). Exports de `Npc.cs`:
  `DisplayName` (default "Senior Dev"), `SpriteKind` ("mentor"/"junior"/"cleric")
  y `Heals` (false). Smoke `tests/HealerSmoke.cs` (seed 12): party herida → doble E
  → HP full → PASS.
- **Side-quests (Senior Dev):** NPC al oeste del spawn (560, 368, camino
  seguro). Acercate (<56px) y pulsá **E**: la primera E muestra la oferta,
  la segunda la acepta (toast + tracker). Tracker del HUD:
  "📜 {quest}: {progreso}/{necesario} {objetivo}". 6 quests encadenadas:
  **Plaga de NullRats** (cazar 3 nullrats, +40 XP) → **El Wraithlord**
  (bajar 1 wraithlord, +80 XP) → **Torre Estruendosa** (limpiar 2 Stack
  Golems, +60 XP) → **El Rey Demonio** (derrotar 1 demonking, +150 XP) →
  **Siseo en la Cripta** (limpiar 2 Serpientes Segfault, +70 XP) →
  **El Guardián del Null** (bajar 1 keeper, +120 XP);
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
- **Balance (modelo de rondas):** salvajes 2-3 rondas, mazmorra 3-4, boss 4-5, final 4-6 (la party pega fuerte; el desafío está en el daño recibido y los usos limitados).
  | Enemigo | HP | ATK | XP |
  |---|---|---|---|
  | NullRat | 26 | 6-9 | 12 |
  | Slime-Bug | 36 | 7-11 | 20 |
  | Legacy Wraith | 55 | 9-14 | 30 |
  | Stack Golem | 75 | 12-16 | 40 |
  | Serpiente Segfault | 85 | 13-17 | 55 |
  | Guardián del Null | 140 | 13-19 | 120 |
  | Wraithlord Legacy | 160 | 14-20 | 100 |
  | Rey Demonio | 300 | 18-25 | 250 |
  Nivel 5 en 500 XP; fórmulas genéricas (Level-1)*10 HP y (Level-1)*2 ATK
  ya escalan (Lv5 = 90 HP/20 ATK base). Tuning: la party de 4 pega ~60-70
  por ronda a Lv1, por eso los HP subieron (slime 30→36, wraithlord 130→160,
  demonking 180→300) y el ATK enemigo amenaza sin one-shotear (máx 25
  vs ~340 HP de party a Lv4, con Hotfix de Grace para emergencias).
- **Música real (WAVs chiptune generados):** `tools/make_audio.py` (solo stdlib:
  wave+struct+math) genera 22050 Hz mono 16-bit: `battle_loop.wav` (24 s,
  La menor, bajo en 8vos + lead cuadrado, 4 compases en loop perfecto),
  `title_loop.wav` (16 s, arpegios Am-F-C-G suaves) y `victory.wav` (3 s,
  fanfarria C-E-G-C + acorde). `Title` loopea title_loop y `Battle`
  battle_loop (LoopMode Forward, puntos 0..fin) a -18 dB desde `_Ready`
  (SFX intactos a -12 dB); al terminar frena el loop y suena victory.wav
  (victoria) o nada extra (derrota, ya tiene el buzz de Sfx). Carga directa
  del archivo sin importador (MusicUtil en `src/Title.cs`): anda con driver
  Dummy. Regenerar: `python3 tools/make_audio.py`.
- **Animaciones (sin assets de imagen):** caminata en 2 frames (`PixelArt.Hero(tint, step)`: step=false piernas
  separadas, step=true juntas bajo el cuerpo; `Player`/`Ally` cachean ambas texturas al cambiar de job
  y alternan `StepIndex` cada 0.18s en movimiento, frame A quietos; sin generar texturas por frame).
  Squash-and-stretch del slime (`SlimeBug._Process`: escala X/Y opuesta con sin, solo visual,
  la colisión intacta). En batalla (`Battle`): lunge de ataque con Tween (atacante ±36px en X,
  0.09s ida + 0.09s vuelta, disparado sin await para no alargar turnos, en paralelo al flash)
  y bob idle (enemigo ±3px, party ±1.5px con fases distintas, solo Y para no pelear con el lunge).
  Smoke `tests/AnimSmoke.cs` (seed 6): caminar anima Player + Ally0.
- **Rey Demonio:** jefe final = Rey Demonio (300 HP, atk 18-25, 250 XP).
  Vencerlo muestra "¡REY DEMONIO DERROTADO!" (nivel y XP + flavor NG+) con
  ↻ Nueva partida (ResetRun: todo a inicial) y ↻ NG+ (nivel y jobs).
  Huir del Rey Demonio se permite.
- **NG+ (post-game):** se desbloquea al vencer al Rey Demonio (EndPanel).
  `GameState.ResetRun(newGamePlus: true)` conserva Level, XP, UnlockedJobs y
  CurrentJob con cura full; resetea bosses, quests, cristales, spawn/área y
  contadores (stats base recalculadas por nivel conservado). `ScaledEnemy(id)`
  escala +30% HP/ATK en NG+ (mismo XP/Tint/Scale; slime 36→46 HP).
  El save lleva versión (`version=1` + `ngplus`; faltantes → 1/false sin fallar).
  Smoke `tests/NgPlusSmoke.cs` (seed 15): NG+ conserva y escala → victoria →
  Main → ResetRun total → slime 36 HP → huida → PASS.
- **Nivel 5:** umbrales 20 XP → 2, 100 XP → 3, 250 XP → 4, 500 XP → 5; HP/atk base 50/12 +10/+2 por nivel.
- **Reto = daño/castigo:** el comando 🧩 Reto abre un puzzle de código al azar
  (offbyone, loop, http, strictnull, sql), 1 uso por batalla. Se responde por
  teclado y con Enter; correcta = 25-35 de daño ("¡conocimiento aplicado!");
  incorrecta = el bug contraataca y el turno pasa al enemigo.
- **Rey Demonio:** jefe final = Legacy Monolith / outage total.
- **Título:** la escena principal es `scenes/ui/Title.tscn` (CODE ISEKAI):
  ▶ Continuar solo si hay save (viaja al mapa del área guardada) y
  ✦ Nueva partida (borra el save + ResetRun). Foco inicial en el botón visible.
- **Autoguardado:** al entrar a cada mapa, `World` guarda área + posición del
  player (nivel, XP, HP, jobs, cristales, flags, TotalWildWins + versión y NG+).
  Incluye todo lo necesario para continuar donde estabas.
- **Torre del Stack (área 2):** portal en la mazmorra (1008, 624) →
  `scenes/world/Tower.tscn` (spawn 656, 624); salida (656, 560) → mazmorra
  (spawn 1008, 560). Paleta azulada, 11 pilares, pool propio:
  50% Stack Golem (75 HP, atk 12-16, 40 XP) / 30% Legacy Wraith / 20% Wraithlord,
  cada 48px al 28%.
- **Cripta del Null (área 3):** portal en la torre (656, 176) →
  `scenes/world/Crypt.tscn` (spawn 656, 624); salida (656, 560) → torre
  (spawn 656, 240). Paleta verdosa oscura, 10 pilares, pool propio:
  50% Serpiente Segfault (85 HP, atk 13-17, 55 XP) / 30% Legacy Wraith /
  20% Wraithlord, cada 48px al 30%. NPC **Junior Dev** (560, 560, túnica teal propia vía
  `SpriteKind="junior"`) junto a la salida: mismo diálogo de quests que el Senior Dev.
  Al norte (656, 176) espera el **Guardián del Null** (140 HP, atk 13-19, 120 XP):
  bajarlo completa la quest 6 (target "keeper") y cierra la cadena (NextOffer()=="").
- **Puzzles nuevos (8 total):** recursion → caso base; bigo → o(1); push → push.

> NG+ post-game + save versionado + pulido final. Próximo paso = "release: export presets Linux/Windows + página de itch.io".

Leé `docs/concept.md` antes de agregar sistemas nuevos.

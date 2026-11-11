# Code Isekai — visión

## Premisa
Sos un developer que despierta en un mundo de magia y fantasía. Acá compilar
es conjurar, deployar es abrir portales y el Rey Demonio es un monolito legacy
que se tragó a los héroes anteriores.

## Encuentros (regla FF)
- Los caminos de tierra son seguros; el pasto es peligroso.
- Overworld: cada 64px sobre pasto, 15% de encuentro (65% NullRat / 35% Slime salvaje).
- Mazmorra: sin caminos (todo el piso es pasto), cada 48px, 25% de encuentro
  (40% NullRat / 25% Slime salvaje / 35% Legacy Wraith).
- Torre del Stack: sin caminos, cada 48px, 28% de encuentro
  (50% Stack Golem / 30% Legacy Wraith / 20% Wraithlord).
- 120px de gracia tras cada batalla (huida, victoria o portal); 60px al cargar el mapa.
- Constantes en `World.cs` (RollEveryPx/EncounterChance + variantes Dungeon/Tower) y tabla de
  enemigos en `GameState.cs` (dict Enemies: slime 36 HP, nullrat 26 HP,
  wraith 55 HP, golem 75 HP atk 12-16 XP 40, wraithlord 160 HP atk 14-20 XP 100,
  demonking 300 HP atk 18-25 XP 250).
- Área actual `GameState.AreaId`: 0 overworld, 1 mazmorra, 2 torre.
  `InDungeon` queda como reflejo (AreaId > 0); los portales setean ambos.
- Clase de encuentro `EncounterKind`: "wild" (aleatorio), "slime" (fijo),
  "boss" (mazmorra), "demonking" (final). Victoria: slime→SlimeDefeated,
  boss→BossDefeated, wild→TotalWildWins, demonking→flag en Battle + EndPanel.
- Niveles: 20 XP → 2, 100 XP → 3, 250 XP → 4; HP/atk base = 50/12 +10/+2 por nivel (+ mod del job).
- Boss: Wraithlord al fondo de la mazmorra (656, 176); su derrota abre el DemonPortal
  del overworld (752, 96), sellado hasta entonces (toast). El Rey Demonio no deja
  volver al mapa: muestra el final con nivel/XP y ↻ Nueva partida (ResetRun).

## Balance (modelo de rondas)
- Objetivo: salvajes 2-3 rondas, mazmorra 3-4, boss 4-5, final 4-6 (la party pega fuerte; el desafío está en el daño recibido y los usos limitados).
  | Enemigo | HP | ATK | XP |
  |---|---|---|---|
  | NullRat | 26 | 6-9 | 12 |
  | Slime-Bug | 36 | 7-11 | 20 |
  | Legacy Wraith | 55 | 9-14 | 30 |
  | Stack Golem | 75 | 12-16 | 40 |
  | Wraithlord Legacy | 160 | 14-20 | 100 |
  | Rey Demonio | 300 | 18-25 | 250 |
- Nivel 4 en 250 XP; fórmulas genéricas (Level-1)*10 HP y (Level-1)*2 ATK
  ya escalan (Lv4 = 80 HP/18 ATK base; mensaje "¡La party subió a nivel N!").
- Tuning: la party de 4 pega ~60-70 por ronda a Lv1, por eso los HP subieron
  (slime 30→36, wraithlord 130→160, demonking 180→300) y el ATK enemigo
  amenaza sin one-shotear (máx 25 vs ~340 HP de party a Lv4, con Hotfix
  de Grace para emergencias). BossSmoke lo guarda: el final dura al menos
  3 turnos "player" en el TurnLog.

## Party de 4
- El player + 3 aliados con jobs fijos: Tux (Frontend Knight), Ada (Backend Mage),
  Grace (DevOps Alchemist). HP base [70, 40, 60], escalan con el nivel como el player.
- En el mapa siguen al líder en fila india: `World` registra un trail (pre-llenado con
  81 puntos, nuevo punto cada >8px, cap 400) y cada Ally lee `GetTrailPoint(i)`.
- En batalla, cola por ronda player → ally0 → ally1 → ally2 → enemy (saltea muertos;
  la victoria se resuelve al vaciar la cola). Aliados con 2 usos c/u: Tux = Shield Bash,
  Ada = Fireball, Grace = Hotfix (35) al miembro vivo con menor fracción de HP si < 0.6.
  El enemigo pega a un miembro aleatorio vivo; derrota solo si cae toda la party.

## Mazmorra
- Portal al norte del overworld (656, 96) → `scenes/world/Dungeon.tscn` (spawn 656, 624);
  portal de salida (656, 560) → `main.tscn` (spawn 656, 132). El Portal setea
  `AreaId`, `InDungeon` (= AreaId > 0), `SpawnPoint` y `CalmSteps = 120`.
- Paleta oscura (piso 0.13/0.13/0.22, muro 0.08/0.08/0.14), sin caminos, pilares sueltos
  en (5,4), (11,6), (26,4), (31,6), (8,13), (14,17), (26,14), (33,16), (5,18)
  (sin puerta del Rey Demonio) + anillo de muros.
- Enemigo propio: Legacy Wraith (55 HP, atk 9-14, 30 XP, tint 0.7/0.2/0.3, escala 1.1).

## Torre del Stack (segunda área)
- Portal en la mazmorra (1008, 624) → `scenes/world/Tower.tscn` (spawn 656, 624);
  portal de salida (656, 560) → mazmorra (spawn 1008, 560, pisable: col 31 fila 17 libre).
- Paleta azulada (piso 0.10/0.12/0.25, muro 0.05/0.06/0.12), sin caminos, 11 pilares
  en (5,4), (11,6), (26,4), (31,6), (8,13), (14,17), (26,14), (33,16), (5,18),
  (17,10), (22,16) (corredores col 19-20, fila 11, spawn y salida libres).
- Enemigo propio: Stack Golem (75 HP, atk 12-16, 40 XP, tint 0.45/0.5/0.6, escala 1.25).

## Guardado + título
- Scena principal: `scenes/ui/Title.tscn` (CODE ISEKAI + subtítulo):
  ▶ Continuar visible solo si hay save (viaja al mapa del área guardada) y
  ✦ Nueva partida (borra el save + ResetRun → overworld). Foco diferido al visible.
- `src/SaveGame.cs` (static, `user://code_isekai_save.json`): Write guarda
  nivel, XP, HP del player, HP de aliados, jobs, cristales, flags
  (slime/boss/demon), job actual, área, posición, TotalWildWins y quests
  (active_id, progress, done: ids completadas);
  Read restaura todo (SpawnPoint, InDungeon = área > 0, AreaId, quests +
  RefreshTracker) o devuelve
  false sin tocar el estado si no hay archivo o el JSON es inválido; Wipe borra.
- Autoguardado: `World._Ready` escribe área + posición del player al entrar
  a cada mapa. Smoke `tests/SaveSmoke.cs`: save/load + carga de la torre.

## Reto de código (comando Reto)
- Botón 🧩 Reto en el menú (entre Job y Huir): abre un panel centrado con una
  pregunta al azar de la tabla `Battle.Puzzles`, deshabilita el menú y da foco
  al campo de respuesta. Cerrar no consume el uso.
- Tabla (pregunta → respuesta): offbyone → 4; loop → 2; http → 200;
  strictnull → false; sql → where; recursion → caso base; bigo → o(1);
  push → push. La comparación normaliza con StripEdges().ToLower().
- 1 uso por batalla (`PuzzleUsesLeft`): correcta = 25-35 de daño por la vía de
  DoOffense ("¡conocimiento aplicado!"); incorrecta = mensaje
  "¡Respuesta incorrecta! El bug contraataca…" y el turno pasa al enemigo
  por la misma vía que tras un ataque.
- Se responde escribiendo + Enter (`text_submitted`) o con el botón
  "Responder (Enter)".

## Side-quests (Senior Dev)
- NPC Mentor en el overworld (560, 368, camino de tierra seguro): ofrece
  2 quests encadenadas con la tecla E (acción "interact"). Primera E abre
  la oferta en el DialogPanel del HUD; segunda E acepta (toast
  "¡Quest aceptada: {título}!"), cierra y actualiza el tracker.
- Tracker: "📜 {título}: {progreso}/{necesario} {objetivo}" con quest
  activa; "📜 Hablá con el Senior Dev (oeste del spawn)" si hay oferta
  pendiente; "📜 Todo hecho. ¡Al Rey Demonio!" cuando NextOffer() == "".
- Quests (`src/QuestLog.cs`, resetea con `GameState.ResetRun`): rats
  ("Plaga de NullRats": 3 nullrats, +40 XP) → boss ("El Wraithlord":
  1 wraithlord, +80 XP) → golems ("Torre Estruendosa": 2 golems,
  +60 XP) → demonking ("El Rey Demonio": 1 demonking, +150 XP). `Battle.DoVictory` reporta el kill y anexa
  "¡Quest completa: {título} (+{xp} XP)!" (+ level up si hay) al Say.
  Completar cura full a la party. Tuning: las recompensas (40/80/60/150 XP)
  aceleran el nivel 2-4 antes del Rey Demonio.
- Smoke `tests/QuestSmoke.cs` (seed 9): doble E → 3×nullrat → doble E →
  wraithlord → PASS. Smoke `tests/QuestSaveSmoke.cs` (seed 4, sin escenas):
  cadena rats → boss → golems (1/2) → Write → ResetRun → Read (activa +
  progreso + done + área + spawn) → golems → demonking → PASS.

## Sprites pixel-art propios (sin assets)
- `src/PixelArt.cs` (static): `Sprite(rows, palette)` construye un
  ImageTexture RGBA8 desde filas de strings (' ' = transparente; las filas
  se rellenan a la más larga) + `Dictionary<char, Color>`. Filtro nearest
  global (project.godot), legibles escalados x2-x4.
- Factories y tamaños: Hero(tint, step) 14x18 (túnica=tint del job;
  step=false piernas separadas = frame A, step=true piernas juntas bajo el
  cuerpo = frame B; solo cambian las 6 filas de piernas), Slime(tint)
  14x12, Wraith(tint) 12x16 (base rasgada transparente), Golem(tint) 14x14
  (núcleo cyan), DemonKing(tint) 16x18, Gem(tint) 10x14, Mentor() 12x18
  (gris + barba + báculo). `Enemy(id, tint)` mapea id → factory
  (wraith/wraithlord → Wraith, golem → Golem, demonking → DemonKing,
  resto → Slime); el tint vivo sale de `GameState.Enemies` vía Modulate.
- Dónde están: la textura se asigna en `_Ready` (Player/Ally Body x2,
  SlimeBug Sprite x3·Scale, Crystal Gem x3 con pulso, Npc Sprite x2,
  Battle EnemySprite x4·Scale + party x2 y cambio de job en RefreshUi).
  FlashEnemy anima modulate a rojo y vuelve al tint del enemigo.
- Animaciones: caminata 2 frames en mapa (`Player`/`Ally`: cachean las 2
  texturas en RefreshJobVisual/`_Ready`, exponen `StepIndex` y alternan cada
  0.18s en movimiento, frame A quietos; sin generar texturas por frame).
  Squash-and-stretch del slime (`SlimeBug._Process`: bob en Y + escala X/Y
  opuesta con sin ±8%, solo visual). Batalla (`Battle`): lunge de ataque con
  Tween (atacante ±36px en X, 0.09s + 0.09s, sin await extra, en paralelo al
  flash; bases guardadas en `_Ready`) y bob idle en `_Process` (enemigo ±3px,
  party ±1.5px con fases distintas, solo Y). Smoke `tests/AnimSmoke.cs`
  (seed 6): caminar 60 frames anima Player + Ally0, soltar hasta 90 → PASS.
- Para agregar un sprite: dibujá las filas con caracteres, declará la
  paleta en una factory nueva y asigná `Sprite2D.Texture` en el `_Ready`.

## SFX procedurales (sin assets)
- `src/Sfx.cs`: tonos generados en runtime como AudioStreamWAV 16-bit a
  22050 Hz (sine con decay (1-t); buzz square con Math.Sign; portal con
  rampa de fase 200→1200 Hz), cache por nombre, AudioStreamPlayer hijo
  de Root (se libera en Finished) a -12 dB. Anda con driver Dummy.
- Tabla: hit [(180,0.08),(90,0.12)]; victory [(523,0.12),(659,0.12),
  (784,0.2)]; defeat [(392,0.2),(311,0.2),(233,0.35)]; pickup
  [(880,0.08),(1320,0.15)]; portal (rampa 0.3s); ding [(1046,0.1),
  (1568,0.18)]; buzz [(140,0.25)] square.
- Dónde suenan: hit en cada golpe (player/aliados/enemigo), victory en
  DoVictory, defeat en DoDefeat, ding/buzz en SubmitPuzzleAnswer
  (correcta/incorrecta), pickup en Crystal, portal en Portal (viaje).

## Música real (WAVs generados)
- `tools/make_audio.py` (solo stdlib: wave+struct+math) genera 22050 Hz
  mono 16-bit: `battle_loop.wav` (24 s, La menor, bajo en 8vos + lead
  cuadrado, patrón de 4 compases a 120 BPM repetido 3x en loop perfecto),
  `title_loop.wav` (16 s, arpegios Am-F-C-G suaves, 2 ciclos) y
  `victory.wav` (3 s, fanfarria C-E-G-C + acorde). Composición legible:
  tabla de notas/frecuencias con comentarios en el script.
- Integración: `Title` loopea title_loop y `Battle` battle_loop
  (LoopMode Forward, puntos 0..fin) a -18 dB desde `_Ready`; al terminar
  (DoVictory/DoDefeat) frena el loop y suena victory.wav (victoria) o
  nada extra (derrota, ya tiene el buzz de Sfx). Volumen SFX intacto (-12 dB).
- Carga directa del archivo sin importador (MusicUtil en `src/Title.cs`:
  FileAccess + parseo del header RIFF): anda con driver Dummy en un
  checkout fresco. Regenerar: `python3 tools/make_audio.py`.
- Smoke `tests/AudioSmoke.cs`: título con player audible + batalla con
  player audible + 1 ataque sin ERROR → PASS.

## Referencia
Final Fantasy V (SNES): vista top-down, party de 4, jobs por cristales,
batalla por turnos con ATB simple, mapa + dungeons + jefes.

## Pilares
1. **Retos = combate.** Cada encuentro es un problema de desarrollo
   (bug, refactor, deploy, incidente, code review). Resolverlo = daño/XP.
2. **Jobs de dev.** Cristales desbloquean roles mezclables:
   Frontend Knight, Backend Mage, DevOps Alchemist, QA Ranger, Data Oracle…
3. **Progresión visible.** Skills, características y niveles suben en la ficha
   y cambian opciones en batalla y en el mapa.

## Alcance del prototipo actual
- Proyecto Godot 4.7 + C# corriendo en Linux.
- Mapa top-down 40x23 caminable (TileMapLayer Ground + Walls, TileSet en runtime).
- Player CharacterBody2D 4 direcciones (WASD/flechas), cámara con límites.
- Slime-Bug en el camino norte: al tocarlo hay batalla por turnos 1v1
  (Atacar/Huir, HP, XP, level up a nivel 2, game over con reintento).
- 4 jobs: Freelancer (Café/cura), Frontend Knight (Shield Bash, tanque),
  Backend Mage (Fireball, cañón de cristal), DevOps Alchemist (Hotfix/cura).
  Cristales en el mapa desbloquean + equipan + curan; J cicla jobs en el mapa
  y el menú Job cambia en batalla (cuesta el turno). Habilidad: 2 usos.
- Victoria: +XP y vuelta al mapa; level up a nivel 2 con 20 XP, a nivel 3 con 100 XP, a nivel 4 con 250 XP.
- Cierre: boss Wraithlord en la mazmorra, portal sellado, Rey Demonio final
  con pantalla de victoria y nueva partida (ResetRun).
- Smokes: mapa, encuentro fijo, aleatorios, jobs y party/mazmorra.
- Party de 4 (aliados con comandos e IA simple) + dungeon. Hecho.

## Próximos pasos sugeridos
1. ~~Mapa top-down caminable.~~ Hecho.
2. ~~Batalla por turnos mínima (1 héroe vs 1 slime-bug).~~ Hecho.
3. ~~Encuentros aleatorios + NullRat.~~ Hecho (pasto peligroso).
4. ~~Jobs con cristales (4 jobs, habilidades, cambio en mapa y batalla).~~ Hecho.
5. ~~Party de 4 (aliados con comandos e IA simple) + dungeon.~~ Hecho.
6. ~~Primer reto real: puzzle de código dentro de la batalla.~~ Hecho
   (comando Reto: 5 puzzles, 1 uso, Enter para responder).
7. ~~Boss de la mazmorra y camino al Rey Demonio.~~ Hecho (Wraithlord 160 HP,
   portal sellado, Rey Demonio 300 HP con final + nueva partida, nivel 4).
8. ~~Guardado en disco + contenido extra (segunda área, más puzzles).~~ Hecho
   (título con continuar/nueva partida, autoguardado al entrar a mapas,
   Torre del Stack + Stack Golem, 3 puzzles nuevos, 8 smokes).
9. Más quests/áreas + sprites propios + balance. Hecho (4 quests con
   persistencia + 7 sprites en código; smokes: 10).
10. ~~Animaciones de caminar/atacar.~~ Hecho (2 frames 0.18s/paso, lunge con
    Tween, squash-and-stretch, bob idle; smoke AnimSmoke; smokes: 11).
11. ~~Balance general + audio real.~~ Hecho (modelo de rondas + nivel 4,
    WAVs chiptune generados por `tools/make_audio.py`, música en título y
    batalla; smokes: 12).
12. quest del Rey Demonio jugable? NO — ya existe; próximo: segunda área con
    quest propia + sprites de NPCs extra.

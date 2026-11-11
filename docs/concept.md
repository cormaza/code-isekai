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
- 120px de gracia tras cada batalla (huida, victoria o portal); 60px al cargar el mapa.
- Constantes en `World.cs` (RollEveryPx/EncounterChance + variantes Dungeon) y tabla de
  enemigos en `Battle.cs` (dict Enemies).

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
  `InDungeon`, `SpawnPoint` y `CalmSteps = 120`.
- Paleta oscura (piso 0.13/0.13/0.22, muro 0.08/0.08/0.14), sin caminos, pilares sueltos
  en (5,4), (11,6), (26,4), (31,6), (8,13), (14,17), (26,14), (33,16), (5,18)
  (sin puerta del Rey Demonio) + anillo de muros.
- Enemigo propio: Legacy Wraith (44 HP, atk 8-13, 30 XP, tint 0.7/0.2/0.3, escala 1.1).

## Reto de código (comando Reto)
- Botón 🧩 Reto en el menú (entre Job y Huir): abre un panel centrado con una
  pregunta al azar de la tabla `Battle.Puzzles`, deshabilita el menú y da foco
  al campo de respuesta. Cerrar no consume el uso.
- Tabla (pregunta → respuesta): offbyone → 4; loop → 2; http → 200;
  strictnull → false; sql → where. La comparación normaliza con
  StripEdges().ToLower().
- 1 uso por batalla (`PuzzleUsesLeft`): correcta = 25-35 de daño por la vía de
  DoOffense ("¡conocimiento aplicado!"); incorrecta = mensaje
  "¡Respuesta incorrecta! El bug contraataca…" y el turno pasa al enemigo
  por la misma vía que tras un ataque.
- Se responde escribiendo + Enter (`text_submitted`) o con el botón
  "Responder (Enter)".

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
- Victoria: +XP y vuelta al mapa; level up a nivel 2 con 20 XP.
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
7. Boss de la mazmorra y camino al Rey Demonio.

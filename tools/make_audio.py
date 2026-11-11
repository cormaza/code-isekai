#!/usr/bin/env python3
"""Genera los WAVs chiptune del juego (solo stdlib: wave + struct + math).

    python3 tools/make_audio.py

Regenera (22050 Hz, mono, 16-bit):
- assets/audio/battle_loop.wav (24 s): La menor, bajo en 8vos + lead
  cuadrado. Patron de 4 compases a 120 BPM (2 s/compas) repetido 3x = 24 s,
  loop perfecto (arranque/cierre en fase con micro-fade de 5 ms).
- assets/audio/title_loop.wav (16 s): arpegios suaves Am-F-C-G
  (4 compases de 2 s por ciclo, 2 ciclos = 16 s), loop perfecto.
- assets/audio/victory.wav (3 s, sin loop): fanfarria C-E-G-C + acorde.

Tabla de notas (Hz, temperamento igual, A4 = 440):
"""
import math
import struct
import wave
from pathlib import Path

RATE = 22050  # Hz, igual que Sfx.cs en runtime

# --- Tabla de notas / frecuencias (Hz) ---
# Octava 2 (bajo):   A2 110.00, C3 130.81, D3 146.83, E3 164.81,
#                    F3 174.61, G3 196.00
# Octava 3 (acordes):A3 220.00, C4 261.63, D4 293.66, E4 329.63,
#                    F4 349.23, G4 392.00
# Octava 4 (lead):   A4 440.00, C5 523.25, D5 587.33, E5 659.25,
#                    F5 698.46, G5 783.99
# Octava 5 (brillo): A5 880.00, C6 1046.50, E6 1318.51, G6 1567.98
A2, C3, D3, E3, F3, G3 = 110.00, 130.81, 146.83, 164.81, 174.61, 196.00
A3, C4, D4, E4, F4, G4 = 220.00, 261.63, 293.66, 329.63, 349.23, 392.00
A4, C5, D5, E5, F5, G5 = 440.00, 523.25, 587.33, 659.25, 698.46, 783.99
A5, C6, E6, G6 = 880.00, 1046.50, 1318.51, 1567.98

OUT = Path(__file__).resolve().parent.parent / "assets" / "audio"


def sine(phase: float) -> float:
    return math.sin(phase)


def square(phase: float) -> float:
    # Cuadrada suavizada (signo + un poco de fundamental para no aliasar).
    return math.copysign(1.0, math.sin(phase)) * 0.7 + math.sin(phase) * 0.3


def triangle(phase: float) -> float:
    return 2.0 * abs(2.0 * (phase / (2 * math.pi) % 1.0) - 1.0) - 1.0


def adsr(i: int, n: int, attack: float = 0.01, release: float = 0.15) -> float:
    """Envolvente 0..1 con ataque rapido y release al final de la nota."""
    if n <= 0:
        return 0.0
    t = i / n
    a = min(1.0, t / attack) if attack > 0 else 1.0
    r = min(1.0, (1.0 - t) / release) if release > 0 else 1.0
    return min(a, r)


def render_note(buf: list, start: int, dur: float, freq: float, amp: float,
                kind: str = "square") -> None:
    """Suma una nota con envolvente ADSR simple al buffer (mezcla aditiva)."""
    n = int(dur * RATE)
    wave_fn = {"square": square, "sine": sine, "triangle": triangle}[kind]
    phase = 0.0
    step = 2 * math.pi * freq / RATE
    for i in range(n):
        idx = start + i
        if 0 <= idx < len(buf):
            buf[idx] += wave_fn(phase) * amp * adsr(i, n)
        phase += step


def normalize(buf: list, peak: float = 0.85) -> None:
    top = max(1e-6, max(abs(v) for v in buf))
    k = peak / top
    for i in range(len(buf)):
        buf[i] *= k


def loop_fade(buf: list, ms: float = 5.0) -> None:
    """Micro-fade al arranque/cierre para loop sin click (fase continua)."""
    n = int(ms / 1000.0 * RATE)
    n = max(1, min(n, len(buf) // 2))
    for i in range(n):
        g = i / n
        buf[i] *= g
        buf[len(buf) - 1 - i] *= g


def write_wav(path: Path, buf: list) -> None:
    data = struct.pack("<%dh" % len(buf),
                       *(max(-32768, min(32767, int(v * 32767))) for v in buf))
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data)
    print(f"OK {path} ({len(buf) / RATE:.1f}s)")


def make_battle() -> list:
    """24 s a 120 BPM: compas = 2.0 s; patron Am Am F E (4 compases),
    repetido 3x. Bajo en 8vos (0.25 s), lead cuadrado por compas."""
    beat = 0.5          # 120 BPM
    bar = 4 * beat      # 2.0 s
    pattern = [         # (bajo, acorde arpegiado, lead del compas)
        ("Am", A2, (A3, C4, E4, A4), (A4, C5, E5, A5)),
        ("Am", A2, (A3, C4, E4, A4), (G5, E5, C5, D5)),
        ("F", F3, (F3, A3, C4, F4), (F5, E5, C5, A4)),
        ("E", E3, (E3, G3, C4, E4), (E5, D5, C5, E5)),
    ]
    total = bar * 4 * 3
    buf = [0.0] * int(total * RATE)
    for rep in range(3):
        for b, (_name, root, arp, lead) in enumerate(pattern):
            base = int((rep * 4 + b) * bar * RATE)
            # Bajo en 8vos: raiz, raiz, quinta, raiz, ... (8 notas/compas).
            fifth = root * 1.5
            bass_line = [root, root, fifth, root, root, fifth, root, fifth]
            for k, f in enumerate(bass_line):
                render_note(buf, base + int(k * beat / 2 * RATE),
                            beat / 2 * 0.95, f, 0.30, "triangle")
            # Acorde arpegiado en 16vos suaves (colchon).
            for k in range(8):
                f = arp[k % len(arp)]
                render_note(buf, base + int(k * beat / 2 * RATE),
                            beat / 2 * 0.9, f, 0.10, "sine")
            # Lead cuadrado: 4 negras por compas.
            for k, f in enumerate(lead):
                render_note(buf, base + int(k * beat * RATE),
                            beat * 0.9, f, 0.22, "square")
    return buf


def make_title() -> list:
    """16 s: arpegios suaves Am-F-C-G, 1 compas (2 s) por acorde, 2 ciclos."""
    beat = 0.5
    bar = 4 * beat
    chords = [
        ("Am", (A3, C4, E4, A4)),
        ("F", (F3, A3, C4, F4)),
        ("C", (C4, E4, G4, C5)),
        ("G", (G3, D4, G4, D5)),
    ]
    total = bar * 4 * 2
    buf = [0.0] * int(total * RATE)
    for rep in range(2):
        for b, (_name, arp) in enumerate(chords):
            base = int((rep * 4 + b) * bar * RATE)
            # Arpegio en 8vos ida y vuelta, sine + un toque de triangle.
            seq = [arp[0], arp[1], arp[2], arp[3],
                   arp[3], arp[2], arp[1], arp[2]]
            for k, f in enumerate(seq):
                render_note(buf, base + int(k * beat / 2 * RATE),
                            beat / 2 * 1.4, f, 0.20, "sine")
                render_note(buf, base + int(k * beat / 2 * RATE),
                            beat / 2 * 1.4, f / 2, 0.08, "triangle")
    return buf


def make_victory() -> list:
    """3 s: fanfarria C-E-G-C (negras) + acorde final C-E-G-C (blanca)."""
    buf = [0.0] * int(3.0 * RATE)
    t = 0.0
    for f in (C5, E5, G5, C6):
        render_note(buf, int(t * RATE), 0.28, f, 0.30, "square")
        render_note(buf, int(t * RATE), 0.28, f / 2, 0.15, "triangle")
        t += 0.32
    # Acorde final: C5 + E5 + G5 + C6, 1.4 s con decay largo.
    for f in (C5, E5, G5, C6):
        render_note(buf, int(t * RATE), 1.4, f, 0.18, "square")
        render_note(buf, int(t * RATE), 1.4, f / 2, 0.10, "sine")
    return buf


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for name, make, loop in (("battle_loop.wav", make_battle, True),
                             ("title_loop.wav", make_title, True),
                             ("victory.wav", make_victory, False)):
        buf = make()
        normalize(buf)
        if loop:
            loop_fade(buf)
        write_wav(OUT / name, buf)


if __name__ == "__main__":
    main()

# Audio Manager paketinin örnek seslerini sentezler (telifsiz, tamamen kodla üretilmiş).
#   python make_sounds.py <çıktı_klasörü>
import math, os, random, struct, sys, wave

RATE = 44100
out_dir = sys.argv[1] if len(sys.argv) > 1 else "."
os.makedirs(out_dir, exist_ok=True)
rng = random.Random(7)


def write(name, samples):
    peak = max(1e-9, max(abs(s) for s in samples))
    gain = 0.89 / peak if peak > 0.89 else 1.0
    with wave.open(os.path.join(out_dir, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s * gain)) * 32767)) for s in samples))
    print("wrote", name, f"{len(samples) / RATE:.2f}s")


def n(seconds):
    return int(seconds * RATE)


def env(i, total, attack=0.005, release=None):
    t = i / RATE
    a = min(1.0, t / attack) if attack > 0 else 1.0
    if release is None:
        return a
    remaining = (total - i) / RATE
    return a * min(1.0, remaining / release)


def lowpass(samples, cutoff):
    # Tek kutuplu alçak geçiren filtre.
    rc = 1.0 / (2 * math.pi * cutoff)
    alpha = (1.0 / RATE) / (rc + 1.0 / RATE)
    out, y = [], 0.0
    for x in samples:
        y += alpha * (x - y)
        out.append(y)
    return out


# ---------------------------------------------------------------- arayüz
def ui_click():
    total = n(0.045)
    return [math.sin(2 * math.pi * 1500 * i / RATE) * math.exp(-i / RATE * 90) * env(i, total, 0.001) for i in range(total)]


def ui_confirm():
    total = n(0.22)
    out = []
    for i in range(total):
        t = i / RATE
        f = 880 if t < 0.08 else 1320
        out.append(0.6 * math.sin(2 * math.pi * f * t) * math.exp(-((t if t < 0.08 else t - 0.08)) * 18) * env(i, total, 0.002, 0.03))
    return out


# ---------------------------------------------------------------- oyun
def coin(f1, f2):
    total = n(0.35)
    out = []
    for i in range(total):
        t = i / RATE
        f = f1 if t < 0.07 else f2
        square = 1.0 if math.sin(2 * math.pi * f * t) > 0 else -1.0
        out.append(0.35 * square * math.exp(-max(0, t - 0.07) * 9) * env(i, total, 0.002, 0.05))
    return lowpass(out, 6000)


def jump():
    total = n(0.25)
    out, phase = [], 0.0
    for i in range(total):
        t = i / RATE
        f = 280 + 600 * (t / 0.25) ** 0.7
        phase += 2 * math.pi * f / RATE
        out.append(0.7 * math.sin(phase) * env(i, total, 0.004, 0.08))
    return out


def impact(thump_freq, noise_amount, length):
    total = n(length)
    noise = lowpass([rng.uniform(-1, 1) for _ in range(total)], 1800)
    out = []
    for i in range(total):
        t = i / RATE
        thump = math.sin(2 * math.pi * thump_freq * t * (1 - t * 1.5)) * math.exp(-t * 28)
        out.append((thump + noise_amount * noise[i] * math.exp(-t * 35)) * env(i, total, 0.001, 0.02))
    return out


def explosion():
    total = n(1.6)
    noise = [rng.uniform(-1, 1) for _ in range(total)]
    rumble = lowpass(lowpass(noise, 300), 300)
    crack = lowpass(noise, 3000)
    out = []
    for i in range(total):
        t = i / RATE
        boom = math.sin(2 * math.pi * 48 * t) * math.exp(-t * 4)
        out.append((1.8 * rumble[i] * math.exp(-t * 2.2) + 0.5 * crack[i] * math.exp(-t * 12) + 0.6 * boom) * env(i, total, 0.003, 0.3))
    return out


def engine_loop():
    # 1 sn, tüm frekanslar tam sayı → kusursuz döngü. Pitch ile hız hissi verilir.
    total = n(1.0)
    out = []
    for i in range(total):
        t = i / RATE
        v = 0.0
        for k, a in ((1, 1.0), (2, 0.55), (3, 0.3), (4, 0.18), (6, 0.08)):
            v += a * math.sin(2 * math.pi * 55 * k * t + k)
        v *= 0.75 + 0.25 * math.sin(2 * math.pi * 27 * t)  # silindir vuruşu
        out.append(v)
    return lowpass(out, 1500)


# ---------------------------------------------------------------- müzik (8 sn döngü)
NOTE = lambda m: 440.0 * 2 ** ((m - 69) / 12)


def music(chords, bpm, arp_pattern, with_kick, bass_octave=-24, name=""):
    beat = 60 / bpm
    total = n(8.0)
    out = [0.0] * total
    chord_len = total // len(chords)
    for c, chord in enumerate(chords):
        start = c * chord_len
        # Pad: yumuşak akor
        for i in range(chord_len):
            t = i / RATE
            e = min(1, t / 0.25) * min(1, (chord_len - i) / RATE / 0.25)
            v = sum(math.sin(2 * math.pi * NOTE(m) * t) for m in chord) / len(chord)
            out[start + i] += 0.25 * v * e
        # Bas
        for i in range(chord_len):
            t = i / RATE
            e = min(1, t / 0.02) * math.exp(-(t % (beat * 2)) * 1.5)
            out[start + i] += 0.3 * math.sin(2 * math.pi * NOTE(chord[0] + bass_octave + 12) * t) * e
        # Arpej
        step = beat / 2
        steps = int(round(chord_len / RATE / step))
        for s in range(steps):
            m = chord[arp_pattern[s % len(arp_pattern)] % len(chord)] + 12
            a = start + int(s * step * RATE)
            for i in range(n(step)):
                if a + i >= total:
                    break
                t = i / RATE
                out[a + i] += 0.16 * math.sin(2 * math.pi * NOTE(m) * t) * math.exp(-t * 6) * min(1, t / 0.005)
        # Davul
        if with_kick:
            for b in range(int(round(chord_len / RATE / beat))):
                a = start + int(b * beat * RATE)
                for i in range(n(0.18)):
                    if a + i >= total:
                        break
                    t = i / RATE
                    out[a + i] += 0.55 * math.sin(2 * math.pi * (110 - 260 * t) * t) * math.exp(-t * 22)
    # Döngü noktasında tık olmasın.
    fade = n(0.01)
    for i in range(fade):
        out[i] *= i / fade
        out[-1 - i] *= i / fade
    return out


write("UI_Click", ui_click())
write("UI_Confirm", ui_confirm())
write("Coin_01", coin(988, 1319))
write("Coin_02", coin(1047, 1397))
write("Jump", jump())
write("Impact_01", impact(90, 0.8, 0.25))
write("Impact_02", impact(75, 1.0, 0.3))
write("Impact_03", impact(110, 0.6, 0.22))
write("Explosion", explosion())
write("Engine_Loop", engine_loop())
# Am - F - C - G (sakin) ve Dm - Bb - F - C (hareketli)
write("Music_Calm", music([[57, 60, 64], [53, 57, 60], [48, 52, 55], [55, 59, 62]], 90, [0, 1, 2, 1], False))
write("Music_Action", music([[50, 53, 57], [46, 50, 53], [53, 57, 60], [48, 52, 55]], 128, [0, 2, 1, 2, 0, 1, 2, 1], True))

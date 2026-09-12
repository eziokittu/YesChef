"""Generate original 44.1 kHz music, ambience, and SFX for Yes Chef.

The synthesis is deterministic and uses only NumPy/SciPy. Re-running this file
recreates the complete audio set without downloading or licensing dependencies.
"""

from pathlib import Path
import math
import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, sosfilt


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Assets" / "Audio" / "Generated"
SR = 44_100
RNG = np.random.default_rng(20260910)


def seconds(length):
    return np.arange(int(length * SR), dtype=np.float64) / SR


def lowpass(signal, cutoff):
    return sosfilt(butter(4, cutoff, btype="lowpass", fs=SR, output="sos"), signal)


def highpass(signal, cutoff):
    return sosfilt(butter(4, cutoff, btype="highpass", fs=SR, output="sos"), signal)


def bandpass(signal, low, high):
    return sosfilt(butter(3, (low, high), btype="bandpass", fs=SR, output="sos"), signal)


def soften(signal, drive=1.25):
    return np.tanh(signal * drive) / np.tanh(drive)


def fade(signal, fade_in=.02, fade_out=.04):
    result = signal.copy()
    a = min(int(fade_in * SR), len(result))
    b = min(int(fade_out * SR), len(result))
    if a: result[:a] *= np.linspace(0, 1, a)
    if b: result[-b:] *= np.linspace(1, 0, b)
    return result


def normalize(signal, peak=.88):
    maximum = float(np.max(np.abs(signal)))
    return signal if maximum < 1e-9 else signal * (peak / maximum)


def write(name, signal, peak=.88):
    OUTPUT.mkdir(parents=True, exist_ok=True)
    rendered = normalize(soften(signal), peak)
    pcm = np.int16(np.clip(rendered, -1, 1) * 32767)
    wavfile.write(OUTPUT / name, SR, pcm)
    channels = 1 if pcm.ndim == 1 else pcm.shape[1]
    print(f"{name}: {len(pcm) / SR:.2f}s, {channels}ch")


def midi(note):
    return 440.0 * 2 ** ((note - 69) / 12)


def piano_note(note, duration, velocity=.7):
    t = seconds(duration)
    f = midi(note)
    tone = np.zeros_like(t)
    for harmonic, amount in enumerate((1.0, .48, .25, .13, .07, .035), start=1):
        detune = 1 + (harmonic - 1) * .00035
        tone += amount * np.sin(2 * np.pi * f * harmonic * detune * t + harmonic * .13)
    attack = 1 - np.exp(-90 * t)
    decay = .72 * np.exp(-2.8 * t / max(duration, .1)) + .28 * np.exp(-.7 * t / max(duration, .1))
    hammer = bandpass(RNG.normal(0, 1, len(t)), 900, 6500) * np.exp(-45 * t) * .035
    return (tone * attack * decay / 1.9 + hammer) * velocity


def felt_piano_note(note, duration, velocity=.5):
    """Rounded low-velocity piano with a soft attack and restrained harmonics."""
    t = seconds(duration)
    f = midi(note)
    tone = (np.sin(2*np.pi*f*t) + .28*np.sin(2*np.pi*f*2*t+.16)
            + .09*np.sin(2*np.pi*f*3*t+.31) + .025*np.sin(2*np.pi*f*4*t))
    attack = 1 - np.exp(-18*t)
    decay = .78*np.exp(-1.7*t/max(duration, .1)) + .22*np.exp(-.38*t/max(duration, .1))
    return lowpass(tone * attack * decay, 3200) * velocity / 1.35


def bass_note(note, duration, velocity=.5):
    t = seconds(duration)
    f = midi(note)
    wave = np.sin(2*np.pi*f*t) + .3*np.sin(2*np.pi*f*2*t+.2) + .1*np.sin(2*np.pi*f*3*t)
    return wave * (1-np.exp(-45*t)) * np.exp(-2.2*t/duration) * velocity


def add(buffer, sound, start, pan=0.0):
    offset = int(start * SR)
    end = min(offset + len(sound), len(buffer))
    if end <= offset: return
    sample = sound[:end-offset]
    angle = (pan + 1) * np.pi / 4
    buffer[offset:end, 0] += sample * np.cos(angle)
    buffer[offset:end, 1] += sample * np.sin(angle)


def add_reverb(stereo):
    wet = stereo.copy()
    for delay, gain, cross in ((.071,.17,False),(.127,.12,True),(.211,.08,False),(.337,.05,True)):
        n = int(delay * SR)
        source = stereo[:-n, ::-1] if cross else stereo[:-n]
        wet[n:] += source * gain
    return wet


def warm_pad_note(note, duration, velocity=.12):
    """A low, breathy tonal bed without the buzz of a saw/square synth."""
    t = seconds(duration)
    f = midi(note)
    vibrato = .0018 * np.sin(2 * np.pi * .18 * t)
    phase = 2 * np.pi * f * (t + vibrato)
    tone = np.sin(phase) + .18 * np.sin(phase * 2 + .3) + .045 * np.sin(phase * 3)
    attack = np.clip(t / .85, 0, 1) ** 1.4
    release = np.clip((duration - t) / 1.15, 0, 1) ** 1.2
    return lowpass(tone * attack * release, 2400) * velocity / 1.22


def soft_chime_note(note, duration, velocity=.1):
    """Rounded wooden/chime accent used very quietly as environmental colour."""
    t = seconds(duration)
    f = midi(note)
    tone = np.sin(2*np.pi*f*t) + .16*np.sin(2*np.pi*f*2.01*t+.25)
    envelope = (1 - np.exp(-24*t)) * np.exp(-3.1*t/max(duration, .1))
    return lowpass(tone * envelope, 3600) * velocity / 1.12


def make_environmental_music(name, tempo, chords, piano_patterns, melody, accents, mood):
    """Compose a spacious, consonant piano piece over a breeze-like tonal bed."""
    beat = 60 / tempo
    bar = beat * 4
    duration = len(chords) * bar
    track = np.zeros((int(duration * SR), 2), dtype=np.float64)

    for bar_index, chord in enumerate(chords):
        start = bar_index * bar

        # Long low pads create warmth without a prominent or repetitive bass line.
        for voice, note in enumerate((chord[0] - 12, chord[1], chord[2])):
            add(track, warm_pad_note(note, bar * 1.08, .045 if voice == 0 else .032),
                start, -.34 + voice * .34)

        # Each bar has its own sparse piano shape, leaving natural pockets of air.
        pattern = piano_patterns[bar_index % len(piano_patterns)]
        for step, chord_index in pattern:
            note = chord[chord_index % len(chord)]
            add(track, felt_piano_note(note, beat * 1.72, .16),
                start + step * beat, -.22 + .14 * chord_index)

    for index, (bar_index, beat_index, note, length) in enumerate(melody):
        add(track, felt_piano_note(note, beat * length, .18),
            bar_index * bar + beat_index * beat, .12 + .13 * math.sin(index * .8))

    for index, (bar_index, beat_index, note) in enumerate(accents):
        add(track, soft_chime_note(note, beat * 2.4, .065),
            bar_index * bar + beat_index * beat, -.18 if index % 2 == 0 else .2)

    # Broad, very quiet filtered air suggests an open window. It has no short loop
    # or obvious pulse, so the dedicated garden/marsh ambience remains distinct.
    air = lowpass(RNG.normal(0, 1, len(track)), 290 if mood == "garden" else 230)
    drift = .0038 + .0012 * np.sin(2*np.pi*(.037 if mood == "garden" else .029)*np.arange(len(track))/SR)
    track[:, 0] += air * drift
    track[:, 1] += np.roll(air, int(.031 * SR)) * drift

    track = add_reverb(track)
    track[:, 0] = lowpass(track[:, 0], 4100)
    track[:, 1] = lowpass(track[:, 1], 4100)
    envelope = np.ones(len(track))
    edge = int(1.5 * SR)
    envelope[:edge] = np.sin(np.linspace(0, np.pi/2, edge)) ** 2
    envelope[-edge:] = np.cos(np.linspace(0, np.pi/2, edge)) ** 2
    write(name, track * envelope[:, None], .56)


def make_music_tracks():
    # Track 1: an open, major-key morning theme with a small descending answer.
    morning_chords = [
        (60,64,67,71), (59,62,67,71), (57,60,64,69), (55,59,62,67),
        (60,64,67,71), (64,67,71,76), (57,60,64,69), (55,59,62,67),
        (60,64,67,71), (62,65,69,72), (57,60,64,69), (55,59,62,67),
    ]
    patterns = [((0,0),(1.5,2),(3,1)), ((.5,1),(2,2)), ((0,2),(2.5,1)), ((1,0),(3,2))]
    morning_melody = [
        (0,.5,76,1.7), (1,2,74,1.4), (2,.5,72,1.8), (3,2,71,1.5),
        (4,.5,76,1.7), (5,2,79,1.5), (6,.5,76,1.8), (7,2,74,1.5),
        (8,.5,79,1.6), (9,2,77,1.5), (10,.5,76,1.8), (11,2,74,1.6),
    ]
    morning_accents = [(2,3.25,84), (6,3.25,83), (10,3.25,81)]
    make_environmental_music("Music_CozyMorning.wav", 66, morning_chords, patterns,
                             morning_melody, morning_accents, "garden")

    # Track 2: a gentler reflective theme; still major and warm, never gloomy.
    evening_chords = [
        (65,69,72,76), (60,65,69,72), (62,65,69,74), (60,64,67,72),
        (65,69,72,76), (57,60,65,69), (62,65,69,74), (60,64,67,72),
        (57,60,65,69), (65,69,72,76), (62,65,69,74), (60,64,67,72),
    ]
    evening_patterns = [((.5,0),(2.5,2)), ((0,1),(2,2),(3.25,0)), ((1,2),(3,1)), ((.5,1),(2.5,2))]
    evening_melody = [
        (0,1,81,1.8), (1,3,79,1.2), (2,1,77,1.8), (3,3,76,1.2),
        (4,1,81,1.8), (5,3,77,1.2), (6,1,74,1.8), (7,3,76,1.3),
        (8,1,77,1.7), (9,3,81,1.2), (10,1,79,1.8), (11,3,76,1.2),
    ]
    evening_accents = [(1,3.5,84), (5,3.5,81), (9,3.5,84)]
    make_environmental_music("Music_EveningCafe.wav", 62, evening_chords, evening_patterns,
                             evening_melody, evening_accents, "pond")


def impulse_hits(duration, times, tone_freq, noise_low, noise_high):
    result = np.zeros(int(duration * SR))
    for hit in times:
        length = min(int(.16 * SR), len(result) - int(hit * SR))
        t = np.arange(length) / SR
        noise = bandpass(RNG.normal(0, 1, length), noise_low, noise_high)
        sound = (.65 * noise + .35 * np.sin(2*np.pi*tone_freq*t)) * np.exp(-32*t)
        result[int(hit*SR):int(hit*SR)+length] += sound
    return fade(result)


def make_sfx():
    write("SFX_Chopping.wav", impulse_hits(1.05, (.05,.30,.55,.82), 185, 700, 6500), .86)

    t = seconds(6.1)
    sizzle = highpass(RNG.normal(0, 1, len(t)), 2400) * (.16 + .045*np.sin(2*np.pi*.7*t))
    pops = np.zeros_like(t)
    for moment in (.4,1.15,1.9,2.7,3.55,4.2,5.1,5.65):
        start = int(moment*SR); length = min(int(.07*SR), len(t)-start); local=np.arange(length)/SR
        pops[start:start+length] += RNG.normal(0,1,length)*np.exp(-55*local)*.25
    write("SFX_CookingSizzle.wav", fade(sizzle+pops,.12,.18), .72)

    t = seconds(.85)
    creak = np.sin(2*np.pi*(95+38*t)*t) * np.exp(-2.8*t) + lowpass(RNG.normal(0,1,len(t)),900)*.12
    write("SFX_FridgeOpen.wav", fade(creak), .72)
    t = seconds(.55)
    close = np.sin(2*np.pi*72*t)*np.exp(-14*t) + bandpass(RNG.normal(0,1,len(t)),180,1800)*np.exp(-20*t)*.35
    write("SFX_FridgeClose.wav", fade(close,.005,.04), .80)

    prepared = np.zeros(int(1.5*SR))
    for start, note in ((0,72),(.18,76),(.36,79),(.58,84)):
        tone=piano_note(note,.82,.55); i=int(start*SR); prepared[i:i+len(tone)] += tone[:len(prepared)-i]
    write("SFX_FoodPrepared.wav", add_reverb(np.column_stack((prepared,prepared)))[:,0], .82)

    order = np.zeros(int(1.1*SR))
    for start,note in ((0,79),(.17,83),(.34,86)):
        tone=piano_note(note,.6,.52); i=int(start*SR); order[i:i+len(tone)] += tone[:len(order)-i]
    write("SFX_NewOrder.wav", order, .78)

    received = np.zeros(int(.72*SR))
    for start,note in ((0,72),(.13,79)):
        tone=piano_note(note,.48,.48); i=int(start*SR); received[i:i+len(tone)] += tone[:len(received)-i]
    write("SFX_OrderReceived.wav", received, .75)

    arrival = np.zeros(int(1.9*SR))
    for start, duration, pitch in ((.06,.38,132),(.51,.44,145),(1.08,.55,126)):
        local = seconds(duration)
        syllable = sum(np.sin(2*np.pi*pitch*(h+1)*local)/(h+1) for h in range(7))
        syllable = bandpass(syllable, 170, 1450)
        breath = bandpass(RNG.normal(0,1,len(local)), 300, 1800) * .07
        shape = np.sin(np.pi*np.clip(local/duration,0,1)) ** 1.5
        i=int(start*SR); arrival[i:i+len(local)] += (syllable*.13 + breath)*shape
    write("SFX_CustomerArrival.wav", lowpass(arrival, 1750), .54)

    happy = np.zeros(int(1.65*SR))
    for start, duration, pitch in ((.04,.46,165),(.39,.48,196),(.76,.68,220)):
        local=seconds(duration)
        phase=2*np.pi*(pitch*local + 1.6*np.sin(2*np.pi*4.8*local)/(2*np.pi*4.8))
        voice=np.sin(phase) + .32*np.sin(phase*2+.2)
        shape=np.sin(np.pi*np.clip(local/duration,0,1))**1.25
        i=int(start*SR); happy[i:i+len(local)] += lowpass(voice, 2100)*shape*.16
    write("SFX_CustomerHappy.wav", add_reverb(np.column_stack((happy,happy)))[:,0], .58)
    write("SFX_Trash.wav", impulse_hits(.72, (.04,.27), 105, 130, 2400), .82)


def loop_envelope(length, edge=.08):
    env=np.ones(length); n=int(edge*SR); env[:n]=np.linspace(0,1,n); env[-n:]=np.linspace(1,0,n); return env


def make_ambience():
    duration=20.0; t=seconds(duration); env=loop_envelope(len(t), .4)
    wind=lowpass(RNG.normal(0,1,len(t)),650) * (.13+.05*np.sin(2*np.pi*.11*t)+.025*np.sin(2*np.pi*.27*t))
    write("AMB_Wind.wav", wind*env, .58)

    duration=2.4; t=seconds(duration); env=loop_envelope(len(t), .08)
    birds=np.zeros_like(t)
    for start,base in ((.12,1850),(.48,2420),(1.22,2050),(1.61,2740)):
        length=int(.34*SR); local=np.arange(length)/SR
        chirp=np.sin(2*np.pi*(base*local+520*local*local))*.13*np.sin(np.pi*np.clip(local/.34,0,1))
        i=int(start*SR); birds[i:i+length]+=chirp
    write("AMB_Birds.wav", birds*env, .68)

    duration=1.8; t=seconds(duration); env=loop_envelope(len(t), .05)
    crickets=np.zeros_like(t)
    for start in (.08,.18,.34,.72,.84,1.23,1.38):
        length=int(.09*SR); local=np.arange(length)/SR
        chirp=np.sin(2*np.pi*(4600+180*np.sin(2*np.pi*16*local))*local)*np.sin(np.pi*local/.09)*.09
        i=int(start*SR); crickets[i:i+length]+=chirp
    write("AMB_Crickets.wav", crickets*env, .52)

    duration=20.0; t=seconds(duration); env=loop_envelope(len(t), .4)
    water=bandpass(RNG.normal(0,1,len(t)),120,1700)*(.12+.04*np.sin(2*np.pi*.23*t))
    water+=lowpass(RNG.normal(0,1,len(t)),180)*.10
    write("AMB_Water.wav", water*env, .58)

    duration=1.35; t=seconds(duration); env=loop_envelope(len(t), .05)
    paddle=np.zeros_like(t)
    for start in (.08,.48):
        length=int(.75*SR); local=np.arange(length)/SR
        splash=bandpass(RNG.normal(0,1,length),450,6000)*np.exp(-5*local)*.18
        i=int(start*SR); paddle[i:i+length]+=splash
    write("AMB_Paddle.wav", paddle*env, .62)

    duration=1.4; t=seconds(duration); env=loop_envelope(len(t), .05)
    frog=np.zeros_like(t)
    for start in (.10,.72):
        length=int(.52*SR); local=np.arange(length)/SR
        croak=(np.sin(2*np.pi*(115+22*np.sin(2*np.pi*13*local))*local)+.35*np.sin(2*np.pi*230*local))*np.sin(np.pi*local/.52)*.18
        i=int(start*SR); frog[i:i+length]+=croak
    write("AMB_Frog.wav", frog*env, .62)

    duration=1.5; t=seconds(duration); env=loop_envelope(len(t), .05)
    hiss=np.zeros_like(t)
    for start in (.18,):
        length=int(1.1*SR); local=np.arange(length)/SR
        breath=bandpass(RNG.normal(0,1,length),2800,10500)*np.sin(np.pi*local/1.1)*.10
        i=int(start*SR); hiss[i:i+length]+=breath
    write("AMB_Hiss.wav", hiss*env, .48)

    duration=12.0; t=seconds(duration); env=loop_envelope(len(t))
    flies=bandpass(RNG.normal(0,1,len(t)),190,1300)*.025
    flies+=(np.sin(2*np.pi*(185+24*np.sin(2*np.pi*.8*t))*t)+.4*np.sin(2*np.pi*370*t))*(.035+.018*np.sin(2*np.pi*.31*t))
    write("AMB_Flies.wav", flies*env, .38)


if __name__ == "__main__":
    make_music_tracks()
    make_sfx()
    make_ambience()

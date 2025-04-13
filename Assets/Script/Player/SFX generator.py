import numpy as np
from scipy.io.wavfile import write
import os

def generate_tone(frequency, duration, sample_rate=44100, volume=0.5):
    t = np.linspace(0, duration, int(sample_rate * duration), False)
    tone = np.sin(2 * np.pi * frequency * t) * volume
    return tone

def night_vision_on(sample_rate=44100):
    # Simulasi suara naik, seperti "power up"
    freqs = np.linspace(300, 1200, num=500)
    tone = np.concatenate([generate_tone(f, 0.0015, sample_rate) for f in freqs])
    return tone

def night_vision_off(sample_rate=44100):
    # Simulasi suara turun, seperti "power down"
    freqs = np.linspace(1200, 300, num=500)
    tone = np.concatenate([generate_tone(f, 0.0015, sample_rate) for f in freqs])
    return tone

# Gabungkan dan simpan sebagai WAV
sample_rate = 44100
on_sound = night_vision_on(sample_rate)
off_sound = night_vision_off(sample_rate)

# Normalisasi ke int16
combined = np.concatenate((on_sound, np.zeros(22050), off_sound))  # jeda di tengah
audio = np.int16(combined / np.max(np.abs(combined)) * 32767)

write("night_vision_effect.wav", sample_rate, audio)
print("SFX saved as night_vision_effect.wav")

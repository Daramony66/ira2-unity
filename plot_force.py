import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
from scipy.signal import find_peaks

df = pd.read_csv("force_log.csv")

# --- Graphe temporel ---
plt.figure(figsize=(12, 5))
plt.plot(df["time_s"], df["brut_N"], label="Force brute", color="red", alpha=0.6)
plt.plot(df["time_s"], df["filtered_N"], label="Force lissée IIR (alpha=0.87)", color="orange")
plt.plot(df["time_s"], df["applied_N"], label="Force scalée", color="green")
plt.plot(df["time_s"], df["ressenti_N"], label="Force ressentie", color="blue")
plt.axhline(y=3.3, color="red", linestyle="--", label="MaxForce Touch (3.3N)")
plt.xlabel("Temps (s)")
plt.ylabel("Force (N)")
plt.title("Pipeline de traitement du signal de force")
plt.legend()
plt.grid(True)
plt.tight_layout()
plt.savefig("force_plot.png", dpi=150)
plt.show()

# --- FFT ---
signal = df["brut_N"].values
fs_real = 1 / df["time_s"].diff().mean()
N = len(signal)
freqs = np.fft.rfftfreq(N, d=1/fs_real)
fft_vals = np.abs(np.fft.rfft(signal))
plt.figure(figsize=(10, 4))
plt.plot(freqs, fft_vals)
plt.xlabel("Fréquence (Hz)")
plt.ylabel("Amplitude")
plt.title("Spectre fréquentiel de la force brute")
plt.grid(True)
plt.xlim(0, 50)
plt.show()

# --- Détection du pic principal (amplitude maximale) ---
def get_main_peak(series, height, distance=100):
    peaks, _ = find_peaks(series, height=height, distance=distance)
    if len(peaks) == 0:
        return None, None
    main = peaks[np.argmax(series.iloc[peaks].values)]
    return main, series.iloc[main]

idx_brut, val_brut = get_main_peak(df["brut_N"], height=5)
idx_filtered, val_filtered = get_main_peak(df["filtered_N"], height=5)
idx_applied, val_applied = get_main_peak(df["applied_N"], height=0.1)
idx_ressenti, val_ressenti = get_main_peak(df["ressenti_N"], height=0.1)

if idx_brut is not None:
    print(f"Pic brut      : t={df['time_s'].iloc[idx_brut]:.6f}, F={val_brut:.3f}N")
if idx_filtered is not None:
    print(f"Pic filtré    : t={df['time_s'].iloc[idx_filtered]:.6f}, F={val_filtered:.3f}N")
if idx_applied is not None:
    print(f"Pic scalé     : t={df['time_s'].iloc[idx_applied]:.6f}, F={val_applied:.3f}N")
if idx_ressenti is not None:
    print(f"Pic ressenti  : t={df['time_s'].iloc[idx_ressenti]:.6f}, F={val_ressenti:.3f}N")

if idx_brut is not None:
    if idx_filtered is not None:
        delai = (df['time_s'].iloc[idx_filtered] - df['time_s'].iloc[idx_brut]) * 1000
        print(f"\nDélai brut → filtré  : {delai:.1f} ms")
    if idx_applied is not None:
        delai = (df['time_s'].iloc[idx_applied] - df['time_s'].iloc[idx_brut]) * 1000
        print(f"Délai brut → scalé   : {delai:.1f} ms")
    if idx_ressenti is not None:
        delai = (df['time_s'].iloc[idx_ressenti] - df['time_s'].iloc[idx_brut]) * 1000
        print(f"Délai brut → ressenti: {delai:.1f} ms")
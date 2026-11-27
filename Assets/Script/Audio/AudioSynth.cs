using System;
using UnityEngine;

/// <summary>
/// Small procedural sound synthesizer. The project ships with music only and no sound effects,
/// so every effect and ambience loop is generated from noise, oscillators, filters and envelopes.
/// Replace any of them with recorded clips through the AudioCatalog without code changes.
/// </summary>
public static class AudioSynth
{
    public const int Rate = 22050;

    public class Buffer
    {
        public float[] d;
        public readonly System.Random rng;
        public Buffer(float seconds, int seed) { d = new float[Mathf.CeilToInt(seconds * Rate)]; rng = new System.Random(seed); }
        public int Length => d.Length;
        public float Seconds => d.Length / (float)Rate;
        public float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);
        public float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
    }

    // ---------------- building blocks ----------------
    /// <summary>Filtered noise layer. cutoff/hp are functions of normalized time (0..1).</summary>
    public static void NoiseLayer(Buffer b, float start, float length, Func<float, float> lowpassHz, Func<float, float> highpassHz, Func<float, float> env, float gain)
    {
        int s0 = Mathf.Clamp((int)(start * Rate), 0, b.Length), n = Mathf.Min(b.Length - s0, (int)(length * Rate));
        float lp = 0f, lp2 = 0f, hpState = 0f, prevIn = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Mathf.Max(1, n);
            float x = b.Noise();
            float a = Alpha(lowpassHz(t));
            lp += a * (x - lp);
            lp2 += a * (lp - lp2);
            float y = lp2;
            if (highpassHz != null)
            {
                float rc = 1f / (2f * Mathf.PI * Mathf.Max(10f, highpassHz(t)));
                float k = rc / (rc + 1f / Rate);
                hpState = k * (hpState + y - prevIn);
                prevIn = y;
                y = hpState;
            }
            b.d[s0 + i] += y * env(t) * gain;
        }
    }

    /// <summary>Oscillator layer: wave 0 sine, 1 saw, 2 square, 3 triangle. freq is a function of normalized time.</summary>
    public static void Tone(Buffer b, float start, float length, Func<float, float> freq, Func<float, float> env, float gain, int wave = 0, float vibratoHz = 0f, float vibratoDepth = 0f, float lowpassHz = 0f)
    {
        int s0 = Mathf.Clamp((int)(start * Rate), 0, b.Length), n = Mathf.Min(b.Length - s0, (int)(length * Rate));
        double phase = b.rng.NextDouble();
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Mathf.Max(1, n);
            float f = freq(t) * (1f + vibratoDepth * Mathf.Sin(2f * Mathf.PI * vibratoHz * i / Rate));
            phase += f / Rate;
            phase -= Math.Floor(phase);
            float p = (float)phase;
            float v = wave == 1 ? 2f * p - 1f : wave == 2 ? (p < 0.5f ? 1f : -1f) : wave == 3 ? 1f - 4f * Mathf.Abs(p - 0.5f) : Mathf.Sin(2f * Mathf.PI * p);
            if (lowpassHz > 0f) { lp += Alpha(lowpassHz) * (v - lp); v = lp; }
            b.d[s0 + i] += v * env(t) * gain;
        }
    }

    /// <summary>Short clicks (bone rattles, crackles, chain links).</summary>
    public static void Clicks(Buffer b, float start, float length, int count, float minHz, float maxHz, float clickLen, float gain)
    {
        for (int c = 0; c < count; c++)
        {
            float at = start + b.Rand(0f, length);
            float hz = b.Rand(minHz, maxHz);
            float g = gain * b.Rand(0.5f, 1f);
            NoiseLayer(b, at, clickLen, t => hz, t => hz * 0.5f, t => Mathf.Exp(-t * 6f), g);
        }
    }

    public static float Alpha(float hz) => 1f - Mathf.Exp(-2f * Mathf.PI * Mathf.Clamp(hz, 20f, Rate * 0.45f) / Rate);
    public static Func<float, float> Env(float attack, float decayPow = 1f) => t => (t < attack ? t / Mathf.Max(0.0001f, attack) : Mathf.Pow(1f - (t - attack) / (1f - attack), decayPow));
    public static Func<float, float> Exp(float k) => t => Mathf.Clamp01(t * 60f) * Mathf.Exp(-k * t);
    public static Func<float, float> Const(float v) => t => v;
    public static Func<float, float> Lerp(float a, float b) => t => Mathf.Lerp(a, b, t);

    /// <summary>Simple feedback echo for space (graves, halls).</summary>
    public static void Echo(Buffer b, float delay, float feedback, int taps = 4)
    {
        int ds = (int)(delay * Rate);
        for (int tap = 0; tap < taps; tap++)
            for (int i = b.Length - 1; i >= ds; i--)
                b.d[i] += b.d[i - ds] * feedback;
    }

    /// <summary>Crossfades the tail into the head so the buffer loops without a click.</summary>
    public static void MakeLoop(Buffer b, float fadeSeconds)
    {
        int f = Mathf.Min(b.Length / 3, (int)(fadeSeconds * Rate));
        for (int i = 0; i < f; i++)
        {
            float k = i / (float)f;
            b.d[i] = b.d[i] * k + b.d[b.Length - f + i] * (1f - k);
        }
        Array.Resize(ref b.d, b.Length - f);
    }

    public static AudioClip ToClip(string name, Buffer b, float peak = 0.85f, bool loop = false)
    {
        float[] data = b.d;
        float max = 0.0001f;
        for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
        float g = peak / max;
        var outData = new float[data.Length];
        for (int i = 0; i < data.Length; i++) outData[i] = Mathf.Clamp(data[i] * g, -1f, 1f);
        // Tiny fades at both ends prevent clicks on one-shots.
        int edge = Mathf.Min(64, outData.Length / 4);
        if (!loop)
            for (int i = 0; i < edge; i++) { float k = i / (float)edge; outData[i] *= k; outData[outData.Length - 1 - i] *= k; }
        var clip = AudioClip.Create(name, outData.Length, 1, Rate, false);
        clip.SetData(outData, 0);
        return clip;
    }
}

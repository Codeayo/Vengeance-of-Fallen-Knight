using System;
using System.Collections.Generic;
using UnityEngine;
using S = AudioSynth;

/// <summary>
/// Every sound effect and ambience in the game, keyed by name. Clips are synthesized on first use
/// (three variations for one-shots) and cached. A recorded clip with the same key in the
/// AudioCatalog always takes priority.
/// </summary>
public static class SoundLibrary
{
    private static readonly Dictionary<string, AudioClip[]> cache = new Dictionary<string, AudioClip[]>();
    private static readonly Dictionary<string, Action<S.Buffer>> recipes = new Dictionary<string, Action<S.Buffer>>();
    private static readonly Dictionary<string, float> lengths = new Dictionary<string, float>();
    private static readonly HashSet<string> loops = new HashSet<string>();

    public static AudioClip Get(string key)
    {
        AudioClip[] v = GetAll(key);
        return v == null || v.Length == 0 ? null : v[UnityEngine.Random.Range(0, v.Length)];
    }

    public static AudioClip[] GetAll(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (cache.TryGetValue(key, out var c)) return c;
        AudioClip recorded = AudioCatalog.Instance != null ? AudioCatalog.Instance.Sfx(key) : null;
        if (recorded != null) return cache[key] = new[] { recorded };

        EnsureRecipes();
        if (!recipes.TryGetValue(key, out var recipe))
        {
            Debug.LogWarning("No sound recipe for '" + key + "'");
            return cache[key] = new AudioClip[0];
        }

        bool loop = loops.Contains(key);
        int variants = loop ? 1 : 3;
        var clips = new AudioClip[variants];
        for (int i = 0; i < variants; i++)
        {
            var b = new S.Buffer(lengths[key], key.GetHashCode() + i * 7919);
            recipe(b);
            if (loop) S.MakeLoop(b, 1.2f);
            clips[i] = S.ToClip(key + "_" + i, b, loop ? 0.7f : 0.85f, loop);
        }

        return cache[key] = clips;
    }

    public static bool IsLoop(string key) { EnsureRecipes(); return loops.Contains(key); }

    static void R(string key, float seconds, Action<S.Buffer> recipe, bool loop = false)
    {
        recipes[key] = recipe;
        lengths[key] = seconds;
        if (loop) loops.Add(key);
    }

    static bool built;

    static void EnsureRecipes()
    {
        if (built) return;
        built = true;

        // ---------------- combat ----------------
        R("swing", 0.24f, b => S.NoiseLayer(b, 0, 0.24f, t => Mathf.Lerp(700, 3200, t), t => 300, S.Env(0.35f, 2f), 1f));
        R("swing_heavy", 0.36f, b =>
        {
            S.NoiseLayer(b, 0, 0.36f, t => Mathf.Lerp(380, 1500, t), t => 120, S.Env(0.4f, 2f), 1f);
            S.Tone(b, 0, 0.3f, S.Lerp(120, 70), S.Env(0.3f, 2f), 0.25f);
        });
        R("hit", 0.22f, b =>
        {
            S.NoiseLayer(b, 0, 0.09f, S.Const(2600), null, S.Exp(9f), 0.9f);
            S.Tone(b, 0, 0.2f, S.Lerp(150, 55), S.Exp(7f), 0.8f);
        });
        R("hit_bone", 0.3f, b =>
        {
            S.Tone(b, 0, 0.18f, S.Lerp(130, 60), S.Exp(8f), 0.7f);
            S.Clicks(b, 0, 0.22f, 9, 1800, 4200, 0.025f, 0.7f);
        });
        R("clank", 0.6f, b =>
        {
            foreach (float f in new[] { 523f, 1347f, 2211f, 3170f })
                S.Tone(b, 0, 0.6f, S.Const(f * b.Rand(0.97f, 1.03f)), S.Exp(5f + f / 900f), 0.35f);
            S.NoiseLayer(b, 0, 0.05f, S.Const(5000), null, S.Exp(12f), 0.6f);
        });
        R("sword_ring", 1.6f, b =>
        {
            foreach (float f in new[] { 880f, 1320f, 1985f, 2640f })
                S.Tone(b, 0, 1.6f, S.Const(f), S.Exp(2.5f), 0.3f, 0, 5f, 0.004f);
            S.Echo(b, 0.11f, 0.3f, 3);
        });

        // ---------------- player ----------------
        R("step_dirt", 0.09f, b => S.NoiseLayer(b, 0, 0.09f, S.Const(b.Rand(380, 650)), t => 90, S.Exp(10f), 1f));
        R("step_snow", 0.13f, b => { S.NoiseLayer(b, 0, 0.13f, S.Const(2400), t => 600, S.Exp(6f), 0.8f); S.Clicks(b, 0, 0.1f, 6, 2000, 5000, 0.01f, 0.4f); });
        R("step_stone", 0.1f, b => { S.NoiseLayer(b, 0, 0.05f, S.Const(1400), t => 300, S.Exp(14f), 0.9f); S.Tone(b, 0, 0.08f, S.Const(b.Rand(110, 150)), S.Exp(12f), 0.4f); });
        R("jump", 0.22f, b => S.NoiseLayer(b, 0, 0.22f, S.Lerp(500, 1800), t => 200, S.Env(0.2f, 2f), 0.7f));
        R("land", 0.25f, b => { S.Tone(b, 0, 0.18f, S.Lerp(95, 50), S.Exp(9f), 0.9f); S.NoiseLayer(b, 0, 0.2f, S.Const(700), t => 80, S.Exp(8f), 0.6f); });
        R("roll", 0.4f, b => S.NoiseLayer(b, 0, 0.4f, t => 900 + 600 * Mathf.Sin(t * 9f), t => 150, S.Env(0.25f, 1.5f), 0.8f));
        R("player_hurt", 0.32f, b =>
        {
            S.Tone(b, 0, 0.3f, S.Lerp(175, 120), S.Env(0.08f, 1.5f), 0.6f, 1, 0, 0, 900);
            S.NoiseLayer(b, 0, 0.25f, S.Const(1100), t => 300, S.Env(0.1f, 2f), 0.4f);
        });
        R("player_death", 1.6f, b =>
        {
            S.Tone(b, 0, 1.0f, S.Lerp(150, 70), S.Env(0.1f, 1.2f), 0.6f, 1, 4, 0.02f, 700);
            S.NoiseLayer(b, 0, 1.0f, S.Const(900), t => 200, S.Env(0.1f, 1.5f), 0.3f);
            foreach (float f in new[] { 640f, 1510f, 2390f }) S.Tone(b, 0.75f, 0.8f, S.Const(f), S.Exp(5f), 0.25f);
            S.Tone(b, 0.75f, 0.5f, S.Lerp(90, 45), S.Exp(6f), 0.6f);
        });
        R("block", 0.5f, b =>
        {
            foreach (float f in new[] { 410f, 1090f, 1780f }) S.Tone(b, 0, 0.5f, S.Const(f), S.Exp(6f), 0.35f);
            S.Tone(b, 0, 0.15f, S.Lerp(120, 60), S.Exp(10f), 0.6f);
        });
        R("rise", 2.2f, b =>
        {
            S.NoiseLayer(b, 0, 2.2f, S.Lerp(300, 1600), t => 100, S.Env(0.6f, 1f), 0.5f);
            S.Tone(b, 0, 2.2f, S.Lerp(110, 220), S.Env(0.5f, 1f), 0.3f, 0, 5, 0.01f);
            S.Tone(b, 0, 2.2f, S.Lerp(165, 330), S.Env(0.5f, 1f), 0.2f, 0, 5.5f, 0.01f);
            S.Echo(b, 0.17f, 0.35f, 3);
        });

        // ---------------- skeletons ----------------
        R("bone_rattle", 0.4f, b => S.Clicks(b, 0, 0.35f, 10, 1600, 4000, 0.02f, 0.8f));
        R("skel_step", 0.12f, b => { S.Clicks(b, 0, 0.06f, 3, 1500, 3500, 0.02f, 0.6f); S.Tone(b, 0, 0.08f, S.Const(120), S.Exp(12f), 0.3f); });
        R("skel_alert", 0.7f, b =>
        {
            S.NoiseLayer(b, 0, 0.6f, t => 1200 + 1800 * t, t => 600, S.Env(0.3f, 1.5f), 0.7f);
            S.Clicks(b, 0, 0.5f, 14, 1800, 4500, 0.02f, 0.6f);
        });
        R("skel_hurt", 0.35f, b => { S.NoiseLayer(b, 0, 0.05f, S.Const(4000), null, S.Exp(14f), 1f); S.Clicks(b, 0.02f, 0.25f, 8, 1500, 4000, 0.025f, 0.6f); });
        R("skel_death", 1.2f, b =>
        {
            S.Clicks(b, 0, 1.0f, 36, 1200, 4500, 0.03f, 0.7f);
            S.Tone(b, 0.6f, 0.4f, S.Lerp(100, 50), S.Exp(7f), 0.7f);
            S.NoiseLayer(b, 0, 0.7f, S.Const(900), t => 200, S.Env(0.05f, 2f), 0.4f);
        });
        R("ghoul_groan", 0.9f, b => S.Tone(b, 0, 0.9f, t => 95 + 20 * Mathf.Sin(t * 12f), S.Env(0.2f, 1.2f), 0.7f, 1, 3, 0.03f, 500));

        // ---------------- ninjas ----------------
        R("ninja_step", 0.07f, b => S.NoiseLayer(b, 0, 0.07f, S.Const(900), t => 250, S.Exp(14f), 0.6f));
        R("ninja_alert", 0.28f, b =>
        {
            S.NoiseLayer(b, 0, 0.2f, S.Const(1100), t => 500, S.Env(0.1f, 2f), 0.6f);
            S.Tone(b, 0, 0.2f, S.Lerp(260, 200), S.Env(0.1f, 2f), 0.5f, 1, 0, 0, 1200);
        });
        R("ninja_hurt", 0.25f, b => S.Tone(b, 0, 0.25f, S.Lerp(240, 170), S.Env(0.05f, 2f), 0.7f, 1, 0, 0, 1100));
        R("ninja_death", 0.9f, b =>
        {
            S.Tone(b, 0, 0.6f, S.Lerp(230, 110), S.Env(0.05f, 1.3f), 0.6f, 1, 0, 0, 1000);
            S.Tone(b, 0.55f, 0.3f, S.Lerp(90, 50), S.Exp(8f), 0.6f);
        });
        R("blade_draw", 0.5f, b =>
        {
            S.NoiseLayer(b, 0, 0.35f, S.Lerp(3000, 7000), t => 1500, S.Env(0.2f, 1.5f), 0.6f);
            foreach (float f in new[] { 1760f, 2640f, 3960f }) S.Tone(b, 0.1f, 0.4f, S.Const(f), S.Exp(5f), 0.2f);
        });

        // ---------------- bosses (each has its own voice) ----------------
        R("roar_gravewarden", 2.0f, b =>
        {
            S.Tone(b, 0, 1.8f, t => 80 + 10 * Mathf.Sin(t * 20f), S.Env(0.15f, 1.2f), 0.7f, 1, 7, 0.05f, 600);
            S.Clicks(b, 0, 1.8f, 60, 1500, 4500, 0.03f, 0.5f);
            S.Echo(b, 0.2f, 0.3f, 3);
        });
        R("roar_kage", 1.4f, b =>
        {
            S.NoiseLayer(b, 0, 0.5f, S.Lerp(2500, 8000), t => 1500, S.Env(0.3f, 1.5f), 0.7f);
            foreach (float f in new[] { 1975f, 2960f, 4440f }) S.Tone(b, 0.2f, 1.2f, S.Const(f), S.Exp(3f), 0.25f);
            S.Tone(b, 0.3f, 0.6f, S.Lerp(260, 160), S.Env(0.1f, 1.5f), 0.4f, 1, 0, 0, 1300);
        });
        R("roar_colossus", 2.6f, b =>
        {
            S.Tone(b, 0, 2.4f, t => 48 + 12 * Mathf.Sin(t * 8f), S.Env(0.2f, 1.1f), 0.9f, 1, 4, 0.06f, 380);
            S.NoiseLayer(b, 0, 2.4f, t => 400 + 300 * Mathf.Sin(t * 13f), t => 60, S.Env(0.25f, 1.2f), 0.6f);
            S.Echo(b, 0.24f, 0.3f, 3);
        });
        R("roar_frost", 2.2f, b =>
        {
            S.NoiseLayer(b, 0, 2.0f, t => 1800 + 900 * Mathf.Sin(t * 6f), t => 500, S.Env(0.2f, 1.2f), 0.6f);
            S.Tone(b, 0, 1.6f, t => 70 + 8 * Mathf.Sin(t * 15f), S.Env(0.15f, 1.2f), 0.6f, 1, 6, 0.04f, 500);
            S.Clicks(b, 0.1f, 1.6f, 25, 4000, 8000, 0.015f, 0.5f);
        });
        R("roar_dravek", 2.0f, b =>
        {
            S.Tone(b, 0, 1.6f, t => 90 + 15 * Mathf.Sin(t * 10f), S.Env(0.15f, 1.3f), 0.7f, 1, 5, 0.04f, 700);
            foreach (float f in new[] { 380f, 990f, 1630f }) S.Tone(b, 1.0f, 1.0f, S.Const(f), S.Exp(4f), 0.35f);
            S.Echo(b, 0.22f, 0.3f, 3);
        });
        R("roar_malvric", 3.2f, b =>
        {
            foreach (float f in new[] { 73.4f, 110f, 146.8f, 174.6f, 220f })
                S.Tone(b, 0, 3.2f, S.Const(f * b.Rand(0.995f, 1.005f)), S.Env(0.35f, 1f), 0.22f, 0, 4.5f, 0.006f);
            S.Tone(b, 0, 3.0f, S.Const(55), S.Env(0.2f, 1f), 0.5f, 1, 0, 0, 220);
            S.Echo(b, 0.3f, 0.35f, 4);
        });
        R("boss_slam", 0.8f, b =>
        {
            S.Tone(b, 0, 0.7f, S.Lerp(70, 30), S.Exp(4f), 1f);
            S.NoiseLayer(b, 0, 0.6f, S.Const(500), t => 50, S.Exp(5f), 0.8f);
        });

        // ---------------- story / ghost / items ----------------
        R("ghost_appear", 2.4f, b =>
        {
            foreach (float f in new[] { 392f, 466f, 587f, 784f })
                S.Tone(b, 0, 2.4f, t => f * (0.8f + 0.2f * t), S.Env(0.6f, 1f), 0.18f, 0, 3f, 0.01f);
            S.NoiseLayer(b, 0, 2.4f, S.Lerp(800, 3000), t => 600, S.Env(0.5f, 1f), 0.25f);
            S.Echo(b, 0.23f, 0.4f, 4);
        });
        R("ghost_whisper", 2.4f, b =>
            S.NoiseLayer(b, 0, 2.4f, t => 1500 + 900 * Mathf.Sin(t * 31f), t => 700, t => Mathf.Clamp01(Mathf.Sin(t * Mathf.PI)) * (0.4f + 0.6f * Mathf.Abs(Mathf.Sin(t * 26f))), 0.6f));
        R("ghost_vanish", 2.0f, b =>
        {
            foreach (float f in new[] { 784f, 587f, 466f })
                S.Tone(b, 0, 2.0f, t => f * (1.1f - 0.4f * t), S.Env(0.05f, 1.5f), 0.2f, 0, 3f, 0.01f);
            S.Echo(b, 0.23f, 0.4f, 4);
        });
        R("thunder", 4.5f, b =>
        {
            S.NoiseLayer(b, 0, 0.4f, S.Const(3000), t => 800, S.Exp(8f), 0.6f);
            S.NoiseLayer(b, 0.05f, 4.4f, t => 300 * (1f - t) + 60, t => 25, t => Mathf.Clamp01(t * 15f) * Mathf.Exp(-2.2f * t) * (0.7f + 0.3f * Mathf.Sin(t * 40f)), 1.2f);
        });
        R("raven", 1.4f, b =>
        {
            for (int c = 0; c < 3; c++)
                S.Tone(b, c * 0.38f, 0.26f, S.Lerp(760, 560), S.Env(0.15f, 1.5f), 0.6f, 1, 30, 0.05f, 2200);
        });
        R("dirt_burst", 1.8f, b =>
        {
            S.Tone(b, 0, 1.4f, t => 50 + 8 * Mathf.Sin(t * 30f), S.Env(0.1f, 1.3f), 0.8f);
            S.Clicks(b, 0, 1.6f, 60, 600, 2500, 0.03f, 0.6f);
            S.NoiseLayer(b, 0, 1.6f, S.Const(700), t => 60, S.Env(0.1f, 1.5f), 0.6f);
        });
        R("pickup", 0.3f, b => { S.Tone(b, 0, 0.3f, S.Lerp(880, 1320), S.Exp(5f), 0.5f); S.Tone(b, 0.07f, 0.23f, S.Const(1760), S.Exp(6f), 0.3f); });
        R("pickup_rare", 0.9f, b =>
        {
            foreach (var (f, d) in new[] { (784f, 0f), (988f, 0.1f), (1175f, 0.2f), (1568f, 0.3f) }) S.Tone(b, d, 0.6f, S.Const(f), S.Exp(3.5f), 0.35f);
            S.Echo(b, 0.12f, 0.3f, 3);
        });
        R("heal", 0.9f, b => { S.Tone(b, 0, 0.9f, S.Lerp(440, 880), S.Env(0.3f, 1.2f), 0.4f, 0, 6, 0.01f); S.Tone(b, 0, 0.9f, S.Lerp(660, 1320), S.Env(0.3f, 1.2f), 0.25f); });
        R("levelup", 1.6f, b =>
        {
            foreach (var (f, d) in new[] { (523f, 0f), (659f, 0.12f), (784f, 0.24f), (1047f, 0.36f) }) S.Tone(b, d, 1.2f, S.Const(f), S.Exp(2.5f), 0.35f);
            S.Echo(b, 0.15f, 0.35f, 3);
        });
        R("shrine", 1.6f, b =>
        {
            S.NoiseLayer(b, 0, 0.8f, S.Lerp(400, 2000), t => 200, S.Env(0.1f, 1.5f), 0.5f);
            foreach (float f in new[] { 330f, 495f, 660f }) S.Tone(b, 0.1f, 1.5f, S.Const(f), S.Env(0.2f, 1.2f), 0.25f, 0, 5, 0.008f);
        });
        R("gate_open", 1.4f, b =>
        {
            S.Clicks(b, 0, 1.2f, 30, 800, 2500, 0.04f, 0.6f);
            S.Tone(b, 0, 1.3f, S.Lerp(90, 60), S.Env(0.1f, 1.2f), 0.5f, 1, 0, 0, 400);
        });
        R("chain", 0.8f, b => S.Clicks(b, 0, 0.7f, 12, 2500, 6000, 0.035f, 0.7f));
        R("door_creak", 1.6f, b => S.Tone(b, 0, 1.6f, t => 180 + 140 * Mathf.Sin(t * 5f), S.Env(0.2f, 1f), 0.5f, 1, 18, 0.08f, 1400));
        R("drip", 0.3f, b => S.Tone(b, 0, 0.3f, S.Lerp(1400, 2400), S.Exp(14f), 0.5f));
        R("bird", 0.5f, b => { S.Tone(b, 0, 0.12f, S.Lerp(3200, 4200), S.Env(0.2f, 1.5f), 0.4f); S.Tone(b, 0.18f, 0.12f, S.Lerp(3600, 2900), S.Env(0.2f, 1.5f), 0.35f); });
        R("ice_crack", 0.5f, b => { S.NoiseLayer(b, 0, 0.06f, S.Const(6000), t => 2000, S.Exp(14f), 1f); S.Clicks(b, 0.03f, 0.4f, 8, 4000, 8000, 0.012f, 0.6f); });

        // ---------------- ambience loops ----------------
        R("amb_graveyard", 9f, b =>
        {
            S.NoiseLayer(b, 0, 9f, t => 260 + 180 * Mathf.Sin(t * 6.3f), t => 40, S.Const(1f), 0.8f);
            S.NoiseLayer(b, 0, 9f, t => 900 + 500 * Mathf.Sin(t * 11f + 1f), t => 500, t => 0.3f + 0.3f * Mathf.Sin(t * 8f), 0.25f);
        }, true);
        R("amb_camp", 8f, b =>
        {
            S.NoiseLayer(b, 0, 8f, S.Const(220), t => 40, S.Const(1f), 0.6f);
            S.Clicks(b, 0, 7.6f, 90, 1500, 5000, 0.015f, 0.6f);
            S.NoiseLayer(b, 0, 8f, t => 500 + 200 * Mathf.Sin(t * 9f), t => 200, S.Const(0.6f), 0.3f);
        }, true);
        R("amb_grove", 10f, b =>
        {
            S.Tone(b, 0, 10f, S.Const(55), S.Const(1f), 0.35f, 0, 0.2f, 0.02f);
            S.Tone(b, 0, 10f, S.Const(58.3f), S.Const(1f), 0.3f, 0, 0.13f, 0.02f);
            S.NoiseLayer(b, 0, 10f, t => 1200 + 800 * Mathf.Sin(t * 17f), t => 600, t => 0.2f + 0.35f * Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 9.4f)), 3f), 0.35f);
            S.Tone(b, 0, 10f, t => 880 + 30 * Mathf.Sin(t * 40f), t => 0.15f * Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 6.28f)), 6f), 0.12f);
        }, true);
        R("amb_taiga", 9f, b =>
        {
            S.NoiseLayer(b, 0, 9f, t => 700 + 600 * Mathf.Sin(t * 7.5f), t => 150, t => 0.6f + 0.4f * Mathf.Sin(t * 12.6f), 0.8f);
            S.NoiseLayer(b, 0, 9f, t => 3500, t => 1800, t => 0.2f + 0.2f * Mathf.Sin(t * 18f), 0.2f);
        }, true);
        R("amb_castle", 9f, b =>
        {
            S.Tone(b, 0, 9f, S.Const(61), S.Const(1f), 0.3f);
            S.NoiseLayer(b, 0, 9f, S.Const(350), t => 60, S.Const(1f), 0.35f);
            S.NoiseLayer(b, 0, 9f, t => 700 + 300 * Mathf.Sin(t * 6.28f), t => 300, S.Const(0.4f), 0.15f);
        }, true);
        R("amb_throne", 10f, b =>
        {
            foreach (float f in new[] { 55f, 82.4f, 110f, 130.8f })
                S.Tone(b, 0, 10f, S.Const(f), t => 0.7f + 0.3f * Mathf.Sin(t * 6.28f * 2f + f), 0.18f, 0, 0.3f, 0.01f);
            S.NoiseLayer(b, 0, 10f, S.Const(300), t => 50, S.Const(1f), 0.25f);
        }, true);
        R("amb_menu", 9f, b =>
        {
            S.NoiseLayer(b, 0, 9f, t => 300 + 150 * Mathf.Sin(t * 6.3f), t => 40, S.Const(1f), 0.6f);
            S.Clicks(b, 0, 8.6f, 40, 1500, 4500, 0.015f, 0.3f);
        }, true);
    }
}

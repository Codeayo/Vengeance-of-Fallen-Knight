using UnityEngine;

/// <summary>
/// Small procedural UI sounds (hover tick, confirm, back). The project has no UI sound assets,
/// so these are synthesized once at startup. Assign real clips to override them.
/// </summary>
public class UIAudio : MonoBehaviour
{
    private static UIAudio instance;

    private AudioSource source;
    private AudioClip hoverClip;
    private AudioClip confirmClip;
    private AudioClip backClip;
    private float lastHoverTime;

    private static UIAudio Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[UIAudio]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<UIAudio>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;

        hoverClip = Synth("UI Hover", 0.045f, t => Tone(t, 1250f, 1500f) * Env(t, 0.045f) * 0.22f);
        confirmClip = Synth("UI Confirm", 0.28f, t =>
            (Tone(t, 330f, 330f) * 0.6f + Tone(t, 495f, 495f) * 0.4f + Tone(t, 660f, 660f) * 0.25f) * Env(t, 0.28f) * 0.35f);
        backClip = Synth("UI Back", 0.16f, t => Tone(t, 520f, 300f) * Env(t, 0.16f) * 0.28f);
    }

    public static void Hover()
    {
        UIAudio a = Instance;
        if (Time.unscaledTime - a.lastHoverTime < 0.05f)
            return;
        a.lastHoverTime = Time.unscaledTime;
        a.Play(a.hoverClip);
    }

    public static void Confirm() => Instance.Play(Instance.confirmClip);
    public static void Back() => Instance.Play(Instance.backClip);

    private void Play(AudioClip clip)
    {
        source.PlayOneShot(clip, GameSettings.SfxVolume);
    }

    private static float Tone(float t, float f0, float f1)
    {
        // Linear chirp: phase = 2π (f0 t + (f1-f0) t² / 2T) – approximated with a short fixed T.
        float f = Mathf.Lerp(f0, f1, Mathf.Clamp01(t * 6f));
        return Mathf.Sin(2f * Mathf.PI * f * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * f * t);
    }

    private static float Env(float t, float length)
    {
        float attack = Mathf.Clamp01(t / 0.004f);
        float decay = Mathf.Exp(-5f * t / length);
        return attack * decay;
    }

    private static AudioClip Synth(string name, float length, System.Func<float, float> wave)
    {
        const int rate = 44100;
        int count = Mathf.CeilToInt(rate * length);
        var data = new float[count];
        for (int i = 0; i < count; i++)
            data[i] = Mathf.Clamp(wave(i / (float)rate), -1f, 1f);

        AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The game's single audio system: chapter music with crossfades, boss and cinematic music,
/// chapter ambience loops with random one-shots, and a pooled sound-effect player with
/// distance falloff, panning and spam protection. Volumes: Master, Music, Effects, Ambience.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Music Tracks (legacy, used when the catalog has no entry)")]
    public AudioClip menuTrack;
    public AudioClip graveyardTrack;
    public AudioClip woodcampTrack;
    public AudioClip groveTrack;
    public AudioClip taigaTrack;
    public AudioClip castleTrack;
    public AudioClip throneTrack;
    public AudioClip creditsTrack;

    [Header("UI Sliders (optional)")]
    public Slider musicSlider;
    public Slider sfxSlider;

    private AudioSource musicB;
    private AudioSource ambientA, ambientB;
    private readonly List<AudioSource> pool = new List<AudioSource>();
    private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    private Coroutine musicFade, ambientFade, oneShots;
    private AudioClip sceneMusic;
    private bool overridden;
    private float musicDuck = 1f;
    private string footstepKey = "step_dirt";

    public string FootstepKey => footstepKey;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSources();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ApplyVolumes();
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null) musicSource = GetComponent<AudioSource>();
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null || sfxSource == musicSource) sfxSource = gameObject.AddComponent<AudioSource>();
        musicB = gameObject.AddComponent<AudioSource>();
        ambientA = gameObject.AddComponent<AudioSource>();
        ambientB = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { musicSource, musicB, ambientA, ambientB })
        {
            s.playOnAwake = false; s.loop = true; s.spatialBlend = 0f; s.volume = 0f; s.Stop();
        }
        sfxSource.playOnAwake = false; sfxSource.loop = false; sfxSource.spatialBlend = 0f;
        for (int i = 0; i < 16; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false; s.spatialBlend = 0f;
            pool.Add(s);
        }
    }

    // ---------------- scenes ----------------
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this == null) return;
        var cat = AudioCatalog.Instance;
        AudioCatalog.SceneAudio sa = cat != null ? cat.ForScene(scene.name) : default;
        sceneMusic = sa.music != null ? sa.music : LegacyTrack(scene.name);
        footstepKey = string.IsNullOrEmpty(sa.footstep) ? "step_dirt" : sa.footstep;
        overridden = false;
        musicDuck = 1f;
        CrossfadeMusic(sceneMusic, 2.5f);
        SetAmbience(sa.ambience, 3f);
        if (oneShots != null) StopCoroutine(oneShots);
        if (sa.ambientOneShots != null && sa.ambientOneShots.Length > 0)
            oneShots = StartCoroutine(AmbientOneShots(sa.ambientOneShots, Mathf.Max(3f, sa.oneShotMinGap), Mathf.Max(sa.oneShotMinGap + 1f, sa.oneShotMaxGap)));
    }

    private AudioClip LegacyTrack(string sceneName)
    {
        switch (sceneName)
        {
            case "Graveyard": return graveyardTrack;
            case "Woodcamp": return woodcampTrack;
            case "Grove": return groveTrack;
            case "Taiga": return taigaTrack;
            case "Castle": return castleTrack;
            case "ThroneRoom": return throneTrack;
            case "Credits": return creditsTrack;
            default: return menuTrack;
        }
    }

    // ---------------- music ----------------
    public void CrossfadeMusic(AudioClip clip, float seconds)
    {
        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(MusicFade(clip, seconds));
    }

    /// <summary>Boss / cinematic music; RestoreSceneMusic() returns to the chapter track.</summary>
    public void OverrideMusic(AudioClip clip, float seconds = 1.5f)
    {
        overridden = true;
        CrossfadeMusic(clip, seconds);
    }

    public void RestoreSceneMusic(float seconds = 3f)
    {
        overridden = false;
        CrossfadeMusic(sceneMusic, seconds);
    }

    public void PlayBossMusic(string bossName) => OverrideMusic(AudioCatalog.Instance != null ? AudioCatalog.Instance.ForBoss(bossName) : null, 1.2f);

    /// <summary>Temporarily lowers music (for dialogue and cinematics).</summary>
    public void Duck(float amount) { musicDuck = Mathf.Clamp01(amount); ApplyVolumes(); }

    private IEnumerator MusicFade(AudioClip clip, float seconds)
    {
        AudioSource from = musicSource.isPlaying && musicSource.volume > 0.001f ? musicSource : musicB;
        AudioSource to = from == musicSource ? musicB : musicSource;
        if (from.clip == clip && from.isPlaying) yield break;
        if (clip != null)
        {
            to.clip = clip;
            to.volume = 0f;
            to.Play();
        }

        float t = 0f, startFrom = from.volume;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            from.volume = startFrom * (1f - k);
            if (clip != null) to.volume = TargetMusic * k;
            yield return null;
        }

        from.Stop();
        from.volume = 0f;
        if (clip != null) to.volume = TargetMusic;
        // Keep musicSource as the active one so older scripts that read it keep working.
        if (clip != null && to != musicSource)
        {
            var tmp = musicSource; musicSource = musicB; musicB = tmp;
        }
        musicFade = null;
    }

    private float TargetMusic => GameSettings.MusicVolume * musicDuck;

    // ---------------- ambience ----------------
    public void SetAmbience(string key, float seconds)
    {
        AudioClip clip = string.IsNullOrEmpty(key) ? null : SoundLibrary.Get(key);
        if (ambientFade != null) StopCoroutine(ambientFade);
        ambientFade = StartCoroutine(AmbientFade(clip, seconds));
    }

    private IEnumerator AmbientFade(AudioClip clip, float seconds)
    {
        AudioSource from = ambientA.isPlaying ? ambientA : ambientB;
        AudioSource to = from == ambientA ? ambientB : ambientA;
        if (from.clip == clip && from.isPlaying) yield break;
        if (clip != null) { to.clip = clip; to.volume = 0f; to.Play(); }
        float t = 0f, startFrom = from.volume;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            from.volume = startFrom * (1f - k);
            if (clip != null) to.volume = GameSettings.AmbientVolume * k;
            yield return null;
        }
        from.Stop();
        if (clip != null) to.volume = GameSettings.AmbientVolume;
        ambientFade = null;
    }

    private IEnumerator AmbientOneShots(string[] keys, float minGap, float maxGap)
    {
        yield return new WaitForSeconds(Random.Range(2f, minGap));
        while (true)
        {
            string key = keys[Random.Range(0, keys.Length)];
            Vector3 cam = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            Vector3 at = cam + new Vector3(Random.Range(-14f, 14f), Random.Range(2f, 8f), 0f);
            PlayAt(key, at, 0.5f * GameSettings.AmbientVolume / Mathf.Max(0.01f, GameSettings.SfxVolume), 0.1f, 0f);
            yield return new WaitForSeconds(Random.Range(minGap, maxGap));
        }
    }

    // ---------------- effects ----------------
    /// <summary>Plays a library sound without position (UI, player, cinematic).</summary>
    public void Play(string key, float volume = 1f, float pitchVariance = 0.06f, float cooldown = 0.04f)
    {
        PlayInternal(key, volume, pitchVariance, cooldown, 0f);
    }

    /// <summary>Plays a library sound at a world position: quieter with distance from the camera, panned left/right.</summary>
    public void PlayAt(string key, Vector3 world, float volume = 1f, float pitchVariance = 0.08f, float cooldown = 0.05f, float maxDistance = 22f, float basePitch = 1f)
    {
        Camera cam = Camera.main;
        if (cam == null) { PlayInternal(key, volume, pitchVariance, cooldown, 0f, basePitch); return; }
        Vector2 d = world - cam.transform.position;
        float dist = d.magnitude;
        if (dist > maxDistance) return;
        float att = Mathf.Clamp01(1f - Mathf.Pow(dist / maxDistance, 1.6f));
        PlayInternal(key, volume * att, pitchVariance, cooldown, Mathf.Clamp(d.x / 14f, -0.8f, 0.8f), basePitch);
    }

    private void PlayInternal(string key, float volume, float pitchVariance, float cooldown, float pan, float basePitch = 1f)
    {
        if (volume <= 0.01f) return;
        if (lastPlayed.TryGetValue(key, out float last) && Time.unscaledTime - last < cooldown) return;
        AudioClip clip = SoundLibrary.Get(key);
        if (clip == null) return;
        lastPlayed[key] = Time.unscaledTime;

        AudioSource src = null;
        foreach (var s in pool) if (!s.isPlaying) { src = s; break; }
        if (src == null) src = pool[Random.Range(0, pool.Count)]; // steal a voice rather than pile up
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume) * GameSettings.SfxVolume;
        src.pitch = basePitch * (1f + Random.Range(-pitchVariance, pitchVariance));
        src.panStereo = pan;
        src.ignoreListenerPause = true;
        src.Play();
    }

    // ---------------- legacy API ----------------
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, GameSettings.SfxVolume);
    }

    public void PlayPickupSfx() => Play("pickup", 0.8f);
    public void PlayHealSfx() => Play("heal", 0.8f);

    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(GameSettings.MusicKey, Mathf.Clamp01(volume));
        ApplyVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat(GameSettings.SfxKey, Mathf.Clamp01(volume));
        ApplyVolumes();
    }

    public void ApplyVolumes()
    {
        AudioListener.volume = GameSettings.MasterVolume;
        if (musicFade == null)
        {
            if (musicSource != null && musicSource.isPlaying) musicSource.volume = TargetMusic;
        }
        if (ambientFade == null)
        {
            foreach (var a in new[] { ambientA, ambientB })
                if (a != null && a.isPlaying) a.volume = GameSettings.AmbientVolume;
        }
        if (sfxSource != null) sfxSource.volume = GameSettings.SfxVolume;
    }
}

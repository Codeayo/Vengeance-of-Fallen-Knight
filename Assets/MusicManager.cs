using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Music Tracks")]
    public AudioClip menuTrack;
    public AudioClip graveyardTrack;
    public AudioClip woodcampTrack;
    public AudioClip groveTrack;
    public AudioClip taigaTrack;
    public AudioClip castleTrack;
    public AudioClip throneTrack;
    public AudioClip creditsTrack;

    [Header("UI Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";
    private AudioClip pickupChime;
    private AudioClip healChime;

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
        pickupChime = CreateChime("Pickup Chime", 740f, 1180f, 0.16f);
        healChime = CreateChime("Healing Chime", 440f, 880f, 0.24f);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        LoadVolumeSettings();

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(SetMusicVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        PlaySceneTrack(SceneManager.GetActiveScene().name);
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        if (sfxSource == null || sfxSource == musicSource)
            sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.pitch = 1f;
        musicSource.spatialBlend = 0f;
        musicSource.Stop();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneTrack(scene.name);
    }

    private void PlaySceneTrack(string sceneName)
    {
        AudioClip track = GetTrackForScene(sceneName);
        if (track == null)
        {
            Debug.LogWarning($"No soundtrack is assigned for scene '{sceneName}'.", this);
            return;
        }

        if (musicSource.clip == track && musicSource.isPlaying)
            return;

        musicSource.Stop();
        musicSource.clip = track;
        musicSource.Play();
    }

    private AudioClip GetTrackForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "Main Menu":
            case "MainMenu":
                return menuTrack;
            case "Graveyard":
                return graveyardTrack;
            case "Woodcamp":
                return woodcampTrack;
            case "Grove":
                return groveTrack;
            case "Taiga":
                return taigaTrack;
            case "Castle":
                return castleTrack;
            case "ThroneRoom":
                return throneTrack;
            case "Credits":
                return creditsTrack;
            default:
                return menuTrack;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);

        if (Instance == this)
            Instance = null;
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp01(volume);

        PlayerPrefs.SetFloat(MUSIC_KEY, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
            sfxSource.volume = Mathf.Clamp01(volume);

        PlayerPrefs.SetFloat(SFX_KEY, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("Cannot play a missing sound effect clip.", this);
            return;
        }

        if (sfxSource == null)
            EnsureAudioSources();

        sfxSource.PlayOneShot(clip);
    }

    public void PlayPickupSfx()
    {
        PlaySFX(pickupChime);
    }

    public void PlayHealSfx()
    {
        PlaySFX(healChime);
    }

    private void LoadVolumeSettings()
    {
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_KEY, 0.35f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 0.55f);

        if (musicSource != null)
            musicSource.volume = musicVolume;

        if (sfxSource != null)
            sfxSource.volume = sfxVolume;

        if (musicSlider != null)
            musicSlider.value = musicVolume;

        if (sfxSlider != null)
            sfxSlider.value = sfxVolume;
    }

    private static AudioClip CreateChime(string clipName, float startFrequency, float endFrequency, float duration)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
            phase += 2f * Mathf.PI * frequency / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * t);
            samples[i] = Mathf.Sin(phase) * envelope * 0.3f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}

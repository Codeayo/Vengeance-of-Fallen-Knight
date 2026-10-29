using UnityEngine;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("UI Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    void Awake()
    {
        // Singleton so it persists between scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        LoadVolumeSettings();

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.pitch = 1f;
            if (!musicSource.isPlaying && musicSource.clip != null)
                musicSource.Play();
        }

        // Add listeners
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(SetMusicVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    private void OnDestroy()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }

    // --- Volume Controls ---
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = volume;

        PlayerPrefs.SetFloat(MUSIC_KEY, volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
            sfxSource.volume = volume;

        PlayerPrefs.SetFloat(SFX_KEY, volume);
    }

    // --- Load PlayerPrefs ---
    private void LoadVolumeSettings()
    {
        float musicVol = PlayerPrefs.GetFloat(MUSIC_KEY, 0.5f);
        float sfxVol = PlayerPrefs.GetFloat(SFX_KEY, 0.5f);

        if (musicSource != null)
            musicSource.volume = musicVol;

        if (sfxSource != null)
            sfxSource.volume = sfxVol;

        if (musicSlider != null) musicSlider.value = musicVol;
        if (sfxSlider != null) sfxSlider.value = sfxVol;
    }
}

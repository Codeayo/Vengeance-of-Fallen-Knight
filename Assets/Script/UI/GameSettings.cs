using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent player settings (audio, display). Applied automatically at startup.
/// Volume keys are shared with MusicManager so both stay in sync.
/// </summary>
public static class GameSettings
{
    public const string MusicKey = "MusicVolume";
    public const string SfxKey = "SFXVolume";
    private const string FullscreenKey = "Fullscreen";
    private const string ResolutionKey = "ResolutionIndex";
    private const string QualityKey = "QualityLevel";
    private const string VSyncKey = "VSync";

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 0.35f);
        set
        {
            PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
            if (MusicManager.Instance != null)
                MusicManager.Instance.SetMusicVolume(value);
        }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 0.55f);
        set
        {
            PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
            if (MusicManager.Instance != null)
                MusicManager.Instance.SetSFXVolume(value);
        }
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        set
        {
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            ApplyDisplay();
        }
    }

    public static bool VSync
    {
        get => PlayerPrefs.GetInt(VSyncKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0);
            QualitySettings.vSyncCount = value ? 1 : 0;
        }
    }

    public static int QualityLevel
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
        set
        {
            int level = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
            PlayerPrefs.SetInt(QualityKey, level);
            QualitySettings.SetQualityLevel(level, true);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
        }
    }

    /// <summary>Distinct resolutions supported by the display (largest last).</summary>
    public static List<Vector2Int> Resolutions
    {
        get
        {
            var list = new List<Vector2Int>();
            foreach (Resolution r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (!list.Contains(size))
                    list.Add(size);
            }

            if (list.Count == 0)
                list.Add(new Vector2Int(Screen.width, Screen.height));
            return list;
        }
    }

    public static int ResolutionIndex
    {
        get
        {
            var all = Resolutions;
            int saved = PlayerPrefs.GetInt(ResolutionKey, -1);
            if (saved >= 0 && saved < all.Count)
                return saved;

            int current = all.IndexOf(new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height));
            return current >= 0 ? current : all.Count - 1;
        }
        set
        {
            PlayerPrefs.SetInt(ResolutionKey, Mathf.Clamp(value, 0, Resolutions.Count - 1));
            ApplyDisplay();
        }
    }

    public static string ResolutionLabel(int index)
    {
        var all = Resolutions;
        if (index < 0 || index >= all.Count)
            return $"{Screen.width} x {Screen.height}";
        return $"{all[index].x} x {all[index].y}";
    }

    public static void Save() => PlayerPrefs.Save();

    private static void ApplyDisplay()
    {
#if !UNITY_EDITOR && !UNITY_WEBGL
        var all = Resolutions;
        Vector2Int size = all[Mathf.Clamp(ResolutionIndex, 0, all.Count - 1)];
        FullScreenMode mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(size.x, size.y, mode);
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnStartup()
    {
        if (PlayerPrefs.HasKey(QualityKey))
            QualitySettings.SetQualityLevel(QualityLevel, true);
        QualitySettings.vSyncCount = VSync ? 1 : 0;

        if (PlayerPrefs.HasKey(FullscreenKey) || PlayerPrefs.HasKey(ResolutionKey))
            ApplyDisplay();
    }
}

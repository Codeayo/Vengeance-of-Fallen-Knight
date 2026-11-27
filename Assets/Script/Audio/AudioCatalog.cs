using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Music for every scene and boss, plus optional recorded overrides for any synthesized effect.
/// Lives at Resources/AudioCatalog so the build always includes the clips.
/// </summary>
[CreateAssetMenu(menuName = "Vengeance/Audio Catalog")]
public class AudioCatalog : ScriptableObject
{
    [Serializable]
    public struct SceneAudio
    {
        public string scene;
        public AudioClip music;
        public string ambience;           // SoundLibrary loop key
        public string[] ambientOneShots;  // random one-shots (ravens, chains...)
        public float oneShotMinGap;
        public float oneShotMaxGap;
        public string footstep;           // footstep sound for this chapter's ground
    }

    [Serializable]
    public struct BossMusic { public string boss; public AudioClip music; }

    [Serializable]
    public struct SfxOverride { public string key; public AudioClip clip; }

    public List<SceneAudio> scenes = new List<SceneAudio>();
    public List<BossMusic> bosses = new List<BossMusic>();
    public AudioClip defaultBossMusic;
    public AudioClip openingMusic;
    public AudioClip epilogueMusic;
    public List<SfxOverride> overrides = new List<SfxOverride>();

    private static AudioCatalog instance;
    public static AudioCatalog Instance => instance != null ? instance : (instance = Resources.Load<AudioCatalog>("AudioCatalog"));

    public SceneAudio ForScene(string scene)
    {
        foreach (var s in scenes) if (s.scene == scene) return s;
        return default;
    }

    public AudioClip ForBoss(string boss)
    {
        foreach (var b in bosses) if (b.boss == boss && b.music != null) return b.music;
        return defaultBossMusic;
    }

    public AudioClip Sfx(string key)
    {
        foreach (var o in overrides) if (o.key == key) return o.clip;
        return null;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public static class GraveyardSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded += ConfigureScene;
    }

    private static void ConfigureScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Graveyard")
            return;


        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 8f;
        }

        MusicManager musicManager = Object.FindFirstObjectByType<MusicManager>();
        if (musicManager != null && musicManager.musicSource != null)
        {
            musicManager.musicSource.loop = true;
            musicManager.musicSource.pitch = 1f;
            if (!musicManager.musicSource.isPlaying && musicManager.musicSource.clip != null)
                musicManager.musicSource.Play();
        }
    }
}

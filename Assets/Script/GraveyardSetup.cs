using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

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

        CreateFallbackGround();

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

    private static void CreateFallbackGround()
    {
        TilemapCollider2D[] tilemapColliders = Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None);
        foreach (TilemapCollider2D tilemapCollider in tilemapColliders)
        {
            if (tilemapCollider.enabled && !tilemapCollider.isTrigger)
                return;
        }

        GameObject ground = new GameObject("Graveyard Ground Collision Fallback");
        ground.transform.position = new Vector2(230f, -1.2f);

        BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(500f, 0.5f);
    }
}

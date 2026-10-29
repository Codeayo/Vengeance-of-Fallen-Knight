using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExit : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    [SerializeField] private bool requirePlayerTag = true;
    private bool hasLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneExitBootstrap()
    {
        SceneManager.sceneLoaded += CreateSceneExit;
    }

    private static void CreateSceneExit(Scene scene, LoadSceneMode mode)
    {
        string nextSceneName = GetNextSceneName(scene.name);
        if (string.IsNullOrEmpty(nextSceneName) || FindFirstObjectByType<LevelExit>() != null)
            return;

        GameObject exitObject = new GameObject("Level Exit - " + nextSceneName);
        exitObject.transform.position = GetExitPosition(scene.name);

        BoxCollider2D exitCollider = exitObject.AddComponent<BoxCollider2D>();
        exitCollider.isTrigger = true;
        exitCollider.size = new Vector2(3f, 6f);

        LevelExit exit = exitObject.AddComponent<LevelExit>();
        exit.nextSceneName = nextSceneName;
    }

    private static string GetNextSceneName(string sceneName)
    {
        switch (sceneName)
        {
            case "Graveyard":
                return "Woodcamp";
            case "Woodcamp":
                return "Grove";
            case "Grove":
                return "Main Menu";
            default:
                return string.Empty;
        }
    }

    private static Vector2 GetExitPosition(string sceneName)
    {
        switch (sceneName)
        {
            case "Graveyard":
                return new Vector2(460f, 2.5f);
            case "Woodcamp":
                return new Vector2(120f, 2.5f);
            case "Grove":
                return new Vector2(72f, 2.5f);
            default:
                return Vector2.zero;
        }
    }

    public void Configure(string sceneName)
    {
        nextSceneName = sceneName;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasLoaded || other == null)
            return;

        if (requirePlayerTag && !other.CompareTag("Player"))
            return;

        hasLoaded = true;
        Time.timeScale = 1f;

        if (SceneManagement.Instance != null)
        {
            SceneManagement.Instance.LoadScene(nextSceneName);
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            hasLoaded = false;
            Debug.LogWarning($"Scene '{nextSceneName}' cannot be loaded. Check Build Settings and scene name.", this);
        }
    }
}

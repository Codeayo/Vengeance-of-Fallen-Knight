using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuLoader : MonoBehaviour
{
    private void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (SceneManager.GetActiveScene().name == sceneName)
        {
            Debug.LogWarning($"Already on '{sceneName}'. Not reloading the scene.");
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"Scene '{sceneName}' cannot be loaded. Check Build Settings and scene name.");
        }
    }

    public void LoadGraveyard()
    {
        LoadSceneByName("Graveyard");
    }

    public void LoadWoodcamp()
    {
        LoadSceneByName("Woodcamp");
    }

    public void LoadGrove()
    {
        LoadSceneByName("Grove");
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

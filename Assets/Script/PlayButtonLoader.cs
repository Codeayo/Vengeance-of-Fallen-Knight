using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButtonLoader : MonoBehaviour
{
    public void LoadGraveyard()
    {
        const string sceneName = "Graveyard";

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"Scene '{sceneName}' cannot be loaded. Check Build Settings and scene name.");
        }
    }
}

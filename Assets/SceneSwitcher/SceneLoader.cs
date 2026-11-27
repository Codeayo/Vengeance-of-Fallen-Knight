using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
   public void LoadSceneByName(string sceneName)
   {
       if (string.IsNullOrWhiteSpace(sceneName) ||
           !Application.CanStreamedLevelBeLoaded(sceneName))
       {
           Debug.LogWarning($"Scene '{sceneName}' cannot be loaded. Check Build Settings.", this);
           return;
       }

       SceneManager.LoadScene(sceneName);
   }
}
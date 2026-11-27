using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelLoader : MonoBehaviour
{
    [Header("Transition Settings")]
    public Animator transition;
    public float transitionTime = 1f;
    private bool isLoading;

    void Update()
    {
        // Debug shortcuts: only in the Editor and development builds.
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
        return;
#endif
#pragma warning disable CS0162
        if (Input.GetKeyDown(KeyCode.Alpha1)) LoadLevel("Main Menu");
        if (Input.GetKeyDown(KeyCode.Alpha2)) LoadLevel("Graveyard");
        if (Input.GetKeyDown(KeyCode.Alpha3)) LoadLevel("Woodcamp");
        if (Input.GetKeyDown(KeyCode.Alpha4)) LoadLevel("Grove");
        if (Input.GetKeyDown(KeyCode.Alpha5)) LoadLevel("Taiga");
        if (Input.GetKeyDown(KeyCode.Alpha6)) LoadLevel("Castle");
        if (Input.GetKeyDown(KeyCode.Alpha7)) LoadLevel("Credits");
#pragma warning restore CS0162
    }

    public void LoadLevel(string sceneName)
    {
        if (isLoading)
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"Scene '{sceneName}' cannot be loaded. Check Build Settings.", this);
            return;
        }

        if (SceneManager.GetActiveScene().name == sceneName)
            return;

        isLoading = true;
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        if (transition != null)
            transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime);

        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
        {
            isLoading = false;
            Debug.LogWarning($"Scene '{sceneName}' cannot be loaded. Check Build Settings.", this);
        }
    }
}

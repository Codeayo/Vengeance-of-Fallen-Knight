using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public string sceneToLoad;
    public KeyCode keyToPress = KeyCode.E;

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(sceneToLoad))
            Debug.LogWarning("SceneSwitcher has no target scene assigned; its key will be ignored.", this);
    }

    void Update()
    {
        // Debug level-skip key: only in the Editor and development builds.
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
        return;
#endif
#pragma warning disable CS0162
        if (!Input.GetKeyDown(keyToPress) || string.IsNullOrWhiteSpace(sceneToLoad))
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            Debug.LogWarning($"Scene '{sceneToLoad}' cannot be loaded. Check Build Settings.", this);
            return;
        }

        Debug.Log("Loading scene: " + sceneToLoad);
        SceneManager.LoadScene(sceneToLoad);
#pragma warning restore CS0162
    }
}

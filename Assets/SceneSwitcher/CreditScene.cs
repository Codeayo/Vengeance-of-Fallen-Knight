using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditScene : MonoBehaviour
{
    public float scrollSpeed = 40f;
    [SerializeField] private float returnToMenuAfterSeconds = 75f;
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private RectTransform rectTransform;
    private float startedAt;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startedAt = Time.unscaledTime;
    }

    void Update()
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

        bool skipRequested = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space);
        bool creditsFinished = returnToMenuAfterSeconds > 0f &&
                               Time.unscaledTime - startedAt >= returnToMenuAfterSeconds;
        if (!skipRequested && !creditsFinished)
            return;

        // The tale is complete: clear the finished run (chapters stay unlocked) and return to the menu.
        enabled = false;
        GameSave.NewRun();
        SceneFader.LoadScene(mainMenuSceneName);
    }
}

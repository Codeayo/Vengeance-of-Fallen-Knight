using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string playSceneName = "Graveyard";

    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject aboutPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button aboutButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button closeAboutButton;

    private void Awake()
    {
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);

        if (aboutButton != null)
            aboutButton.onClick.AddListener(OpenAbout);

        if (exitButton != null)
            exitButton.onClick.AddListener(QuitGame);

        if (closeAboutButton != null)
            closeAboutButton.onClick.AddListener(CloseAbout);

        CloseAbout();
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;

        if (SceneManagement.Instance != null)
        {
            SceneManagement.Instance.LoadScene(playSceneName);
            return;
        }

        if (SceneManager.GetActiveScene().name == playSceneName)
        {
            Debug.LogWarning($"Already on '{playSceneName}'. Not reloading the scene.");
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(playSceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(playSceneName);
        }
        else
        {
            Debug.LogWarning($"Scene '{playSceneName}' cannot be loaded. Check Build Settings and scene name.");
        }
    }

    public void OpenAbout()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (aboutPanel != null)
            aboutPanel.SetActive(true);
    }

    public void CloseAbout()
    {
        if (aboutPanel != null)
            aboutPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

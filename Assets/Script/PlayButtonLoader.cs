using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayButtonLoader : MonoBehaviour
{
    private GameObject mainMenuPanel;
    private GameObject aboutPanel;

    private void Awake()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        mainMenuPanel = FindPanel("Main menu Ui");
        aboutPanel = FindPanel("AboutMenuUI");
        if (aboutPanel == null)
            aboutPanel = FindPanel("AboutMenu");

        if (mainMenuPanel == null)
            Debug.LogWarning("Main menu panel was not found in the active scene.", this);
        if (aboutPanel == null)
            Debug.LogWarning("About panel was not found in the active scene.", this);

        Button playButton = FindButton("PlayButton");
        if (playButton == null)
        {
            Debug.LogWarning("Play button was not found in the menu canvas.", this);
        }
        else
        {
            bool hasValidPersistentListener = false;
            for (int i = 0; i < playButton.onClick.GetPersistentEventCount(); i++)
            {
                if (playButton.onClick.GetPersistentTarget(i) != null)
                {
                    hasValidPersistentListener = true;
                    break;
                }
            }

            if (!hasValidPersistentListener)
                playButton.onClick.AddListener(LoadGraveyard);

            Text playLabel = playButton.GetComponentInChildren<Text>(true);
            if (playLabel != null && GameSave.HasSavedGame)
                playLabel.text = "CONTINUE";
        }

        Button aboutButton = FindButton("AboutButton");
        if (aboutButton != null)
            aboutButton.onClick.AddListener(OpenAbout);
        else
            Debug.LogWarning("About button was not found in the menu canvas.", this);

        Button exitButton = FindButton("ExitButton");
        if (exitButton != null && exitButton.onClick.GetPersistentEventCount() == 0)
            exitButton.onClick.AddListener(QuitGame);
        else if (exitButton == null)
            Debug.LogWarning("Exit button was not found in the menu canvas.", this);

        Button backButton = FindButton("BackButton");
        if (backButton != null)
            backButton.onClick.AddListener(CloseAbout);

        CloseAbout();
    }

    public void LoadGraveyard()
    {
        Time.timeScale = 1f;
        string sceneName = GameSave.GetSavedSceneOr("Graveyard");

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"Scene '{sceneName}' cannot be loaded. Check Build Settings and scene name.");
        }
    }

    public void StartNewGame()
    {
        GameSave.ClearProgress();
        LoadGraveyard();
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

    private Button FindButton(string buttonName)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == buttonName)
                    return button;
            }
        }

        return null;
    }

    private GameObject FindPanel(string panelName)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == panelName)
                    return child.gameObject;
            }
        }

        return null;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Animator gameOverAnimator;
    [SerializeField] private Button playButton;
    [Header("Scene Transitions")]
    [SerializeField] private Animator startingTransition;
    [SerializeField] private string startingTrigger = "Start";
    [SerializeField] private Animator endingTransition;
    [SerializeField] private string endingTrigger = "Start";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (playButton == null)
            playButton = GetComponentInChildren<Button>();

        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);
    }

    public void TriggerGameOverBackground()
    {
        if (gameOverAnimator == null)
        {
            Debug.LogWarning("Game Over Animator is not assigned on GameManager.");
            return;
        }

        gameOverAnimator.SetTrigger("Show");
    }

    public void PlayStartingTransition()
    {
        if (startingTransition == null)
        {
            Debug.LogWarning("Starting transition Animator not assigned on GameManager.");
            return;
        }

        startingTransition.SetTrigger(startingTrigger);
    }

    public void PlayEndingTransition()
    {
        if (endingTransition == null)
        {
            Debug.LogWarning("Ending transition Animator not assigned on GameManager.");
            return;
        }

        endingTransition.SetTrigger(endingTrigger);
    }

    public void PlayGame()
    {
        string sceneName = "Graveyard";

        Debug.Log($"PlayGame called. Loading scene: {sceneName}");

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"{sceneName} scene cannot be loaded. Check Build Settings and scene name.");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

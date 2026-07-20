using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagement : MonoBehaviour
{
    public static SceneManagement Instance { get; private set; }

    [Header("Transition Settings")]
    [SerializeField] private GameObject fadeInObject;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float endScreenDelay = 2f;
    [SerializeField] private float loadNextSceneDelay = 1f;
    [SerializeField] private string nextSceneName;
    [SerializeField] private bool fadeInOnStart = true;
    [Space]
    [Tooltip("Animator used for the starting transition (e.g. intro fade)")]
    [SerializeField] private Animator startingTransition;
    [SerializeField] private string startingTrigger = "Start";
    [Tooltip("Animator used for the ending transition (e.g. exit/fade out)")]
    [SerializeField] private Animator endingTransition;
    [SerializeField] private string endingTrigger = "Start";
    [SerializeField] private float transitionTime = 1f;
    [Header("Input")]
    [SerializeField] private bool spaceToLoadNext = true;
    [SerializeField] private bool keyToLoadNext = true;
    [SerializeField] private KeyCode loadNextKey = KeyCode.N;
    private bool isLoading = false;
    private Coroutine fadeRoutine;

    [Header("Level Names")]
    [SerializeField] private string graveyardSceneName = "Graveyard";
    [SerializeField] private string woodcampSceneName = "Woodcamp";
    [SerializeField] private string groveSceneName = "Grove";
    [SerializeField] private string mainMenuSceneName = "Main Menu";

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        ResolveCanvasGroup();

        if (fadeInOnStart)
            FadeIn();

        if (startingTransition != null)
        {
            StartCoroutine(StartingTransitionRoutine());
        }
    }

    private void ResolveCanvasGroup()
    {
        if (canvasGroup != null)
            return;

        GameObject targetObject = fadeInObject != null ? fadeInObject : gameObject;

        if (targetObject != null)
        {
            canvasGroup = targetObject.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = targetObject.AddComponent<CanvasGroup>();
                Debug.Log($"Added CanvasGroup to {targetObject.name} for fade-in.");
            }
        }
    }

    public void FadeIn()
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("No CanvasGroup found for fade-in.");
            return;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeInRoutine());
    }

    public void LoadNextScene()
    {
        if (isLoading) return;

        string sceneToLoad = nextSceneName;

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("No next scene name was set. Falling back to Graveyard.");
            sceneToLoad = graveyardSceneName;
        }

        LoadScene(sceneToLoad);
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading) return;
        isLoading = true;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("No scene name was provided. Falling back to the next configured scene.");
            sceneName = string.IsNullOrEmpty(nextSceneName) ? graveyardSceneName : nextSceneName;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isLoading = false;
        ResolveCanvasGroup();

        if (fadeInOnStart)
            FadeIn();
    }

    private void Update()
    {
        if (isLoading) return;

        if (spaceToLoadNext && Input.GetKeyDown(KeyCode.Space))
        {
            LoadNextScene();
            return;
        }

        if (keyToLoadNext && Input.GetKeyDown(loadNextKey))
        {
            LoadNextScene();
        }
    }

    public void LoadMainMenu()
    {
        LoadScene(mainMenuSceneName);
    }

    public void LoadGraveyard()
    {
        LoadScene(graveyardSceneName);
    }

    public void LoadWoodcamp()
    {
        LoadScene(woodcampSceneName);
    }

    public void LoadGrove()
    {
        LoadScene(groveSceneName);
    }

    public void ShowEndScreenAndLoadNext()
    {
        StartCoroutine(EndScreenThenLoadNextRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public void PlayStartingTransition()
    {
        if (startingTransition != null)
            startingTransition.SetTrigger(startingTrigger);
    }

    private IEnumerator StartingTransitionRoutine()
    {
        startingTransition.SetTrigger(startingTrigger);
        yield return new WaitForSeconds(transitionTime);
    }

    public void PlayEndingTransition()
    {
        if (endingTransition != null)
            endingTransition.SetTrigger(endingTrigger);
    }

    private IEnumerator EndScreenThenLoadNextRoutine()
    {
        yield return new WaitForSeconds(endScreenDelay);

        if (endingTransition != null)
        {
            endingTransition.SetTrigger(endingTrigger);
            yield return new WaitForSeconds(transitionTime);
        }

        yield return new WaitForSeconds(loadNextSceneDelay);
        LoadNextScene();
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (endingTransition != null)
        {
            endingTransition.SetTrigger(endingTrigger);
            yield return new WaitForSeconds(transitionTime);
        }
        else
        {
            yield return new WaitForSeconds(loadNextSceneDelay);
        }

        if (!TryResolveSceneName(sceneName, out string resolvedSceneName))
        {
            isLoading = false;
            Debug.LogWarning($"Scene '{sceneName}' could not be resolved. Check the scene name or Build Settings.");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(resolvedSceneName))
        {
            isLoading = false;
            Debug.LogWarning($"Scene '{resolvedSceneName}' cannot be loaded. Add it to Build Settings or check the scene name.");
            yield break;
        }

        SceneManager.LoadScene(resolvedSceneName);
    }

    private bool TryResolveSceneName(string sceneName, out string resolvedSceneName)
    {
        resolvedSceneName = string.Empty;

        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        string trimmedName = sceneName.Trim();

        if (trimmedName.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
            trimmedName = Path.GetFileNameWithoutExtension(trimmedName);

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string buildSceneName = Path.GetFileNameWithoutExtension(scenePath);

            if (string.Equals(buildSceneName, trimmedName, System.StringComparison.OrdinalIgnoreCase))
            {
                resolvedSceneName = buildSceneName;
                return true;
            }
        }

        resolvedSceneName = trimmedName;
        return false;
    }
}
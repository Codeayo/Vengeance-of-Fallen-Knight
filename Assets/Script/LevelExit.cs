using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExit : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    [SerializeField] private bool requirePlayerTag = true;
    [Tooltip("The exit stays sealed until every one of these is destroyed (e.g. the final encounter).")]
    [SerializeField] private GameObject[] requiredDefeated;
    [SerializeField] private string sealedMessage = "The gate is sealed by the Gravewarden's curse";
    [SerializeField] private GameObject sealedVisual;
    [SerializeField] private GameObject openVisual;
    private bool hasLoaded;
    private bool wasOpen;
    private float lastSealedMessage = -10f;

    public bool IsOpen
    {
        get
        {
            if (requiredDefeated == null)
                return true;
            foreach (GameObject g in requiredDefeated)
            {
                if (g == null || !g.activeInHierarchy)
                    continue;
                EnemyCore core = g.GetComponent<EnemyCore>();
                if (core == null || !core.IsDead)
                    return false;
            }
            return true;
        }
    }

    private void Start()
    {
        wasOpen = IsOpen;
        RefreshVisuals(wasOpen);
    }

    private void Update()
    {
        bool open = IsOpen;
        if (open == wasOpen)
            return;
        wasOpen = open;
        RefreshVisuals(open);
        if (open)
            GameHUD.Instance?.ShowToast("The way forward is open");
    }

    private void RefreshVisuals(bool open)
    {
        if (sealedVisual != null) sealedVisual.SetActive(!open);
        if (openVisual != null) openVisual.SetActive(open);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneExitBootstrap()
    {
        SceneManager.sceneLoaded += CreateSceneExit;
    }

    private static void CreateSceneExit(Scene scene, LoadSceneMode mode)
    {
        string nextSceneName = GetNextSceneName(scene.name);
        if (string.IsNullOrEmpty(nextSceneName) || FindFirstObjectByType<LevelExit>() != null)
            return;

        GameObject exitObject = new GameObject("Level Exit - " + nextSceneName);
        exitObject.transform.position = GetExitPosition(scene.name);

        BoxCollider2D exitCollider = exitObject.AddComponent<BoxCollider2D>();
        exitCollider.isTrigger = true;
        exitCollider.size = new Vector2(3f, 6f);

        LevelExit exit = exitObject.AddComponent<LevelExit>();
        exit.nextSceneName = nextSceneName;
    }

    private static string GetNextSceneName(string sceneName)
    {
        switch (sceneName)
        {
            case "Graveyard":
                return "Woodcamp";
            case "Woodcamp":
                return "Grove";
            case "Grove":
                return "Taiga";
            case "Taiga":
                return "Castle";
            case "Castle":
                return "ThroneRoom";
            case "ThroneRoom":
                return "Credits";
            default:
                return string.Empty;
        }
    }

    private static Vector2 GetExitPosition(string sceneName)
    {
        switch (sceneName)
        {
            case "Graveyard":
                return new Vector2(110f, 2.5f);
            case "Woodcamp":
                return new Vector2(88f, 2.5f);
            case "Grove":
            case "Taiga":
            case "Castle":
            case "ThroneRoom":
                return new Vector2(190f, 2.5f);
            default:
                return Vector2.zero;
        }
    }

    public void Configure(string sceneName)
    {
        nextSceneName = sceneName;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryExit(other);
    private void OnTriggerStay2D(Collider2D other) => TryExit(other);

    private void TryExit(Collider2D other)
    {
        if (hasLoaded || other == null)
            return;

        if (requirePlayerTag && !other.CompareTag("Player"))
            return;

        if (!IsOpen)
        {
            if (Time.time - lastSealedMessage > 4f)
            {
                lastSealedMessage = Time.time;
                GameHUD.Instance?.ShowToast(sealedMessage);
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName) ||
            !Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogWarning($"Next scene '{nextSceneName}' cannot be loaded. Check Build Settings.", this);
            return;
        }

        hasLoaded = true;
        Time.timeScale = 1f;

        Player player = other.GetComponentInParent<Player>();
        Player_Collectables collectables = other.GetComponentInParent<Player_Collectables>();
        GameSave.SaveProgress(
            nextSceneName,
            collectables != null ? collectables.CurrentCoins : GameSave.LoadCoins(),
            player != null ? player.CurrentHealth : GameSave.LoadHealth(100),
            collectables != null ? collectables.HealingFlasks : GameSave.LoadHealingFlasks()
        );

        SceneFader.LoadScene(nextSceneName);
    }
}

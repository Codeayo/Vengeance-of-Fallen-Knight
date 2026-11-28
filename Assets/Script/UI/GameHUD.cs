using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// In-level HUD: health bar with damage trail, coin and flask counters, area title cards,
/// toasts, the respawn fade and the Game Over screen.
/// </summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    [Header("Health")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Image healthTrail;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private RectTransform healthFrame;
    [SerializeField] private Gradient healthColors;

    [Header("Experience")]
    [SerializeField] private Image xpFill;
    [SerializeField] private TMP_Text levelText;

    [Header("Pickup Feed")]
    [SerializeField] private RectTransform feedRoot;
    [SerializeField] private GameObject feedTemplate;

    [Header("Interaction")]
    [SerializeField] private CanvasGroup promptGroup;
    [SerializeField] private TMP_Text promptText;

    [Header("Counters")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text flaskText;

    [Header("Messages")]
    [SerializeField] private CanvasGroup areaTitleGroup;
    [SerializeField] private TMP_Text areaTitle;
    [SerializeField] private TMP_Text areaSubtitle;
    [SerializeField] private CanvasGroup toastGroup;
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private CanvasGroup controlsHint;
    [SerializeField] private float controlsHintSeconds = 9f;

    [Header("Boss")]
    [SerializeField] private CanvasGroup bossGroup;
    [SerializeField] private Image bossFill;
    [SerializeField] private Image bossTrail;
    [SerializeField] private TMP_Text bossName;

    [Header("Screens")]
    [SerializeField] private CanvasGroup screenFade;
    [SerializeField] private CanvasGroup gameOverGroup;
    [SerializeField] private Button riseAgainButton;
    [SerializeField] private Button gameOverMenuButton;
    [SerializeField] private string mainMenuScene = "Main Menu";

    private Player player;
    private Player_Collectables collectables;
    private float shownHealth = 1f;
    private float trailHealth = 1f;
    private float targetHealth = 1f;
    private float trailDelay;
    private float shake;
    private Vector2 frameBasePos;
    private Coroutine toastRoutine;
    private Coroutine titleRoutine;

    private void Awake()
    {
        Instance = this;
        SetGroup(areaTitleGroup, 0f, false);
        SetGroup(toastGroup, 0f, false);
        SetGroup(screenFade, 0f, false);
        SetGroup(gameOverGroup, 0f, false);
        SetGroup(bossGroup, 0f, false);
        if (healthFrame != null)
            frameBasePos = healthFrame.anchoredPosition;

        if (riseAgainButton != null)
            riseAgainButton.onClick.AddListener(RiseAgain);
        if (gameOverMenuButton != null)
            gameOverMenuButton.onClick.AddListener(() => { UIAudio.Back(); SceneFader.LoadScene(mainMenuScene); });
    }

    private void OnEnable()
    {
        PlayerProgress.Changed += OnProgress;
        Inventory.Collected += OnCollected;
    }

    private void OnDisable()
    {
        PlayerProgress.Changed -= OnProgress;
        Inventory.Collected -= OnCollected;
    }

    private void OnProgress()
    {
        if (xpFill != null)
            xpFill.fillAmount = PlayerProgress.XpToNext > 0 ? PlayerProgress.Xp / (float)PlayerProgress.XpToNext : 1f;
        if (levelText != null)
            levelText.text = $"LV {PlayerProgress.Level}";
        if (player != null)
            OnHealthChanged(player.CurrentHealth, player.MaxHealth);
    }

    private void OnCollected(ItemDefinition def, int amount)
    {
        if (feedRoot == null || feedTemplate == null || def == null)
            return;
        GameObject row = Instantiate(feedTemplate, feedRoot);
        row.SetActive(true);
        row.transform.SetAsLastSibling();
        var icon = row.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null) { icon.sprite = def.icon; icon.color = def.iconTint; }
        var label = row.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (label != null)
        {
            string hex = ColorUtility.ToHtmlStringRGB(ItemDefinition.RarityColor(def.rarity));
            label.text = $"<color=#{hex}>{def.displayName}</color>" + (amount > 1 ? $"  x{amount}" : "");
        }
        StartCoroutine(FeedRow(row.GetComponent<CanvasGroup>()));
        while (feedRoot.childCount > 6)
            DestroyImmediate(feedRoot.GetChild(1).gameObject);
    }

    private IEnumerator FeedRow(CanvasGroup g)
    {
        if (g == null) yield break;
        g.alpha = 0f;
        yield return FadeGroup(g, 1f, 0.2f);
        yield return new WaitForSecondsRealtime(3f);
        yield return FadeGroup(g, 0f, 0.8f);
        if (g != null) Destroy(g.gameObject);
    }

    public void SetPrompt(string text)
    {
        if (promptGroup == null) return;
        bool show = !string.IsNullOrEmpty(text);
        if (show && promptText != null) promptText.text = text;
        promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, show ? 1f : 0f, 1f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (player != null)
            player.HealthChanged -= OnHealthChanged;
        if (collectables != null)
            collectables.CountsChanged -= OnCountsChanged;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.GetComponent<Player>();
            collectables = p.GetComponent<Player_Collectables>();
        }

        if (player != null)
        {
            player.HealthChanged += OnHealthChanged;
            OnHealthChanged(player.CurrentHealth > 0 ? player.CurrentHealth : player.MaxHealth, player.MaxHealth);
            shownHealth = trailHealth = targetHealth;
        }

        if (collectables != null)
        {
            collectables.CountsChanged += OnCountsChanged;
            OnCountsChanged(collectables.CurrentCoins, collectables.HealingFlasks);
        }

        if (controlsHint != null)
            StartCoroutine(HideControlsHint());
        OnProgress();
        SetPrompt(null);
    }

    private CanvasGroup[] gameplayGroups;

    private void Update()
    {
        // Gameplay HUD steps aside while a cinematic plays.
        if (gameplayGroups == null)
        {
            var list = new System.Collections.Generic.List<CanvasGroup>();
            foreach (string n in new[] { "Status", "Pickup Feed", "Controls" })
            {
                var t = transform.Find(n);
                if (t == null) continue;
                var g = t.GetComponent<CanvasGroup>();
                if (g == null) g = t.gameObject.AddComponent<CanvasGroup>();
                if (n != "Controls") list.Add(g);
            }
            gameplayGroups = list.ToArray();
        }
        float hudTarget = Cinematic.IsPlaying ? 0f : 1f;
        foreach (var g in gameplayGroups)
            if (g != null) g.alpha = Mathf.MoveTowards(g.alpha, hudTarget, Time.unscaledDeltaTime * 3f);

        float dt = Time.unscaledDeltaTime;
        shownHealth = Mathf.MoveTowards(shownHealth, targetHealth, dt * 2.5f);
        if (trailDelay > 0f)
            trailDelay -= dt;
        else
            trailHealth = Mathf.MoveTowards(trailHealth, shownHealth, dt * 0.8f);
        if (trailHealth < shownHealth)
            trailHealth = shownHealth;

        if (healthFill != null)
        {
            healthFill.fillAmount = shownHealth;
            if (healthColors != null)
                healthFill.color = healthColors.Evaluate(shownHealth);
        }

        if (healthTrail != null)
            healthTrail.fillAmount = trailHealth;

        if (healthFrame != null)
        {
            shake = Mathf.MoveTowards(shake, 0f, dt * 30f);
            healthFrame.anchoredPosition = frameBasePos + Random.insideUnitCircle * shake;
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        float value = max > 0 ? Mathf.Clamp01(current / (float)max) : 0f;
        if (value < targetHealth)
        {
            trailDelay = 0.45f;
            shake = 6f;
        }

        targetHealth = value;
        if (healthText != null)
            healthText.text = $"{current} / {max}";
    }

    private void OnCountsChanged(int coins, int flasks)
    {
        if (coinText != null)
            coinText.text = coins.ToString();
        if (flaskText != null)
            flaskText.text = $"x{flasks}";
    }

    // ---------------- MESSAGES ----------------
    public void ShowAreaTitle(string title, string subtitle)
    {
        if (areaTitleGroup == null)
            return;
        if (titleRoutine != null)
            StopCoroutine(titleRoutine);
        titleRoutine = StartCoroutine(AreaTitleRoutine(title, subtitle));
    }

    private IEnumerator AreaTitleRoutine(string title, string subtitle)
    {
        if (areaTitle != null) areaTitle.text = title;
        if (areaSubtitle != null) areaSubtitle.text = subtitle;
        yield return FadeGroup(areaTitleGroup, 1f, 1.1f);
        yield return new WaitForSecondsRealtime(2.6f);
        yield return FadeGroup(areaTitleGroup, 0f, 1.4f);
        titleRoutine = null;
    }

    public void ShowToast(string message)
    {
        if (toastGroup == null || toastText == null)
            return;
        if (toastRoutine != null)
            StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastText.text = message;
        yield return FadeGroup(toastGroup, 1f, 0.25f);
        yield return new WaitForSecondsRealtime(2.4f);
        yield return FadeGroup(toastGroup, 0f, 0.6f);
        toastRoutine = null;
    }

    private IEnumerator HideControlsHint()
    {
        SetGroup(controlsHint, 0f, false);
        yield return new WaitForSecondsRealtime(1.2f);
        yield return FadeGroup(controlsHint, 1f, 0.8f);
        yield return new WaitForSecondsRealtime(controlsHintSeconds);
        yield return FadeGroup(controlsHint, 0f, 1.5f);
    }

    // ---------------- BOSS ----------------
    private float bossShown = 1f;
    private float bossTrailValue = 1f;
    private float bossTarget = 1f;

    public void ShowBoss(string displayName)
    {
        if (bossGroup == null)
            return;
        if (bossName != null) bossName.text = displayName;
        bossShown = bossTrailValue = bossTarget = 1f;
        StartCoroutine(FadeGroup(bossGroup, 1f, 0.8f));
    }

    public void SetBossHealth(float normalized)
    {
        bossTarget = Mathf.Clamp01(normalized);
        bossShown = Mathf.MoveTowards(bossShown, bossTarget, Time.deltaTime * 2f);
        bossTrailValue = Mathf.MoveTowards(bossTrailValue, bossShown, Time.deltaTime * 0.5f);
        if (bossFill != null) bossFill.fillAmount = bossShown;
        if (bossTrail != null) bossTrail.fillAmount = Mathf.Max(bossTrailValue, bossShown);
    }

    public void HideBoss(bool defeated, string title = "Foe Vanquished", string subtitle = "")
    {
        if (bossGroup == null)
            return;
        if (defeated)
            ShowAreaTitle(title, subtitle);
        StartCoroutine(FadeGroup(bossGroup, 0f, 1.2f));
    }

    // ---------------- SCREENS ----------------
    public IEnumerator FadeScreen(float to, float duration)
    {
        if (screenFade == null)
            yield break;
        yield return FadeGroup(screenFade, to, duration);
    }

    public void ShowGameOver(Player deadPlayer)
    {
        player = deadPlayer;
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(1.4f);
        if (gameOverGroup == null)
            yield break;

        yield return FadeGroup(gameOverGroup, 1f, 1.2f);
        gameOverGroup.interactable = true;
        gameOverGroup.blocksRaycasts = true;
        if (EventSystem.current != null && riseAgainButton != null)
            EventSystem.current.SetSelectedGameObject(riseAgainButton.gameObject);
    }

    private void RiseAgain()
    {
        if (player == null)
            return;
        UIAudio.Confirm();
        StartCoroutine(RiseAgainRoutine());
    }

    private IEnumerator RiseAgainRoutine()
    {
        gameOverGroup.interactable = false;
        yield return FadeScreen(1f, 0.4f);
        SetGroup(gameOverGroup, 0f, false);
        player.Revive();
        yield return FadeScreen(0f, 0.6f);
    }

    public static bool IsGameOverShowing => Instance != null && Instance.gameOverGroup != null && Instance.gameOverGroup.alpha > 0.01f;

    // ---------------- HELPERS ----------------
    private static IEnumerator FadeGroup(CanvasGroup g, float to, float duration)
    {
        if (g == null)
            yield break;
        g.gameObject.SetActive(true);
        float from = g.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }

        g.alpha = to;
        g.blocksRaycasts = to > 0.5f && g.interactable;
    }

    private static void SetGroup(CanvasGroup g, float alpha, bool interactive)
    {
        if (g == null)
            return;
        g.alpha = alpha;
        g.interactable = interactive;
        g.blocksRaycasts = interactive;
    }
}

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
            OnHealthChanged(player.CurrentHealth > 0 ? player.CurrentHealth : player.maxHealth, player.maxHealth);
            shownHealth = trailHealth = targetHealth;
        }

        if (collectables != null)
        {
            collectables.CountsChanged += OnCountsChanged;
            OnCountsChanged(collectables.CurrentCoins, collectables.HealingFlasks);
        }

        if (controlsHint != null)
            StartCoroutine(HideControlsHint());
    }

    private void Update()
    {
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

    public void HideBoss(bool defeated)
    {
        if (bossGroup == null)
            return;
        if (defeated)
            ShowAreaTitle("Gravewarden Vanquished", "The cursed seal is broken");
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

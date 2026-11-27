using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cinematic presentation layer on the HUD: letterbox, title cards, subtitles with speaker names,
/// a black overlay, camera focus through Cinemachine, player lock, music ducking and skipping.
/// Cinematic coroutines use the Step/Say/Title helpers, which all end instantly once skipped.
/// </summary>
public class Cinematic : MonoBehaviour
{
    public static Cinematic Instance { get; private set; }
    public static bool IsPlaying { get; private set; }
    public static bool Skipping { get; private set; }

    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private TMP_Text titleMain;
    [SerializeField] private TMP_Text titleSub;
    [SerializeField] private CanvasGroup subtitleGroup;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private CanvasGroup black;
    [SerializeField] private CanvasGroup skipHint;

    private float bars;
    private float barTarget;
    private Transform focusTarget;
    private CinemachineCamera cm;
    private Transform previousFollow;
    private float skipHeld;

    private void Awake()
    {
        Instance = this;
        IsPlaying = false;
        Skipping = false;
        SetGroup(titleGroup, 0); SetGroup(subtitleGroup, 0); SetGroup(black, 0); SetGroup(skipHint, 0);
        ApplyBars(0f);
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; IsPlaying = false; Player.ControlLocked = false; }
    }

    private void Update()
    {
        bars = Mathf.MoveTowards(bars, barTarget, Time.unscaledDeltaTime * 2.2f);
        ApplyBars(bars);

        if (!IsPlaying || Skipping) { skipHeld = 0f; return; }
        bool held = (Keyboard.current != null && (Keyboard.current.escapeKey.isPressed || Keyboard.current.enterKey.isPressed)) ||
                    (Gamepad.current != null && (Gamepad.current.startButton.isPressed || Gamepad.current.buttonSouth.isPressed));
        skipHeld = held ? skipHeld + Time.unscaledDeltaTime : 0f;
        if (skipHint != null) skipHint.alpha = Mathf.MoveTowards(skipHint.alpha, 1f, Time.unscaledDeltaTime);
        if (skipHeld > 0.6f)
            Skipping = true;
    }

    private void ApplyBars(float k)
    {
        if (topBar == null) return;
        float h = 120f * Mathf.SmoothStep(0, 1, k);
        topBar.sizeDelta = new Vector2(0, h);
        bottomBar.sizeDelta = new Vector2(0, h);
    }

    // ---------------- lifecycle ----------------
    public static void Begin(bool letterbox = true)
    {
        if (Instance == null) return;
        IsPlaying = true;
        Skipping = false;
        Player.ControlLocked = true;
        Instance.barTarget = letterbox ? 1f : 0f;
        Instance.skipHeld = 0f;
        MusicManager.Instance?.Duck(0.55f);
    }

    public static void End()
    {
        if (Instance == null) return;
        Instance.barTarget = 0f;
        SetGroup(Instance.titleGroup, 0); SetGroup(Instance.subtitleGroup, 0); SetGroup(Instance.skipHint, 0);
        ReleaseCamera();
        IsPlaying = false;
        Skipping = false;
        Player.ControlLocked = false;
        MusicManager.Instance?.Duck(1f);
        ScreenShake.Rumble(0f);
    }

    // ---------------- steps (all end immediately when skipping) ----------------
    public static IEnumerator Step(float seconds)
    {
        float t = 0f;
        while (t < seconds && !Skipping) { t += Time.unscaledDeltaTime; yield return null; }
    }

    public static IEnumerator Black(float to, float seconds)
    {
        if (Instance == null || Instance.black == null) yield break;
        var g = Instance.black;
        float from = g.alpha, t = 0f;
        while (t < seconds && !Skipping) { t += Time.unscaledDeltaTime; g.alpha = Mathf.Lerp(from, to, t / seconds); yield return null; }
        g.alpha = to;
    }

    public static void SetBlack(float a) { if (Instance != null && Instance.black != null) Instance.black.alpha = a; }

    /// <summary>Lightning: a quick white flash over the black overlay.</summary>
    public static IEnumerator Flash()
    {
        if (Instance == null || Instance.black == null) yield break;
        var img = Instance.black.GetComponent<UnityEngine.UI.Image>();
        Color c = img.color; float a = Instance.black.alpha;
        img.color = new Color(0.85f, 0.9f, 1f, 1f); Instance.black.alpha = 0.7f;
        yield return new WaitForSecondsRealtime(0.06f);
        Instance.black.alpha = 0.15f;
        yield return new WaitForSecondsRealtime(0.05f);
        Instance.black.alpha = 0.55f;
        yield return new WaitForSecondsRealtime(0.05f);
        img.color = c; Instance.black.alpha = a;
    }

    public static IEnumerator Title(string main, string sub, float hold, float fade = 1.2f)
    {
        if (Instance == null) yield break;
        Instance.titleMain.text = main;
        Instance.titleSub.text = sub;
        yield return Fade(Instance.titleGroup, 1f, fade);
        yield return Step(hold);
        yield return Fade(Instance.titleGroup, 0f, fade * 0.8f);
        SetGroup(Instance.titleGroup, 0);
    }

    /// <summary>Subtitle with typewriter reveal. Voice acting does not exist, so dialogue is text plus atmosphere.</summary>
    public static IEnumerator Say(string speaker, string line, float holdAfter = 1.6f, string sound = null)
    {
        if (Instance == null) yield break;
        Instance.speakerText.text = speaker;
        Instance.lineText.text = line;
        Instance.lineText.maxVisibleCharacters = 0;
        if (!string.IsNullOrEmpty(sound)) MusicManager.Instance?.Play(sound, 0.6f, 0.05f, 0.5f);
        yield return Fade(Instance.subtitleGroup, 1f, 0.3f);
        float t = 0f;
        int total = line.Length;
        while (Instance.lineText.maxVisibleCharacters < total && !Skipping)
        {
            t += Time.unscaledDeltaTime * 42f;
            Instance.lineText.maxVisibleCharacters = Mathf.Min(total, (int)t);
            yield return null;
        }
        Instance.lineText.maxVisibleCharacters = total;
        yield return Step(holdAfter + total * 0.025f);
        yield return Fade(Instance.subtitleGroup, 0f, 0.35f);
    }

    private static IEnumerator Fade(CanvasGroup g, float to, float seconds)
    {
        if (g == null) yield break;
        float from = g.alpha, t = 0f;
        while (t < seconds && !Skipping) { t += Time.unscaledDeltaTime; g.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / seconds)); yield return null; }
        g.alpha = to;
    }

    private static void SetGroup(CanvasGroup g, float a) { if (g != null) { g.alpha = a; g.blocksRaycasts = false; g.interactable = false; } }

    // ---------------- camera ----------------
    /// <summary>Points the camera at a world position (smoothed by Cinemachine damping).</summary>
    public static void FocusCamera(Vector3 position)
    {
        if (Instance == null) return;
        if (Instance.cm == null) Instance.cm = FindFirstObjectByType<CinemachineCamera>();
        if (Instance.cm == null) return;
        if (Instance.focusTarget == null) Instance.focusTarget = new GameObject("[Cinematic Camera Target]").transform;
        if (Instance.cm.Follow != Instance.focusTarget) Instance.previousFollow = Instance.cm.Follow;
        Instance.focusTarget.position = position;
        Instance.cm.Follow = Instance.focusTarget;
    }

    /// <summary>Moves the camera focus smoothly from its current point to a new one.</summary>
    public static IEnumerator PanCamera(Vector3 to, float seconds)
    {
        if (Instance == null) yield break;
        if (Instance.focusTarget == null) { FocusCamera(to); yield break; }
        Vector3 from = Instance.focusTarget.position;
        float t = 0f;
        while (t < seconds && !Skipping)
        {
            t += Time.unscaledDeltaTime;
            Instance.focusTarget.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, t / seconds));
            yield return null;
        }
        Instance.focusTarget.position = to;
    }

    public static void ReleaseCamera()
    {
        if (Instance == null || Instance.cm == null) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        Instance.cm.Follow = player != null ? player.transform : Instance.previousFollow;
    }
}

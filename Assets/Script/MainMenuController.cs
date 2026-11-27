using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Main menu: Continue, New Game, Level Select, Settings, About, Exit.
/// Panels cross-fade; mouse, keyboard and gamepad are all supported; Esc / B goes back.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string firstLevelScene = "Graveyard";

    [Header("Panels")]
    [SerializeField] private CanvasGroup mainPanel;
    [SerializeField] private CanvasGroup levelSelectPanel;
    [SerializeField] private CanvasGroup settingsPanel;
    [SerializeField] private CanvasGroup aboutPanel;
    [SerializeField] private CanvasGroup confirmPanel;
    [SerializeField] private CanvasGroup titleGroup;

    [Header("Main Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button aboutButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Text continueDetail;

    [Header("Level Select")]
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private Button levelBackButton;

    [Header("Settings")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider resolutionSlider;
    [SerializeField] private Slider qualitySlider;
    [SerializeField] private TMP_Text musicValue;
    [SerializeField] private TMP_Text sfxValue;
    [SerializeField] private TMP_Text resolutionValue;
    [SerializeField] private TMP_Text qualityValue;
    [SerializeField] private TMP_Text fullscreenValue;
    [SerializeField] private Button settingsBackButton;

    [Header("About / Confirm")]
    [SerializeField] private Button aboutBackButton;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    private CanvasGroup current;
    private GameObject lastSelected;
    private Coroutine transition;
    private bool starting;

    private void Awake()
    {
        Time.timeScale = 1f;
        EnsureEventSystem();

        Bind(continueButton, ContinueGame);
        Bind(newGameButton, RequestNewGame);
        Bind(levelSelectButton, () => Show(levelSelectPanel, FirstUnlockedLevelButton()));
        Bind(settingsButton, () => Show(settingsPanel, musicSlider != null ? musicSlider.gameObject : null));
        Bind(aboutButton, () => Show(aboutPanel, aboutBackButton != null ? aboutBackButton.gameObject : null));
        Bind(exitButton, QuitGame);
        Bind(levelBackButton, Back, true);
        Bind(settingsBackButton, Back, true);
        Bind(aboutBackButton, Back, true);
        Bind(confirmYesButton, StartNewGame);
        Bind(confirmNoButton, Back, true);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int index = i;
            Bind(levelButtons[i], () => LoadLevel(index));
        }

#if UNITY_WEBGL
        if (exitButton != null) exitButton.gameObject.SetActive(false);
#endif

        SetupSettings();
        foreach (CanvasGroup g in new[] { levelSelectPanel, settingsPanel, aboutPanel, confirmPanel })
            SetVisible(g, false);
        SetVisible(mainPanel, true);
        current = mainPanel;
    }

    private void Start()
    {
        RefreshMainButtons();
        RefreshLevelButtons();
        SceneFader.FadeFromBlack(1.2f);
        StartCoroutine(Intro());
    }

    private IEnumerator Intro()
    {
        // Title rises in, then the menu options fade in.
        if (titleGroup != null) titleGroup.alpha = 0f;
        mainPanel.alpha = 0f;
        mainPanel.interactable = false;
        RectTransform titleRect = titleGroup != null ? (RectTransform)titleGroup.transform : null;
        Vector2 titlePos = titleRect != null ? titleRect.anchoredPosition : Vector2.zero;

        float t = 0f;
        while (t < 1.6f)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.SmoothStep(0f, 1f, t / 1.2f);
            float b = Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 1f);
            if (titleGroup != null)
            {
                titleGroup.alpha = a;
                titleRect.anchoredPosition = titlePos + new Vector2(0f, -24f * (1f - a));
            }
            if (current != mainPanel)
                yield break;
            mainPanel.alpha = b;
            yield return null;
        }

        if (titleRect != null) titleRect.anchoredPosition = titlePos;
        if (current == mainPanel)
        {
            mainPanel.alpha = 1f;
            mainPanel.interactable = true;
            Select(DefaultMainSelection());
        }
    }

    private void Update()
    {
        if (EventSystem.current == null)
            return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null && selected.activeInHierarchy)
        {
            lastSelected = selected;
        }
        else if (NavigationPressed())
        {
            // Mouse clicked empty space: restore a selection for keyboard / gamepad.
            Select(lastSelected != null && lastSelected.activeInHierarchy ? lastSelected : DefaultSelectionFor(current));
        }

        if (current != mainPanel && BackPressed())
            Back();
    }

    // ---------------- MAIN ACTIONS ----------------
    private void ContinueGame()
    {
        if (!GameSave.HasSavedGame)
            return;
        GameSave.LoadRun();
        Load(GameSave.GetSavedSceneOr(firstLevelScene));
    }

    private void RequestNewGame()
    {
        if (GameSave.HasSavedGame && confirmPanel != null)
            Show(confirmPanel, confirmNoButton != null ? confirmNoButton.gameObject : null);
        else
            StartNewGame();
    }

    private void StartNewGame()
    {
        GameSave.NewRun();
        Load(firstLevelScene);
    }

    private void LoadLevel(int index)
    {
        if (!GameSave.IsLevelUnlocked(index))
            return;

        // Starting a chapter from Level Select begins a fresh run at that chapter, with fair gear.
        GameSave.NewRunAtChapter(index);
        Load(GameSave.Levels[index].scene);
    }

    private void Load(string scene)
    {
        if (starting)
            return;
        starting = true;
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;
        SceneFader.LoadScene(scene);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ---------------- PANELS ----------------
    private void Back()
    {
        if (current == mainPanel)
            return;

        GameObject focus = current == levelSelectPanel ? levelSelectButton?.gameObject
            : current == settingsPanel ? settingsButton?.gameObject
            : current == aboutPanel ? aboutButton?.gameObject
            : newGameButton?.gameObject;

        GameSettings.Save();
        Show(mainPanel, focus);
    }

    private void Show(CanvasGroup panel, GameObject focus)
    {
        if (panel == null || starting)
            return;

        if (transition != null)
            StopCoroutine(transition);
        transition = StartCoroutine(SwitchPanel(current, panel, focus));
        current = panel;
    }

    private IEnumerator SwitchPanel(CanvasGroup from, CanvasGroup to, GameObject focus)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        const float duration = 0.22f;
        float t = 0f;
        bool showTitle = to == mainPanel;
        to.gameObject.SetActive(true);
        to.interactable = false;
        if (from != null)
            from.interactable = false;

        RectTransform toRect = (RectTransform)to.transform;
        Vector2 basePos = toRect.anchoredPosition;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            if (from != null && from != to)
                from.alpha = 1f - k;
            to.alpha = k;
            toRect.localScale = Vector3.one * Mathf.Lerp(0.97f, 1f, k);
            if (titleGroup != null)
                titleGroup.alpha = Mathf.MoveTowards(titleGroup.alpha, showTitle ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            yield return null;
        }

        if (from != null && from != to)
            SetVisible(from, false);
        SetVisible(to, true);
        toRect.anchoredPosition = basePos;
        if (titleGroup != null)
            titleGroup.alpha = showTitle ? 1f : 0f;

        if (to == mainPanel)
            RefreshMainButtons();
        Select(focus != null ? focus : DefaultSelectionFor(to));
        transition = null;
    }

    private static void SetVisible(CanvasGroup g, bool visible)
    {
        if (g == null)
            return;
        g.alpha = visible ? 1f : 0f;
        g.interactable = visible;
        g.blocksRaycasts = visible;
        g.transform.localScale = Vector3.one;
        g.gameObject.SetActive(visible);
    }

    // ---------------- STATE ----------------
    private void RefreshMainButtons()
    {
        bool hasSave = GameSave.HasSavedGame;
        if (continueButton != null)
            continueButton.interactable = hasSave;

        if (continueDetail != null)
        {
            int index = GameSave.LevelIndexOf(GameSave.SavedScene);
            continueDetail.text = hasSave && index >= 0 ? GameSave.Levels[index].title : "No journey begun";
        }

        if (levelSelectButton != null)
            levelSelectButton.interactable = true;
    }

    private void RefreshLevelButtons()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            Button b = levelButtons[i];
            if (b == null)
                continue;

            bool exists = i < GameSave.Levels.Length && Application.CanStreamedLevelBeLoaded(GameSave.Levels[i].scene);
            bool unlocked = exists && GameSave.IsLevelUnlocked(i);
            b.interactable = unlocked;

            TMP_Text text = b.GetComponentInChildren<TMP_Text>(true);
            if (text != null && i < GameSave.Levels.Length)
                text.text = unlocked ? GameSave.Levels[i].title : $"{GameSave.Levels[i].title.Split('.')[0]}. <i>Sealed</i>";
        }
    }

    private GameObject FirstUnlockedLevelButton()
    {
        foreach (Button b in levelButtons)
            if (b != null && b.interactable)
                return b.gameObject;
        return levelBackButton != null ? levelBackButton.gameObject : null;
    }

    private GameObject DefaultMainSelection()
    {
        if (continueButton != null && continueButton.interactable)
            return continueButton.gameObject;
        return newGameButton != null ? newGameButton.gameObject : null;
    }

    private GameObject DefaultSelectionFor(CanvasGroup panel)
    {
        if (panel == mainPanel) return DefaultMainSelection();
        if (panel == levelSelectPanel) return FirstUnlockedLevelButton();
        if (panel == settingsPanel) return musicSlider != null ? musicSlider.gameObject : null;
        if (panel == aboutPanel) return aboutBackButton != null ? aboutBackButton.gameObject : null;
        if (panel == confirmPanel) return confirmNoButton != null ? confirmNoButton.gameObject : null;
        return null;
    }

    private static void Select(GameObject go)
    {
        if (go == null || EventSystem.current == null)
            return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(go);
    }

    // ---------------- SETTINGS ----------------
    private void SetupSettings()
    {
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            musicSlider.onValueChanged.AddListener(v => { GameSettings.MusicVolume = v; UpdateSettingLabels(); });
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            sfxSlider.onValueChanged.AddListener(v => { GameSettings.SfxVolume = v; UIAudio.Hover(); UpdateSettingLabels(); });
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            fullscreenToggle.onValueChanged.AddListener(v => { GameSettings.Fullscreen = v; UIAudio.Confirm(); UpdateSettingLabels(); });
        }

        if (resolutionSlider != null)
        {
            resolutionSlider.wholeNumbers = true;
            resolutionSlider.minValue = 0;
            resolutionSlider.maxValue = Mathf.Max(0, GameSettings.Resolutions.Count - 1);
            resolutionSlider.SetValueWithoutNotify(GameSettings.ResolutionIndex);
            resolutionSlider.onValueChanged.AddListener(v => { GameSettings.ResolutionIndex = Mathf.RoundToInt(v); UpdateSettingLabels(); });
        }

        if (qualitySlider != null)
        {
            qualitySlider.wholeNumbers = true;
            qualitySlider.minValue = 0;
            qualitySlider.maxValue = Mathf.Max(0, QualitySettings.names.Length - 1);
            qualitySlider.SetValueWithoutNotify(GameSettings.QualityLevel);
            qualitySlider.onValueChanged.AddListener(v => { GameSettings.QualityLevel = Mathf.RoundToInt(v); UpdateSettingLabels(); });
        }

        UpdateSettingLabels();
    }

    private void UpdateSettingLabels()
    {
        if (musicValue != null) musicValue.text = $"{Mathf.RoundToInt(GameSettings.MusicVolume * 100f)}%";
        if (sfxValue != null) sfxValue.text = $"{Mathf.RoundToInt(GameSettings.SfxVolume * 100f)}%";
        if (fullscreenValue != null) fullscreenValue.text = GameSettings.Fullscreen ? "On" : "Off";
        if (resolutionValue != null) resolutionValue.text = "< " + GameSettings.ResolutionLabel(GameSettings.ResolutionIndex) + " >";
        if (qualityValue != null) qualityValue.text = "< " + QualitySettings.names[GameSettings.QualityLevel] + " >";
    }

    // ---------------- INPUT ----------------
    private static bool BackPressed()
    {
        return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
               (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
    }

    private static bool NavigationPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad g = Gamepad.current;
        bool key = k != null && (k.upArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame ||
                                 k.wKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame ||
                                 k.tabKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
        bool pad = g != null && (g.dpad.up.wasPressedThisFrame || g.dpad.down.wasPressedThisFrame ||
                                 g.leftStick.ReadValue().sqrMagnitude > 0.25f || g.buttonSouth.wasPressedThisFrame);
        return key || pad;
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action, bool isBack = false)
    {
        if (button == null)
            return;
        button.onClick.AddListener(() =>
        {
            if (isBack) UIAudio.Back(); else UIAudio.Confirm();
            action();
        });
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null)
            return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        es.transform.SetAsLastSibling();
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>In-level pause menu (Esc / P / gamepad Start): Resume, volume, Main Menu, Quit.</summary>
public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider ambientSlider;
    [SerializeField] private TMP_Text masterValue;
    [SerializeField] private TMP_Text ambientValue;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TMP_Text musicValue;
    [SerializeField] private TMP_Text sfxValue;
    [SerializeField] private string mainMenuScene = "Main Menu";

    private void Awake()
    {
        IsPaused = false;
        Show(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(() => { UIAudio.Back(); SetPaused(false); });
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ToMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
#if UNITY_WEBGL
        if (quitButton != null) quitButton.gameObject.SetActive(false);
#endif

        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
            masterSlider.onValueChanged.AddListener(v => { GameSettings.MasterVolume = v; Labels(); });
        }

        if (ambientSlider != null)
        {
            ambientSlider.SetValueWithoutNotify(GameSettings.AmbientVolume);
            ambientSlider.onValueChanged.AddListener(v => { GameSettings.AmbientVolume = v; Labels(); });
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            musicSlider.onValueChanged.AddListener(v => { GameSettings.MusicVolume = v; Labels(); });
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            sfxSlider.onValueChanged.AddListener(v => { GameSettings.SfxVolume = v; UIAudio.Hover(); Labels(); });
        }

        Labels();
    }

    private void OnDestroy()
    {
        if (IsPaused)
            Time.timeScale = 1f;
        IsPaused = false;
    }

    private void Update()
    {
        if (SceneFader.IsLoading || GameHUD.IsGameOverShowing || InventoryUI.IsOpen || ReadPanel.IsOpen || ShrineMenu.IsOpen || Cinematic.IsPlaying)
            return;

        bool toggle = (Keyboard.current != null &&
                       (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)) ||
                      (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
        bool back = IsPaused && Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;

        if (toggle || back)
        {
            if (IsPaused) UIAudio.Back(); else UIAudio.Confirm();
            SetPaused(!IsPaused);
        }

        if (IsPaused && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null &&
            (Keyboard.current != null && (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame) ||
             Gamepad.current != null && Gamepad.current.dpad.ReadValue() != Vector2.zero))
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = false;
        Show(paused);

        if (paused && EventSystem.current != null && resumeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }

        if (!paused)
            GameSettings.Save();
    }

    private void Show(bool visible)
    {
        if (panel == null)
            return;
        panel.gameObject.SetActive(visible);
        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }

    private void ToMainMenu()
    {
        UIAudio.Back();
        IsPaused = false;
        Time.timeScale = 1f;
        Show(false);
        SceneFader.LoadScene(mainMenuScene);
    }

    private void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void Labels()
    {
        if (masterValue != null) masterValue.text = $"{Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%";
        if (ambientValue != null) ambientValue.text = $"{Mathf.RoundToInt(GameSettings.AmbientVolume * 100f)}%";
        if (musicValue != null) musicValue.text = $"{Mathf.RoundToInt(GameSettings.MusicVolume * 100f)}%";
        if (sfxValue != null) sfxValue.text = $"{Mathf.RoundToInt(GameSettings.SfxVolume * 100f)}%";
    }
}

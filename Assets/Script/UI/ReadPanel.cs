using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Parchment panel for notes and inscriptions. Pauses the game while open.</summary>
public class ReadPanel : MonoBehaviour
{
    private static ReadPanel instance;
    public static bool IsOpen { get; private set; }

    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    private float openedAt;

    private void Awake()
    {
        instance = this;
        IsOpen = false;
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            if (IsOpen) Time.timeScale = 1f;
            IsOpen = false;
        }
    }

    public static void Show(string title, string body)
    {
        if (instance == null)
            return;
        instance.titleText.text = title;
        instance.bodyText.text = body;
        instance.openedAt = Time.unscaledTime;
        IsOpen = true;
        Time.timeScale = 0f;
        instance.SetVisible(true);
        UIAudio.Confirm();
    }

    private void Update()
    {
        if (!IsOpen || Time.unscaledTime - openedAt < 0.2f)
            return;
        bool close = (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame ||
                                                   Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) ||
                     (Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.buttonNorth.wasPressedThisFrame ||
                                                  Gamepad.current.buttonSouth.wasPressedThisFrame)) ||
                     (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (close)
        {
            IsOpen = false;
            Time.timeScale = 1f;
            SetVisible(false);
            UIAudio.Back();
        }
    }

    private void SetVisible(bool v)
    {
        if (group == null) return;
        group.alpha = v ? 1f : 0f;
        group.blocksRaycasts = v;
        group.interactable = v;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Credits roll: scrolls the credits upward until the last line has passed, holds the closing
/// card, then returns to the main menu. Hold Space / gamepad A to speed up; Esc / Start skips.
/// </summary>
public class CreditScene : MonoBehaviour
{
    public float scrollSpeed = 70f;
    [SerializeField] private RectTransform content;
    [SerializeField] private CanvasGroup closingCard;
    [SerializeField] private float closingHold = 6f;
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    // Legacy field kept so older scene data still deserializes.
    [SerializeField, HideInInspector] private float returnToMenuAfterSeconds;

    private float startY;
    private float doneAt = -1f;
    private bool leaving;

    private void Start()
    {
        if (content == null) content = GetComponent<RectTransform>();
        startY = content.anchoredPosition.y;
        if (closingCard != null) closingCard.alpha = 0f;
        SceneFader.FadeFromBlack(1.5f);
    }

    private void Update()
    {
        if (leaving || content == null)
            return;

        bool fast = (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.enterKey.isPressed)) ||
                    (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed);
        bool skip = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                    (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        var canvas = content.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        float travel = content.rect.height + canvas.rect.height;
        if (content.anchoredPosition.y - startY < travel)
        {
            content.anchoredPosition += new Vector2(0f, scrollSpeed * (fast ? 4f : 1f) * Time.deltaTime);
        }
        else if (doneAt < 0f)
        {
            doneAt = Time.time;
        }

        if (doneAt >= 0f && closingCard != null)
            closingCard.alpha = Mathf.Clamp01((Time.time - doneAt) / 1.5f);

        if (skip || (doneAt >= 0f && Time.time - doneAt > closingHold))
            Leave();
    }

    private void Leave()
    {
        // The tale is complete: clear the finished run (chapters stay unlocked) and return to the menu.
        leaving = true;
        GameSave.NewRun();
        GameSave.IntroPending = false;
        SceneFader.LoadScene(mainMenuSceneName);
    }
}

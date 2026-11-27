using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hover / selection / press feedback for any menu Selectable (Button, Slider, Toggle).
/// Mouse hover selects the element, so mouse, keyboard and gamepad share one highlight.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class MenuButton : MonoBehaviour,
    ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, ISubmitHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject[] selectionMarkers;
    [SerializeField] private Graphic glow;
    [SerializeField] private Color normalColor = new Color(0.80f, 0.76f, 0.70f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.45f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.42f, 0.40f, 0.40f, 0.75f);
    [SerializeField] private float selectedScale = 1.06f;
    [SerializeField] private float labelSlide = 10f;
    [SerializeField] private bool silent;

    private Selectable selectable;
    private bool selected;
    private bool pressed;
    private Vector3 baseScale;
    private Vector2 labelBasePos;
    private float glowAlpha;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        baseScale = transform.localScale;
        if (label != null)
            labelBasePos = label.rectTransform.anchoredPosition;
        Apply(true);
    }

    private void OnEnable()
    {
        selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        pressed = false;
        Apply(true);
    }

    private void Update()
    {
        Apply(false);
    }

    private void Apply(bool instant)
    {
        bool interactable = selectable == null || selectable.IsInteractable();
        bool on = selected && interactable;
        float k = instant ? 1f : 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);

        float targetScale = pressed && on ? selectedScale - 0.05f : on ? selectedScale : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * targetScale, k);

        if (label != null)
        {
            Color target = !interactable ? disabledColor : on ? selectedColor : normalColor;
            label.color = Color.Lerp(label.color, target, k);
            Vector2 pos = labelBasePos + (on ? new Vector2(labelSlide, 0f) : Vector2.zero);
            label.rectTransform.anchoredPosition = Vector2.Lerp(label.rectTransform.anchoredPosition, pos, k);
        }

        if (selectionMarkers != null)
            foreach (GameObject marker in selectionMarkers)
                if (marker != null && marker.activeSelf != on)
                    marker.SetActive(on);

        if (glow != null)
        {
            float pulse = on ? 0.32f + 0.08f * Mathf.Sin(Time.unscaledTime * 3f) : 0f;
            glowAlpha = Mathf.Lerp(glowAlpha, pulse, k);
            Color c = glow.color;
            c.a = glowAlpha;
            glow.color = c;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!selected && !silent && selectable.IsInteractable())
            UIAudio.Hover();
        selected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        pressed = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable.IsInteractable() && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerDown(PointerEventData eventData) => pressed = true;
    public void OnPointerUp(PointerEventData eventData) => pressed = false;

    public void OnSubmit(BaseEventData eventData)
    {
        // Brief press punch for keyboard / gamepad activation.
        transform.localScale = baseScale * (selectedScale - 0.08f);
    }
}

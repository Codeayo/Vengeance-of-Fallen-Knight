using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Slow drifting parallax for menu backdrop layers. Deeper layers (lower depth) move less.
/// Reacts subtly to the mouse position and drifts on its own over time.
/// </summary>
public class MenuParallaxLayer : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float depth = 0.5f;
    [SerializeField] private float mouseAmount = 0.6f;
    [SerializeField] private float driftAmount = 0.25f;
    [SerializeField] private float driftSpeed = 0.12f;
    [SerializeField] private float scrollSpeed;
    [SerializeField] private float scrollWrap;

    private Vector3 basePos;
    private Vector2 smoothedMouse;
    private float scroll;

    private void Start()
    {
        basePos = transform.localPosition;
    }

    private void LateUpdate()
    {
        Vector2 mouse = Vector2.zero;
        if (Mouse.current != null && Screen.width > 0 && Screen.height > 0)
        {
            Vector2 p = Mouse.current.position.ReadValue();
            mouse = new Vector2(p.x / Screen.width - 0.5f, p.y / Screen.height - 0.5f);
        }

        smoothedMouse = Vector2.Lerp(smoothedMouse, mouse, 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));
        float t = Time.unscaledTime * driftSpeed;
        Vector2 drift = new Vector2(Mathf.Sin(t), Mathf.Sin(t * 0.7f + 1.3f) * 0.5f) * driftAmount;

        if (scrollSpeed != 0f && scrollWrap > 0f)
            scroll = Mathf.Repeat(scroll + scrollSpeed * Time.unscaledDeltaTime, scrollWrap);

        Vector2 offset = (smoothedMouse * mouseAmount + drift) * depth;
        transform.localPosition = basePos + new Vector3(offset.x - scroll, offset.y, 0f);
    }
}

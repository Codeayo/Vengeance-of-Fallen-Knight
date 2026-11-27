using TMPro;
using UnityEngine;

/// <summary>Floats a world-space damage number upward, fades it out and destroys it.</summary>
public class FloatingNumber : MonoBehaviour
{
    private const float Life = 0.8f;
    private TMP_Text text;
    private float t;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        t += Time.deltaTime;
        transform.position += Vector3.up * (2.2f * (1f - t / Life) * Time.deltaTime);
        float s = t < 0.1f ? Mathf.Lerp(0.6f, 1.25f, t / 0.1f) : Mathf.Lerp(1.25f, 1f, (t - 0.1f) / 0.2f);
        transform.localScale = Vector3.one * s;
        if (text != null)
        {
            Color c = text.color;
            c.a = Mathf.Clamp01(1f - (t - 0.45f) / (Life - 0.45f));
            text.color = c;
        }

        if (t >= Life)
            Destroy(gameObject);
    }
}

using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Hit feedback for enemies: brief white/red flash plus a floating damage number.
/// The player's attack uses SendMessageUpwards("TakeDamage"), so this receives every hit
/// alongside the enemy's own AI script without needing changes to it.
/// </summary>
public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = new Color(1f, 0.45f, 0.4f, 1f);
    [SerializeField] private float flashTime = 0.12f;
    [SerializeField] private float numberHeight = 2.5f;

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private Coroutine routine;
    private float lastHit = -1f;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseColors[i] = renderers[i].color;
    }

    public void TakeDamage(int amount)
    {
        // SendMessageUpwards can reach this twice in one frame (child collider + root); count once.
        if (Time.frameCount == (int)lastHit)
            return;
        lastHit = Time.frameCount;

        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(Flash());
        SpawnNumber(amount);
    }

    private IEnumerator Flash()
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = flashColor;
        yield return new WaitForSeconds(flashTime);
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = baseColors[i];
        routine = null;
    }

    private void SpawnNumber(int amount)
    {
        var go = new GameObject("Damage " + amount);
        go.transform.position = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), numberHeight, 0f);
        var text = go.AddComponent<TextMeshPro>();
        text.text = amount.ToString();
        text.fontSize = 6f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.85f, 0.5f);
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(30, 10, 5, 255);
        text.sortingLayerID = SortingLayer.NameToID("ui");
        text.sortingOrder = 50;
        go.AddComponent<FloatingNumber>();
    }
}

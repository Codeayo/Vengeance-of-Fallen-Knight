using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The Watcher: a spectral, hooded narrator built at runtime from the graveyard's statue art,
/// a glow, wisps and a cold light. Hovers, sways and flickers; fades in and out with sound.
/// Appears only in cinematics (the opening, the throne room and the epilogue).
/// </summary>
public class GhostNarrator : MonoBehaviour
{
    public const string SpeakerName = "The Watcher";

    private SpriteRenderer body, glow;
    private Light2D lightSource;
    private ParticleSystem wisps;
    private Vector3 anchor;
    private float alpha;
    private float targetAlpha;
    private float t;

    public static GhostNarrator Spawn(Vector3 position, bool faceLeft = true)
    {
        var assets = CinematicAssets.Instance;
        var go = new GameObject("The Watcher (Ghost)");
        go.transform.position = position;
        var g = go.AddComponent<GhostNarrator>();
        g.anchor = position;

        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(go.transform, false);
        glowGo.transform.localPosition = new Vector3(0, 0.4f, 0);
        glowGo.transform.localScale = new Vector3(5f, 7f, 1f);
        g.glow = glowGo.AddComponent<SpriteRenderer>();
        g.glow.sprite = assets != null ? assets.softGlow : null;
        g.glow.sortingLayerName = "Grill";
        g.glow.sortingOrder = 40;

        var bodyGo = new GameObject("Figure");
        bodyGo.transform.SetParent(go.transform, false);
        bodyGo.transform.localScale = Vector3.one * 3.2f;
        g.body = bodyGo.AddComponent<SpriteRenderer>();
        g.body.sprite = assets != null ? assets.ghostFigure : null;
        g.body.flipX = !faceLeft;
        g.body.sortingLayerName = "Grill";
        g.body.sortingOrder = 41;

        var lightGo = new GameObject("Spectral Light");
        lightGo.transform.SetParent(go.transform, false);
        g.lightSource = lightGo.AddComponent<Light2D>();
        g.lightSource.lightType = Light2D.LightType.Point;
        g.lightSource.color = new Color(0.55f, 0.85f, 1f);
        g.lightSource.pointLightOuterRadius = 7f;
        g.lightSource.pointLightInnerRadius = 0.5f;
        g.lightSource.intensity = 0f;

        g.wisps = BuildWisps(go.transform, assets);
        g.SetAlpha(0f);
        return g;
    }

    private static ParticleSystem BuildWisps(Transform parent, CinematicAssets assets)
    {
        var go = new GameObject("Wisps");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, -1.2f, 0);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var r = go.GetComponent<ParticleSystemRenderer>();
        if (assets != null) r.sharedMaterial = assets.particleMaterial;
        r.sortingLayerName = "Grill";
        r.sortingOrder = 42;
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.9f, 1f, 0.8f), new Color(0.85f, 0.95f, 1f, 0.6f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 120;
        var tsa = ps.textureSheetAnimation;
        tsa.enabled = true; tsa.mode = ParticleSystemAnimationMode.Sprites;
        if (assets != null) tsa.SetSprite(0, assets.softGlow);
        var em = ps.emission; em.rateOverTime = 22f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.6f, 0.6f, 0.1f);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f); vel.y = new ParticleSystem.MinMaxCurve(0.4f, 1.1f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.6f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0, 1) });
        col.color = g;
        return ps;
    }

    public IEnumerator Appear(float seconds = 2.2f)
    {
        MusicManager.Instance?.PlayAt("ghost_appear", transform.position, 0.9f, 0.02f, 0f);
        wisps.Play();
        targetAlpha = 1f;
        float start = alpha, k = 0f;
        while (k < seconds)
        {
            k += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(start, 1f, Mathf.SmoothStep(0, 1, k / seconds)));
            yield return null;
        }
    }

    public IEnumerator Vanish(float seconds = 1.8f)
    {
        MusicManager.Instance?.PlayAt("ghost_vanish", transform.position, 0.8f, 0.02f, 0f);
        targetAlpha = 0f;
        wisps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        float start = alpha, k = 0f;
        while (k < seconds)
        {
            k += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(start, 0f, k / seconds));
            transform.position += Vector3.up * Time.unscaledDeltaTime * 0.6f;
            yield return null;
        }
        Destroy(gameObject);
    }

    public void Whisper() => MusicManager.Instance?.PlayAt("ghost_whisper", transform.position, 0.55f, 0.05f, 1.2f);

    private void SetAlpha(float a)
    {
        alpha = a;
        float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(t * 3f, 0.3f);
        if (body != null) body.color = new Color(0.62f, 0.88f, 1f, 0.62f * a * flicker);
        if (glow != null) glow.color = new Color(0.45f, 0.8f, 1f, 0.35f * a);
        if (lightSource != null) lightSource.intensity = 1.6f * a * flicker;
    }

    private void Update()
    {
        t += Time.unscaledDeltaTime;
        transform.position = new Vector3(anchor.x + Mathf.Sin(t * 0.7f) * 0.25f, transform.position.y + (anchor.y + Mathf.Sin(t * 1.3f) * 0.3f - transform.position.y) * Mathf.Clamp01(Time.unscaledDeltaTime * 3f), 0f);
        if (body != null) body.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.9f) * 2.5f);
        SetAlpha(alpha);
        if (targetAlpha <= 0f) anchor += Vector3.up * Time.unscaledDeltaTime * 0.6f;
    }
}

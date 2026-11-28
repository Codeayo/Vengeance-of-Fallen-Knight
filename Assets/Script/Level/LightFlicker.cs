using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Gentle fire flicker for torches, braziers and candles.</summary>
[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    private Light2D l;
    private float baseIntensity;
    private float seed;

    private void Awake()
    {
        l = GetComponent<Light2D>();
        baseIntensity = l.intensity;
        seed = Random.value * 100f;
    }

    private void Update()
    {
        l.intensity = baseIntensity * (0.8f + 0.35f * Mathf.PerlinNoise(seed, Time.time * 7f));
    }
}

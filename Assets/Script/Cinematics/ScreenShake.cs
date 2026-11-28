using UnityEngine;

/// <summary>Camera shake applied after Cinemachine positions the camera each frame.</summary>
[DefaultExecutionOrder(10000)]
public class ScreenShake : MonoBehaviour
{
    private static ScreenShake instance;
    private float strength;
    private float rumble;
    private Vector3 applied;

    public static void Shake(float amount)
    {
        Ensure();
        if (instance != null) instance.strength = Mathf.Max(instance.strength, amount);
    }

    /// <summary>Sustained low shake (grave rumble, boss roars) until set back to 0.</summary>
    public static void Rumble(float amount)
    {
        Ensure();
        if (instance != null) instance.rumble = amount;
    }

    private static void Ensure()
    {
        if (instance != null || Camera.main == null) return;
        instance = Camera.main.GetComponent<ScreenShake>();
        if (instance == null) instance = Camera.main.gameObject.AddComponent<ScreenShake>();
    }

    private void OnDestroy() { if (instance == this) instance = null; }

    private void LateUpdate()
    {
        transform.position -= applied;
        float s = strength + rumble;
        applied = s > 0.001f ? (Vector3)(Random.insideUnitCircle * s) : Vector3.zero;
        transform.position += applied;
        strength = Mathf.MoveTowards(strength, 0f, Time.unscaledDeltaTime * 2.5f);
    }
}

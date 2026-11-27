using UnityEngine;

/// <summary>
/// Scales a world-space backdrop so it always covers the orthographic camera view,
/// whatever the screen aspect ratio. referenceSize is the backdrop's unscaled world size.
/// </summary>
[ExecuteAlways]
public class FitCameraHeight : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 referenceSize = new Vector2(32f, 18f);
    [SerializeField] private float overscan = 1.08f;

    private void LateUpdate()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null || !cam.orthographic || referenceSize.x <= 0f || referenceSize.y <= 0f)
            return;

        float viewH = cam.orthographicSize * 2f;
        float viewW = viewH * cam.aspect;
        float scale = Mathf.Max(viewW / referenceSize.x, viewH / referenceSize.y) * overscan;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}

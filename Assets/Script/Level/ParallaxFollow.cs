using UnityEngine;

/// <summary>
/// Moves a background layer with the camera by a fraction of the camera's movement.
/// 1 = locked to the camera (infinitely far), 0 = static in the world.
/// </summary>
public class ParallaxFollow : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float followX = 0.85f;
    [SerializeField, Range(0f, 1f)] private float followY = 0.85f;

    private Transform cam;
    private Vector3 startPos;
    private Vector3 camStart;

    private void Start()
    {
        if (Camera.main == null)
            return;
        cam = Camera.main.transform;
        startPos = transform.position;
        camStart = cam.position;
    }

    private void LateUpdate()
    {
        if (cam == null)
            return;
        Vector3 d = cam.position - camStart;
        transform.position = new Vector3(startPos.x + d.x * followX, startPos.y + d.y * followY, startPos.z);
    }
}

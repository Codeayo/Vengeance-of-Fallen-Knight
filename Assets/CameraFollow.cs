using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;       // HeroKnight
    public float smoothSpeed = 0.15f;
    public Vector3 offset;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothed = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Keep camera at z = -10 so it can see the scene
        transform.position = new Vector3(smoothed.x, smoothed.y, -10f);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    private float startPos, length;
    public GameObject cam;
    public float parallaxEffect; // The speed at which the background should move relative to the camera

    void Start()
    {
        if (cam == null)
            cam = Camera.main != null ? Camera.main.gameObject : null;

        if (cam == null)
            return;

        startPos = transform.position.x;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            length = sr.bounds.size.x;
    }

    void FixedUpdate()
    {
        if (cam == null)
            return;

        // Calculate distance background moves based on camera movement
        float distance = cam.transform.position.x * parallaxEffect; // 0 = move with cam || 1 = won't move || 0.5 = half
        float movement = cam.transform.position.x * (1 - parallaxEffect);

        transform.position = new Vector3(startPos + distance, transform.position.y, transform.position.z);

        // If background has reached the end of its length, adjust its position for infinite scrolling
        if (movement > startPos + length)
        {
            startPos += length;
        }
        else if (movement < startPos - length)
        {
            startPos -= length;
        }
    }
}

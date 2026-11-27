using UnityEngine;

/// <summary>
/// SpriteRenderer.flipX mirrors the art around the sprite pivot, but colliders do not follow.
/// For characters whose body is off-centre in the sprite, this mirrors collider offsets
/// so the hitbox always stays on the body.
/// </summary>
[DefaultExecutionOrder(100)]
public class FlipColliderSync : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D[] colliders;

    private Vector2[] baseOffsets;
    private bool baseFlip;
    private bool lastFlip;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (colliders == null || colliders.Length == 0)
            colliders = GetComponents<Collider2D>();

        baseOffsets = new Vector2[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
            baseOffsets[i] = colliders[i] != null ? colliders[i].offset : Vector2.zero;

        baseFlip = spriteRenderer != null && spriteRenderer.flipX;
        lastFlip = baseFlip;
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null || spriteRenderer.flipX == lastFlip)
            return;

        lastFlip = spriteRenderer.flipX;
        bool mirrored = lastFlip != baseFlip;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
                continue;
            Vector2 o = baseOffsets[i];
            colliders[i].offset = new Vector2(mirrored ? -o.x : o.x, o.y);
        }
    }
}

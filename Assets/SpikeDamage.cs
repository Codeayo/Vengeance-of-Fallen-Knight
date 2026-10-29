using UnityEngine;
using UnityEngine.Tilemaps;

public class SpikeDamage : MonoBehaviour
{
    [Header("Spike Damage Settings")]
    public int damageAmount = 10;
    public float damageCooldown = 1f;
    public float knockbackForce = 5f;

    private float lastDamageTime;
    private Tilemap spikeTilemap;

    private void Awake()
    {
        spikeTilemap = GetComponent<Tilemap>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (spikeTilemap != null && !IsStandingOnSpikeTile(collision.bounds)) return;

        if (Time.time > lastDamageTime + damageCooldown)
        {
            lastDamageTime = Time.time;

            // Damage player
            Player player = collision.GetComponentInParent<Player>();
            if (player != null)
            {
                Debug.Log($"Spike damage: {damageAmount} at {transform.position}", this);
                player.TakeDamage(damageAmount);
            }

            // Knockback effect
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = (collision.transform.position - transform.position).normalized;
                rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }

    private bool IsStandingOnSpikeTile(Bounds playerBounds)
    {
        Vector3Int minCell = spikeTilemap.WorldToCell(new Vector3(playerBounds.min.x, playerBounds.min.y, 0f));
        Vector3Int maxCell = spikeTilemap.WorldToCell(new Vector3(playerBounds.max.x, playerBounds.center.y, 0f));

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                if (spikeTilemap.HasTile(new Vector3Int(x, y, 0)))
                    return true;
            }
        }

        return false;
    }
}

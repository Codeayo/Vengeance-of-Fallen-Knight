using UnityEngine;

public class SpikeDamage : MonoBehaviour
{
    [Header("Spike Damage Settings")]
    public int damageAmount = 10;
    public float damageCooldown = 1f;
    public float knockbackForce = 5f;

    private float lastDamageTime;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (Time.time > lastDamageTime + damageCooldown)
        {
            lastDamageTime = Time.time;

            // Damage player
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
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
}

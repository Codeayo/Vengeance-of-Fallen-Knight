using UnityEngine;

public class DeathBox : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.transform.position.y < transform.position.y)
        {
            Player player = collision.GetComponentInParent<Player>();

            if (player != null)
            {
                Debug.Log("Death box damage: player fell below the level.", this);
                player.TakeDamage(9999); // guaranteed kill
            }
        }
    }
}

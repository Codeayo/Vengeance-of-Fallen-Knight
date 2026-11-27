using System.Collections;
using UnityEngine;

/// <summary>
/// Spike pit / out-of-bounds volume. The player takes damage and, if still alive,
/// is returned to the last checkpoint after a short fade, so a pit can never trap them.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HazardZone : MonoBehaviour
{
    [SerializeField] private int damage = 20;
    [SerializeField] private bool respawnPlayer = true;
    [SerializeField] private string message = "Impaled on the iron spikes";

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null || player.IsDead || player.IsRespawning)
            return;

        StartCoroutine(Hit(player));
    }

    private IEnumerator Hit(Player player)
    {
        player.TakeDamage(damage, ignoreInvulnerability: true);
        if (player.IsDead || !respawnPlayer)
            yield break;

        GameHUD.Instance?.ShowToast(message);
        yield return player.RespawnRoutine();
    }
}

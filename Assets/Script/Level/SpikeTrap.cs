using System.Collections;
using UnityEngine;

/// <summary>
/// Telegraphed iron spikes that rise out of the ground on a cycle.
/// They rattle (warning) before rising, and only hurt while raised, so timing is readable and fair.
/// </summary>
public class SpikeTrap : MonoBehaviour
{
    [SerializeField] private Transform spikes;
    [SerializeField] private BoxCollider2D damageArea;
    [SerializeField] private float raisedHeight = 1.2f;
    [SerializeField] private float loweredTime = 1.8f;
    [SerializeField] private float warningTime = 0.6f;
    [SerializeField] private float raisedTime = 1.1f;
    [SerializeField] private float startDelay;
    [SerializeField] private int damage = 15;
    [SerializeField] private float knockback = 7f;

    private Vector3 loweredPos;
    private bool dangerous;
    private bool hitThisCycle;

    private void Awake()
    {
        if (spikes != null)
            loweredPos = spikes.localPosition;
        if (damageArea != null)
            damageArea.isTrigger = true;
    }

    private void OnEnable()
    {
        StartCoroutine(Cycle());
    }

    private IEnumerator Cycle()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        while (true)
        {
            dangerous = false;
            SetHeight(0f);
            yield return new WaitForSeconds(loweredTime);

            // Warning: spikes peek out and rattle.
            float t = 0f;
            while (t < warningTime)
            {
                t += Time.deltaTime;
                SetHeight(0.18f + Mathf.Sin(t * 60f) * 0.04f);
                yield return null;
            }

            // Thrust up quickly. Each thrust can hit the player at most once.
            t = 0f;
            dangerous = true;
            hitThisCycle = false;
            while (t < 0.08f)
            {
                t += Time.deltaTime;
                SetHeight(Mathf.Lerp(0.18f, 1f, t / 0.08f));
                yield return null;
            }

            yield return new WaitForSeconds(raisedTime);

            dangerous = false;
            t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                SetHeight(Mathf.Lerp(1f, 0f, t / 0.35f));
                yield return null;
            }
        }
    }

    private void SetHeight(float normalized)
    {
        if (spikes != null)
            spikes.localPosition = loweredPos + Vector3.up * (raisedHeight * normalized);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!dangerous || hitThisCycle || !other.CompareTag("Player"))
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null || player.IsDead)
            return;

        int before = player.CurrentHealth;
        player.TakeDamage(damage);
        if (player.CurrentHealth < before)
            hitThisCycle = true;
        if (player.CurrentHealth < before && other.attachedRigidbody != null)
        {
            float dir = Mathf.Sign(other.transform.position.x - transform.position.x);
            if (dir == 0f) dir = -1f;
            other.attachedRigidbody.linearVelocity = new Vector2(dir * knockback, knockback * 1.1f);
        }
    }
}

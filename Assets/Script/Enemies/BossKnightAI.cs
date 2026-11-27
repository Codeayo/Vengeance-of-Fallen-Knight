using System.Collections;
using UnityEngine;

/// <summary>
/// Lord Malvric, the usurper: a dark mirror of the knight using the same animation set.
/// Phase 1: measured sword strings and blocks. Phase 2 (below 60%): faster combos, rolls through
/// the player and lunging strikes. Phase 3 (below 30%): calls his royal guard back from the dead.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class BossKnightAI : MonoBehaviour, IBossHealth
{
    [SerializeField] private int maxHealth = 420;
    [SerializeField] private int strikeDamage = 18;
    [SerializeField] private float walkSpeed = 3.2f;
    [SerializeField] private float strikeReach = 3.6f;
    [SerializeField] private float aggroRange = 14f;
    [SerializeField] private GameObject[] summonPrefabs;
    [SerializeField] private Transform[] summonPoints;
    [SerializeField] private Color baseTint = new Color(0.55f, 0.35f, 0.45f);

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private Transform player;
    private Player playerComp;
    private int health;
    private int phase = 1;
    private bool busy;
    private bool dead;
    private bool blocking;
    private bool engaged;
    private bool summoned;
    private float nextAction;

    public float HealthNormalized => Mathf.Clamp01(health / (float)maxHealth);
    public bool IsDead => dead;
    public bool Engaged => engaged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        health = maxHealth;
        if (sr != null) sr.color = baseTint;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { player = p.transform; playerComp = p.GetComponent<Player>(); }
    }

    private void Update()
    {
        if (dead || player == null)
            return;

        float dx = player.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);

        if (!engaged)
        {
            if (dist < aggroRange) engaged = true;
            else { Idle(); return; }
        }

        if (playerComp != null && playerComp.IsDead)
        {
            Idle();
            return;
        }

        if (busy)
            return;

        Face(dx);
        if (Time.time < nextAction)
        {
            Idle();
            return;
        }

        if (dist > strikeReach)
        {
            // Close the distance; in phase 2+ sometimes lunge from mid-range.
            if (phase >= 2 && dist < strikeReach * 2.2f && Random.value < 0.012f)
                StartCoroutine(Lunge(Mathf.Sign(dx)));
            else
                Walk(Mathf.Sign(dx));
            return;
        }

        float r = Random.value;
        if (phase >= 2 && r < 0.18f)
            StartCoroutine(RollThrough(Mathf.Sign(dx)));
        else if (r < 0.32f)
            StartCoroutine(Guard());
        else
            StartCoroutine(Combo(phase >= 2 ? 3 : 2));
    }

    private void Idle()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        anim.SetBool("IsRunning", false);
    }

    private void Walk(float dir)
    {
        float speed = walkSpeed * (phase >= 2 ? 1.3f : 1f);
        if (!EnemyCore.GroundAhead(EnemyCore.Feet(gameObject), dir, 1.2f, 3f)) { Idle(); return; }
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
        anim.SetBool("IsRunning", true);
    }

    private void Face(float dx)
    {
        if (Mathf.Abs(dx) > 0.2f && sr != null)
            sr.flipX = dx < 0f;
    }

    private IEnumerator Combo(int hits)
    {
        busy = true;
        Idle();
        for (int i = 0; i < hits && !dead; i++)
        {
            anim.SetInteger("CombpStep", i);
            anim.SetTrigger("Attack");
            // Telegraph: a brief red flash before each blow.
            if (sr != null) sr.color = Color.Lerp(baseTint, Color.red, 0.6f);
            yield return new WaitForSeconds(phase >= 2 ? 0.28f : 0.4f);
            if (sr != null) sr.color = baseTint;
            if (dead) break;
            float dx = player.position.x - transform.position.x;
            bool facing = (dx < 0f) == (sr != null && sr.flipX);
            if (facing && Mathf.Abs(dx) <= strikeReach * 1.1f && Mathf.Abs(player.position.y - transform.position.y) < 2.5f)
                playerComp?.TakeDamage(strikeDamage + (phase - 1) * 4);
            yield return new WaitForSeconds(0.22f);
        }

        nextAction = Time.time + (phase >= 2 ? 0.6f : 1.1f);
        busy = false;
    }

    private IEnumerator Guard()
    {
        busy = true;
        blocking = true;
        Idle();
        anim.SetBool("IsBlocking", true);
        yield return new WaitForSeconds(0.9f);
        anim.SetBool("IsBlocking", false);
        blocking = false;
        nextAction = Time.time + 0.2f;
        busy = false;
    }

    private IEnumerator RollThrough(float dir)
    {
        busy = true;
        anim.SetTrigger("Roll");
        float t = 0f;
        while (t < 0.4f && !dead)
        {
            t += Time.deltaTime;
            if (EnemyCore.GroundAhead(EnemyCore.Feet(gameObject), dir, 1.2f, 3f))
                rb.linearVelocity = new Vector2(dir * 11f, rb.linearVelocity.y);
            else
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            yield return null;
        }

        Idle();
        Face(player.position.x - transform.position.x);
        yield return Combo(2);
    }

    private IEnumerator Lunge(float dir)
    {
        busy = true;
        if (sr != null) sr.color = Color.Lerp(baseTint, Color.red, 0.7f);
        Idle();
        yield return new WaitForSeconds(0.45f);
        if (sr != null) sr.color = baseTint;
        anim.SetTrigger("Attack");
        float t = 0f;
        bool hit = false;
        while (t < 0.35f && !dead)
        {
            t += Time.deltaTime;
            if (EnemyCore.GroundAhead(EnemyCore.Feet(gameObject), dir, 1.2f, 3f))
                rb.linearVelocity = new Vector2(dir * 13f, rb.linearVelocity.y);
            if (!hit && Mathf.Abs(player.position.x - transform.position.x) < strikeReach && Mathf.Abs(player.position.y - transform.position.y) < 2.5f)
            {
                hit = true;
                playerComp?.TakeDamage(strikeDamage + 6);
            }
            yield return null;
        }

        Idle();
        nextAction = Time.time + 0.8f;
        busy = false;
    }

    /// <summary>Receives the knight's blows (SendMessageUpwards).</summary>
    public void TakeDamage(int amount)
    {
        if (dead || amount <= 0)
            return;
        if (!engaged) engaged = true;

        int applied = blocking ? Mathf.Max(1, amount / 4) : amount;
        health = Mathf.Max(0, health - applied);
        if (blocking)
            GameHUD.Instance?.ShowToast("Malvric parries your blow");
        else
            anim.SetTrigger("Hurt");

        if (phase == 1 && HealthNormalized <= 0.6f)
        {
            phase = 2;
            baseTint = new Color(0.65f, 0.25f, 0.3f);
            GameHUD.Instance?.ShowAreaTitle("\"You were always the lesser blade, Aldric.\"", "Malvric's fury grows");
        }

        if (phase == 2 && HealthNormalized <= 0.3f)
        {
            phase = 3;
            if (!summoned)
            {
                summoned = true;
                StartCoroutine(Summon());
            }
        }

        if (health <= 0)
            Die();
    }

    private IEnumerator Summon()
    {
        GameHUD.Instance?.ShowToast("\"Rise, my guard! Rise and serve your king!\"");
        if (summonPrefabs == null || summonPoints == null)
            yield break;
        for (int i = 0; i < summonPoints.Length && i < summonPrefabs.Length; i++)
        {
            if (summonPrefabs[i] == null || summonPoints[i] == null) continue;
            GameObject g = Instantiate(summonPrefabs[i], summonPoints[i].position, Quaternion.identity);
            g.SetActive(true);
            yield return new WaitForSeconds(0.4f);
        }
    }

    private void Die()
    {
        dead = true;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("IsDead", true);
        anim.SetTrigger("Death");
        if (sr != null) sr.color = baseTint;
        foreach (Collider2D c in GetComponents<Collider2D>()) c.enabled = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        EnemyCore.ReportDeath(gameObject);
    }
}

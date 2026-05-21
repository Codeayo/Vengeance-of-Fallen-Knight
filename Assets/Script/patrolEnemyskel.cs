using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;
    public Rigidbody2D rb;

    [Header("Settings")]
    public float patrolSpeed = 1.2f;
    public float chaseSpeed = 2.2f;
    public float chaseRange = 5f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1f;

    int health = 50;
    float lastAttackTime = 0;
    bool isDead = false;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        Flip();

        if (dist <= attackRange)
        {
            PlayerInRange();
        }
        else if (dist <= chaseRange)
        {
            PlayerDetected();
        }
        else
        {
            Patrol();
        }
    }

    // ---------------- STATE: PATROL ----------------
    void Patrol()
    {
        animator.SetBool("PlayerDetected", false);
        animator.SetBool("PlayerInRange", false);

        // Walk in facing direction
        float moveDir = (transform.localScale.x > 0) ? -1 : 1;
        rb.linearVelocity = new Vector2(moveDir * patrolSpeed, rb.linearVelocity.y);
    }

    // ---------------- STATE: CHASE ----------------
    void PlayerDetected()
    {
        animator.SetBool("PlayerDetected", true);
        animator.SetBool("PlayerInRange", false);

        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(dir.x * chaseSpeed, rb.linearVelocity.y);
    }

    // ---------------- STATE: ATTACK ----------------
    void PlayerInRange()
    {
        animator.SetBool("PlayerDetected", false);
        animator.SetBool("PlayerInRange", true);

        rb.linearVelocity = Vector2.zero;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            animator.SetTrigger("Attack");
        }
    }

    // ---------------- DAMAGE SYSTEM ----------------
    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        health -= dmg;
        animator.SetTrigger("Hurt");

        if (health <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        animator.SetBool("Dead", true);

        foreach (Collider2D c in GetComponents<Collider2D>())
            c.enabled = false;

        Destroy(gameObject, 3f);
    }

    // ---------------- LOOK AT PLAYER ----------------
    void Flip()
    {
        if (player.position.x > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }
}

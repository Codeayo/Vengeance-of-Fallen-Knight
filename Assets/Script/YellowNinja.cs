using UnityEngine;

public class YellowNinja : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 1.5f;
    public float chaseSpeed = 2.5f;
    public float stopDistance = 1.0f; // how close to get when chasing
    public float retrieveDistance = 3f;

    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public float groundCheckDistance = 0.3f;
    public LayerMask whatIsGround;

    [Header("Player Detection")]
    public float detectionRadius = 6f;
    public float attackRadius = 1f;
    public LayerMask whatIsPlayer;
    public Vector3 detectionOffset;

    [Header("Attack")]
    public Transform attackPoint;
    public float attackCooldown = 1f;
    public int attackDamage = 10;

    [Header("Health")]
    public int maxHealth = 30;
    public GameObject floatingTextPrefab;
    public Transform textSpawnPoint;

    // internal state
    int currentHealth;
    bool facingLeft = true;
    bool isAttacking = false;
    bool isDead = false;
    float lastAttackTime = 0f;

    Rigidbody2D rb;
    Animator animator;
    Transform player;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    void Update()
    {
        if (player == null || isDead) return;

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackRadius)
        {
            TryStartAttack();
        }
        else if (dist <= detectionRadius)
        {
            ChasePlayer(dist);
        }
        else
        {
            PatrolMovement();
        }
    }

    // ---------------- PATROL ----------------
    void PatrolMovement()
    {
        float dir = facingLeft ? -1f : 1f;
        rb.linearVelocity = new Vector2(dir * walkSpeed, rb.linearVelocity.y);
        animator.SetBool("Attack", false);

        if (groundCheckPoint != null)
        {
            RaycastHit2D ground = Physics2D.Raycast(
                groundCheckPoint.position,
                Vector2.down,
                groundCheckDistance,
                whatIsGround
            );

            if (!ground)
                Flip();
        }
    }

    // ---------------- CHASE ----------------
    void ChasePlayer(float distance)
    {
        animator.SetBool("Attack", false);

        float dirToPlayer = player.position.x - transform.position.x;

        // flip only when clearly on one side
        if (Mathf.Abs(dirToPlayer) > 0.25f)
        {
            bool wantLeft = dirToPlayer < 0f;
            if (wantLeft != facingLeft)
                FlipTo(wantLeft);
        }

        float moveDir = facingLeft ? -1f : 1f;

        // maintain a small gap from player
        float targetX = player.position.x - moveDir * stopDistance;
        Vector2 targetPos = new Vector2(targetX, rb.position.y);

        rb.position = Vector2.MoveTowards(
            rb.position,
            targetPos,
            chaseSpeed * Time.deltaTime
        );
    }

    // ---------------- ATTACK ----------------
    void TryStartAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        if (!PlayerInFront()) return;

        isAttacking = true;
        lastAttackTime = Time.time;

        animator.SetBool("Attack", true);
        rb.linearVelocity = Vector2.zero;

        Invoke(nameof(DoDamage), 0.25f);
        Invoke(nameof(EndAttack), 0.6f);
    }

    bool PlayerInFront()
    {
        float dirToPlayer = player.position.x - transform.position.x;
        float face = facingLeft ? -1 : 1;
        return Mathf.Sign(dirToPlayer) == Mathf.Sign(face);
    }

    void DoDamage()
    {
        if (attackPoint == null) return;

        Collider2D hit = Physics2D.OverlapCircle(
            attackPoint.position,
            attackRadius,
            whatIsPlayer
        );

        if (hit)
        {
            Debug.Log("Player hit by Yellow Ninja");

            // Uncomment once Player.instance exists:
            // hit.GetComponent<Player>()?.TakeDamage(attackDamage);
        }
    }

    void EndAttack()
    {
        isAttacking = false;
        animator.SetBool("Attack", false);
    }

    // ---------------- DAMAGE SYSTEM ----------------
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        animator.SetTrigger("Hurt");

        if (floatingTextPrefab != null && textSpawnPoint != null)
            Instantiate(floatingTextPrefab, textSpawnPoint.position, Quaternion.identity);

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;
        isAttacking = true;

        animator.SetTrigger("Death");
        rb.linearVelocity = Vector2.zero;

        foreach (Collider2D c in GetComponents<Collider2D>())
            c.enabled = false;

        Destroy(gameObject, 2f);
    }

    // ---------------- FLIP ----------------
    void FlipTo(bool makeLeft)
    {
        facingLeft = makeLeft;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (facingLeft ? 1 : -1);
        transform.localScale = s;
    }

    void Flip() => FlipTo(!facingLeft);

    // ---------------- GIZMOS ----------------
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + detectionOffset, detectionRadius);

        Gizmos.color = Color.green;
        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);

        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                groundCheckPoint.position,
                groundCheckPoint.position + Vector3.down * groundCheckDistance
            );
        }
    }
}

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class YellowNinja : MonoBehaviour, IBossHealth
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
    [Tooltip("Centre-to-centre distance at which the ninja starts a strike (bodies cannot overlap).")]
    public float attackTriggerDistance = 2.8f;
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
    public float HealthNormalized => maxHealth > 0 ? Mathf.Clamp01(currentHealth / (float)maxHealth) : 0f;
    public bool IsDead => isDead;
    bool facingLeft = true;
    bool isAttacking = false;
    bool isDead = false;
    float lastAttackTime = 0f;

    Rigidbody2D rb;
    Animator animator;
    Transform player;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = Mathf.Max(1, maxHealth);
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    void Update()
    {
        if (isDead) return;
        if (player == null || (player.GetComponent<Player>() != null && player.GetComponent<Player>().IsDead))
        {
            PatrolMovement();
            return;
        }

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackTriggerDistance)
        {
            TryStartAttack();
        }
        else if (dist <= detectionRadius)
        {
            ChasePlayer();
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
        if (rb != null)
            rb.linearVelocity = new Vector2(dir * walkSpeed, rb.linearVelocity.y);
        if (animator != null)
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

    bool alerted;
    Vector3 lastPos;

    void LateUpdate()
    {
        if (animator == null) return;
        float speed = Mathf.Abs(transform.position.x - lastPos.x) / Mathf.Max(0.0001f, Time.deltaTime);
        lastPos = transform.position;
        animator.SetFloat("Speed", isDead || isAttacking ? 0f : speed);
    }

    // ---------------- CHASE ----------------
    void ChasePlayer()
    {
        if (!alerted)
        {
            alerted = true;
            EnemyVoice.Of(gameObject)?.Alert();
        }
        if (animator != null)
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

        // Never chase off a ledge.
        if (!EnemyCore.GroundAhead(EnemyCore.Feet(gameObject), moveDir, 0.7f, 2.5f))
            return;

        if (rb != null)
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

        if (animator != null)
            animator.SetBool("Attack", true);
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Invoke(nameof(DoDamage), 0.25f);
        EnemyVoice.Of(gameObject)?.Swing();
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
            Player playerHit = hit.GetComponentInParent<Player>();
            if (playerHit != null)
            {
                playerHit.TakeDamageFrom(attackDamage, transform.position);
                EnemyVoice.Of(gameObject)?.Impact();
            }
        }
    }

    void EndAttack()
    {
        isAttacking = false;
        if (animator != null)
            animator.SetBool("Attack", false);
    }

    // ---------------- DAMAGE SYSTEM ----------------
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        if (amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth > 0) EnemyVoice.Of(gameObject)?.Hurt();
        if (animator != null)
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
        EnemyCore.ReportDeath(gameObject);

        CancelInvoke();
        if (animator != null)
            animator.SetTrigger("Death");
        if (rb != null)
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

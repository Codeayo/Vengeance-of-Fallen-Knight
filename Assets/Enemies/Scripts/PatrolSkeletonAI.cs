using UnityEngine;

public class PatrolSkeletonAI : MonoBehaviour, IBossHealth
{
    [Header("References")]
    public Transform player;
    public Animator animator;
    public Rigidbody2D rb;
    public Transform groundCheckPoint;

    [Header("Settings")]
    public float patrolSpeed = 1.2f;
    public float chaseSpeed = 2.2f;
    public float chaseRange = 5f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1f;
    public float groundCheckDistance = 1.5f;
    public LayerMask groundLayer;
    public int attackDamage = 10;

    [SerializeField] int maxHealth = 50;
    [Tooltip("Delay between the attack animation starting and the hit landing (readable telegraph).")]
    public float attackWindup = 0.35f;
    [Tooltip("Bosses: faster and more aggressive below half health.")]
    public bool enrageAtHalf;
    bool enraged;
    bool attacking;
    int health;

    public float HealthNormalized => maxHealth > 0 ? Mathf.Clamp01(health / (float)maxHealth) : 0f;
    public bool IsDead => isDead;
    float lastAttackTime = 0;
    int patrolDirection = -1;
    bool isDead = false;

    void Awake()
    {
        health = Mathf.Max(1, maxHealth);
    }

    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            patrolDirection = spriteRenderer.flipX ? -1 : 1;

        if (groundCheckPoint == null)
            groundCheckPoint = transform.Find("GroundCheck");

        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Ground");

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (isDead) return;
        if (player == null || PlayerIsDead())
        {
            Patrol();
            return;
        }

        if (attacking)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (enrageAtHalf && !enraged && HealthNormalized <= 0.5f)
        {
            enraged = true;
            chaseSpeed *= 1.35f;
            attackCooldown *= 0.7f;
            attackWindup *= 0.8f;
            GameHUD.Instance?.ShowToast("It rages!");
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackRange)
        {
            FacePlayer();
            PlayerInRange();
        }
        else if (dist <= chaseRange)
        {
            FacePlayer();
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
        if (animator != null)
        {
            animator.SetBool("PlayerDetected", false);
            animator.SetBool("PlayerInRange", false);
        }

        if (groundCheckPoint != null)
        {
            Vector3 checkPosition = groundCheckPoint.position;
            checkPosition.x = transform.position.x + Mathf.Abs(groundCheckPoint.localPosition.x) * patrolDirection;
            bool wallAhead = Physics2D.Raycast((Vector2)EnemyCore.Feet(gameObject) + Vector2.up * 0.6f, Vector2.right * patrolDirection, 1.2f, groundLayer);
            if (wallAhead || !Physics2D.Raycast(checkPosition, Vector2.down, groundCheckDistance, groundLayer))
                patrolDirection *= -1;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.flipX = patrolDirection < 0;

        if (rb != null)
            rb.linearVelocity = new Vector2(patrolDirection * patrolSpeed, rb.linearVelocity.y);
    }

    // ---------------- STATE: CHASE ----------------
    void PlayerDetected()
    {
        if (animator != null)
        {
            animator.SetBool("PlayerDetected", true);
            animator.SetBool("PlayerInRange", false);
        }

        Vector2 dir = (player.position - transform.position).normalized;
        // Never chase off a ledge: hold position at the edge instead.
        bool safe = EnemyCore.GroundAhead(EnemyCore.Feet(gameObject), dir.x, 1.0f, 2.5f);
        if (animator != null && !safe)
            animator.SetBool("PlayerDetected", false);
        if (rb != null)
            rb.linearVelocity = new Vector2(safe ? dir.x * chaseSpeed : 0f, rb.linearVelocity.y);
    }

    // ---------------- STATE: ATTACK ----------------
    void PlayerInRange()
    {
        if (animator != null)
        {
            animator.SetBool("PlayerDetected", false);
            animator.SetBool("PlayerInRange", true);
        }

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            StartCoroutine(AttackRoutine());
        }
    }

    System.Collections.IEnumerator AttackRoutine()
    {
        attacking = true;
        if (animator != null)
            animator.SetTrigger("Attack");
        yield return new WaitForSeconds(attackWindup);

        // The hit only lands if the knight is still in reach when the blow falls (rolling away works).
        if (!isDead && player != null && Vector2.Distance(transform.position, player.position) <= attackRange * 1.15f)
        {
            Player playerCharacter = player.GetComponentInParent<Player>();
            if (playerCharacter != null)
                playerCharacter.TakeDamage(attackDamage);
        }

        yield return new WaitForSeconds(0.25f);
        attacking = false;
    }

    bool PlayerIsDead()
    {
        Player p = player != null ? player.GetComponent<Player>() : null;
        return p != null && p.IsDead;
    }

    // ---------------- DAMAGE SYSTEM ----------------
    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        if (dmg <= 0) return;

        health -= dmg;
        if (animator != null)
            animator.SetTrigger("Hurt");

        if (health <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;
        StopAllCoroutines();
        attacking = false;
        EnemyCore.ReportDeath(gameObject);
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetBool("Dead", true);

        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        Destroy(gameObject, 3f);
    }

    // ---------------- LOOK AT PLAYER ----------------
    void FacePlayer()
    {
        patrolDirection = player.position.x >= transform.position.x ? 1 : -1;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.flipX = patrolDirection < 0;
    }
}

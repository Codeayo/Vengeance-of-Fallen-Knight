using UnityEngine;

public class PatrolSkeletonAI : MonoBehaviour
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
        if (player == null)
        {
            Patrol();
            return;
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
            if (!Physics2D.Raycast(checkPosition, Vector2.down, groundCheckDistance, groundLayer))
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
        if (rb != null)
            rb.linearVelocity = new Vector2(dir.x * chaseSpeed, rb.linearVelocity.y);
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
            if (animator != null)
                animator.SetTrigger("Attack");

            Player playerCharacter = player.GetComponentInParent<Player>();
            if (playerCharacter != null)
                playerCharacter.TakeDamage(attackDamage);
        }
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

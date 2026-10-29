using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private KeyCode menuKey = KeyCode.P;
    [SerializeField] private bool autoDisableMenu = true;
    [SerializeField] private float autoDisableSeconds = 5f;
    private Coroutine autoDisableCoroutine;

    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Jump")]
    public float jumpForce = 14f;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private int maxJumps = 2;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;

    [Header("Combat")]
    public Transform attackPoint;
    public float attackRange = 0.8f;
    public LayerMask enemyLayer;
    public int attackDamage = 10;
    public float attackDelay = 0.12f;
    public float attackCooldown = 0.5f;

    [Header("Health")]
    public Slider healthSlider;
    public int maxHealth = 100;

    // Animator parameters
    const string RUN = "IsRunning";
    const string ATTACK = "Attack";
    const string JUMP = "Jump";
    const string ROLL = "Roll";
    const string HURT = "Hurt";
    const string HEAL = "IsHealing";
    const string BLOCK = "IsBlocking";
    const string DEATH = "Death";

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;

    float moveInput;
    float lastAttackTime;
    int currentHealth;
    bool isDead = false;
    bool warnedAboutGroundLayer;
    bool warnedAboutMissingGroundCollider;
    int jumpsRemaining;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        jumpsRemaining = Mathf.Max(1, maxJumps);

        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    void Update()
    {
        if (isDead) return;

        if (Input.GetKeyDown(menuKey))
            ToggleMenu();

        if (menuPanel != null && menuPanel.activeSelf)
            return;

        moveInput = Input.GetAxisRaw("Horizontal");

        Move();
        Jump();
        Attack();
        Roll();
        Block();
        Heal();
    }

    void ToggleMenu()
    {
        if (menuPanel == null)
        {
            Debug.LogWarning("Menu panel is not assigned to Player.");
            return;
        }

        bool willOpen = !menuPanel.activeSelf;
        menuPanel.SetActive(willOpen);

        if (willOpen)
        {
            Time.timeScale = 0f;

            if (autoDisableMenu)
            {
                if (autoDisableCoroutine != null)
                    StopCoroutine(autoDisableCoroutine);

                autoDisableCoroutine = StartCoroutine(AutoDisableMenuRoutine(autoDisableSeconds));
            }
        }
        else
        {
            Time.timeScale = 1f;

            if (autoDisableCoroutine != null)
            {
                StopCoroutine(autoDisableCoroutine);
                autoDisableCoroutine = null;
            }
        }
    }

    private IEnumerator AutoDisableMenuRoutine(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        if (menuPanel != null && menuPanel.activeSelf)
        {
            menuPanel.SetActive(false);
            Time.timeScale = 1f;
            autoDisableCoroutine = null;
        }
    }

    private void OnDisable()
    {
        if (menuPanel != null && menuPanel.activeSelf)
            menuPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    // ---------------- MOVEMENT -----------------
    void Move()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        anim.SetBool(RUN, Mathf.Abs(moveInput) > 0.1f);

        if (moveInput > 0) sr.flipX = false;
        if (moveInput < 0) sr.flipX = true;
    }

    // ---------------- JUMP -----------------
    void Jump()
    {
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(jumpKey);

        try
        {
            jumpPressed |= Input.GetButtonDown("Jump");
        }
        catch (UnityException)
        {
            // The legacy input action may not exist when only the Input System is enabled.
        }

        bool grounded = IsGrounded();
        if (grounded)
            jumpsRemaining = Mathf.Max(1, maxJumps);

        if (jumpPressed && jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.SetTrigger(JUMP);
            jumpsRemaining--;
        }
    }

    bool IsGrounded()
    {
        if (groundLayer.value == 0)
        {
            if (!warnedAboutGroundLayer)
            {
                Debug.LogWarning("Player Ground Layer is empty. Assign the platform layer in the Inspector.", this);
                warnedAboutGroundLayer = true;
            }

            return false;
        }

        if (groundCheck != null)
        {
            bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            if (!grounded && !warnedAboutMissingGroundCollider && rb.linearVelocity.y < -5f)
            {
                Debug.LogWarning("Player is falling without detecting ground. Check the Graveyard tilemap collider and Ground layer.", this);
                warnedAboutMissingGroundCollider = true;
            }

            return grounded;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(0f, -col.bounds.extents.y - 0.02f);
            return Physics2D.OverlapCircle(origin, Mathf.Max(groundCheckRadius, 0.12f), groundLayer);
        }

        return false;
    }

    // ---------------- ATTACK -----------------
    void Attack()
    {
        if (Input.GetKeyDown(KeyCode.J) && Time.time > lastAttackTime + attackCooldown)
        {
            anim.SetTrigger(ATTACK);
            lastAttackTime = Time.time;
            StartCoroutine(DoDamage());
        }
    }

    IEnumerator DoDamage()
    {
        yield return new WaitForSeconds(attackDelay);

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayer
        );

        foreach (var h in hits)
            h.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
    }

    // ---------------- ROLL -----------------
    void Roll()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
            anim.SetTrigger(ROLL);
    }

    // ---------------- BLOCK -----------------
    void Block()
    {
        anim.SetBool(BLOCK, Input.GetMouseButton(1));
    }

    // ---------------- HEAL -----------------
    void Heal()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            HealPlayer(20);
            anim.SetBool(HEAL, true);
            StartCoroutine(StopHealAnim());
        }
    }

    IEnumerator StopHealAnim()
    {
        yield return new WaitForSeconds(0.5f);
        anim.SetBool(HEAL, false);
    }

    void HealPlayer(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (healthSlider != null)
            healthSlider.value = currentHealth;
    }

    // ---------------- TAKE DAMAGE -----------------
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        anim.SetTrigger(HURT);

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        if (currentHealth <= 0)
            Die();
    }

    // ---------------- DEATH -----------------
    void Die()
    {
        isDead = true;
        anim.SetBool(DEATH, true);
        rb.linearVelocity = Vector2.zero;

        foreach (Collider2D c in GetComponents<Collider2D>())
            c.enabled = false;

        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOverBackground();

        this.enabled = false;

        Destroy(gameObject, 2f);
    }

    // ---------------- GIZMOS -----------------
    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}

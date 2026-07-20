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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

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
        Jump();   // infinite jump
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

    // ---------------- MOVEMENT -----------------
    void Move()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        anim.SetBool(RUN, Mathf.Abs(moveInput) > 0.1f);

        if (moveInput > 0) sr.flipX = false;
        if (moveInput < 0) sr.flipX = true;
    }

    // ---------------- INFINITE JUMP -----------------
    void Jump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.SetTrigger(JUMP);
        }
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
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

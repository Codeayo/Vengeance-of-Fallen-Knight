using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class Enemy1 : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;

    [Header("Patrol")]
    public float walkSpeed = 1f;
    public Transform groundCheckPoint;
    public float distance = 0.7f;
    public LayerMask whatIsGround;

    [Header("Player Detection")]
    public float attackRangeRadius = 7f;
    public LayerMask whatIsPlayer;
    public GameObject player;
    public float chaseSpeed = 2f;
    public float retrieveDistance = 3f;

    [Header("Combat")]
    public GameObject floatingTextPrefab;
    public Transform textSpawnPoint;
    public Transform attackPoint;
    public float attackRadius = 1.71f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector3 attackPointFacingRightPosition;
    private int currentHealth;
    private int patrolDirection;
    private float lastAttackTime;
    private bool isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = Mathf.Max(1, maxHealth);
        patrolDirection = spriteRenderer != null && spriteRenderer.flipX ? -1 : 1;

        if (attackPoint != null)
            attackPointFacingRightPosition = attackPoint.localPosition;
    }

    private void Start()
    {
        if (player == null || !player.scene.IsValid())
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject;
        }

        if (whatIsGround.value == 0)
            whatIsGround = LayerMask.GetMask("Ground");

        if (whatIsPlayer.value == 0)
            whatIsPlayer = LayerMask.GetMask("Default", "Player");
    }

    private void Update()
    {
        if (isDead)
            return;

        if (player == null || !player.scene.IsValid())
        {
            Patrol();
            return;
        }

        float playerDistance = Vector2.Distance(transform.position, player.transform.position);
        if (playerDistance > attackRangeRadius)
        {
            Patrol();
            return;
        }

        FacePlayer();
        if (playerDistance <= attackRadius)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            TryAttack();
        }
        else
        {
            float direction = Mathf.Sign(player.transform.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(direction * chaseSpeed, rb.linearVelocity.y);
        }
    }

    private void Patrol()
    {
        if (groundCheckPoint != null &&
            !Physics2D.Raycast(groundCheckPoint.position, Vector2.down, distance, whatIsGround))
            patrolDirection *= -1;

        SetFacing(patrolDirection);
        rb.linearVelocity = new Vector2(patrolDirection * walkSpeed, rb.linearVelocity.y);
    }

    private void FacePlayer()
    {
        float direction = player.transform.position.x >= transform.position.x ? 1f : -1f;
        SetFacing(direction > 0f ? 1 : -1);
    }

    private void SetFacing(int direction)
    {
        patrolDirection = direction;
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction < 0;

        if (attackPoint != null)
        {
            attackPoint.localPosition = new Vector3(
                Mathf.Abs(attackPointFacingRightPosition.x) * direction,
                attackPointFacingRightPosition.y,
                attackPointFacingRightPosition.z
            );
        }
    }

    private void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;
        SetAnimatorSignal("Attack");

        Collider2D hit = attackPoint != null
            ? Physics2D.OverlapCircle(attackPoint.position, attackRadius, whatIsPlayer)
            : null;

        Player playerHit = hit != null ? hit.GetComponentInParent<Player>() : null;
        if (playerHit != null)
            playerHit.TakeDamage(attackDamage);
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        SetAnimatorSignal("Hurt");

        if (floatingTextPrefab != null && textSpawnPoint != null)
            Instantiate(floatingTextPrefab, textSpawnPoint.position, Quaternion.identity);

        if (currentHealth == 0)
            Die();
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        SetAnimatorSignal("Death", true);

        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = false;

        Destroy(gameObject, 2f);
    }

    private void SetAnimatorSignal(string parameterName, bool keepBool = false)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name != parameterName)
                continue;

            if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(parameterName);
            }
            else if (parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, true);
                if (!keepBool)
                {
                    CancelInvoke(nameof(ResetTemporaryAnimatorBools));
                    Invoke(nameof(ResetTemporaryAnimatorBools), 0.25f);
                }
            }

            return;
        }
    }

    private void ResetTemporaryAnimatorBools()
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool &&
                parameter.name != "Death" && parameter.name != "IsDead")
                animator.SetBool(parameter.name, false);
        }
    }
}
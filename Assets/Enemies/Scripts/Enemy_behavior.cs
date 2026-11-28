using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy_behaviour : MonoBehaviour
{
    public Transform rayCast;
    public LayerMask raycaskMask;
    public float rayCastLength = 2f;
    public float attackDistance = 1f;
    public float moveSpeed = 1f;
    public float timer = 2f;
    public int attackDamage = 10;
    public LayerMask groundLayer = 1 << 3;
    public float groundSnapDistance = 10f;
    public float groundClearance = 0.02f;
    public int maxHealth = 30;

    private RaycastHit2D hit;
    private GameObject target;
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float distance;
    private bool inRange;
    private bool cooling;
    private bool isDead;
    private float intTimer;
    private int currentHealth;

    void Awake()
    {
        intTimer = timer;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        currentHealth = Mathf.Max(1, maxHealth);
    }

    void Start()
    {
        SnapToGround();
    }

    void FixedUpdate()
    {
        if (!isDead)
            SnapToGround();
    }

    void SnapToGround()
    {
        if (rb == null)
            return;

        // Only solid body colliders define the feet; triggers (e.g. the detection radius) are ignored.
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        bool hasBounds = false;
        Bounds bounds = default;
        foreach (Collider2D collider in colliders)
        {
            if (collider.isTrigger || !collider.enabled)
                continue;
            if (!hasBounds) { bounds = collider.bounds; hasBounds = true; }
            else bounds.Encapsulate(collider.bounds);
        }

        if (!hasBounds)
            return;

        Vector2 rayOrigin = new Vector2(bounds.center.x, bounds.max.y + 0.1f);
        RaycastHit2D ground = default;
        RaycastHit2D[] groundHits = Physics2D.RaycastAll(rayOrigin, Vector2.down, groundSnapDistance, groundLayer);
        foreach (RaycastHit2D groundHit in groundHits)
        {
            if (groundHit.collider.transform.IsChildOf(transform))
                continue;

            ground = groundHit;
            break;
        }

        if (ground.collider != null && bounds.min.y < ground.point.y + groundClearance)
        {
            rb.position += Vector2.up * (ground.point.y + groundClearance - bounds.min.y);
            rb.linearVelocity = Vector2.zero;
        }
    }

    void Update()
    {
        if (isDead || Cinematic.IsPlaying)
            return;

        if (target == null)
        {
            inRange = false;
            if (anim != null)
                anim.SetBool("canWalk", false);
            StopAttack();
            return;
        }

        if (inRange)
        {
            if (rayCast != null)
            {
                Vector2 direction = target.transform.position.x >= transform.position.x
                    ? Vector2.right
                    : Vector2.left;
                hit = Physics2D.Raycast(rayCast.position, direction, rayCastLength, raycaskMask);
                Debug.DrawLine(rayCast.position, rayCast.position + (Vector3)direction * rayCastLength, Color.red);
            }
            else
            {
                hit = default;
            }
        }

        if (hit.collider != null)
        {
            EnemyLogic();
        }
        else
        {
            inRange = false;
        }

        if (!inRange)
        {
            if (anim != null)
                anim.SetBool("canWalk", false);
            StopAttack();
        }
    }

    void OnTriggerEnter2D(Collider2D trig)
    {
        if (trig.CompareTag("Player"))
        {
            if (target == null && !isDead)
            {
                EnemyVoice.Of(gameObject)?.Alert();
                if (HasAnimationParameter("Alert", AnimatorControllerParameterType.Trigger)) anim.SetTrigger("Alert");
            }
            target = trig.gameObject;
            inRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D trig)
    {
        if (trig.CompareTag("Player") && target == trig.gameObject)
        {
            target = null;
            inRange = false;
        }
    }

    void EnemyLogic()
    {
        distance = Vector2.Distance(transform.position, target.transform.position);
        if (spriteRenderer != null)
            spriteRenderer.flipX = target.transform.position.x < transform.position.x;

        if (distance > attackDistance)
        {
            Move();
            StopAttack();
        }
        else if (distance <= attackDistance && !cooling)
        {
            Attack();
        }

        if (cooling)
        {
            Cooldown();
        }
    }

    void Move()
    {
        if (anim != null)
            anim.SetBool("canWalk", true);

        // Don't move during attack animation
        if (anim == null || !anim.GetCurrentAnimatorStateInfo(0).IsName("Skel_attack"))
        {
            float dir = Mathf.Sign(target.transform.position.x - transform.position.x);
            // Never walk off a ledge while chasing.
            if (!EnemyCore.GroundAhead(EnemyCore.Feet(gameObject), dir, 0.8f, 2.5f))
                return;
            Vector2 targetPosition = new Vector2(target.transform.position.x, transform.position.y);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
    }

    void Attack()
    {
        if (target == null || Vector2.Distance(transform.position, target.transform.position) > attackDistance)
        {
            StopAttack();
            return;
        }

        timer = intTimer;
        if (anim != null)
            anim.SetBool("Attack", true);
        cooling = true;

        CancelInvoke(nameof(LandHit));
        Invoke(nameof(LandHit), 0.3f);
        EnemyVoice.Of(gameObject)?.Swing();
    }

    // The axe lands after a short wind-up, and only if the knight is still in reach.
    void LandHit()
    {
        if (isDead || target == null)
            return;
        if (Vector2.Distance(transform.position, target.transform.position) > attackDistance * 1.15f)
            return;
        Player playerHit = target.GetComponentInParent<Player>();
        if (playerHit != null)
        {
            playerHit.TakeDamageFrom(attackDamage, transform.position);
            EnemyVoice.Of(gameObject)?.Impact();
        }
    }

    void StopAttack()
    {
        if (anim != null)
            anim.SetBool("Attack", false);
        cooling = false;
        timer = intTimer;
    }

    void Cooldown()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            cooling = false;
            timer = intTimer;
            if (anim != null)
                anim.SetBool("Attack", false);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth > 0) EnemyVoice.Of(gameObject)?.Hurt();
        if (HasAnimationParameter("Hurt", AnimatorControllerParameterType.Trigger))
            anim.SetTrigger("Hurt");

        if (currentHealth == 0)
            Die();
    }

    private void Die()
    {
        isDead = true;
        cooling = false;
        CancelInvoke();
        EnemyCore.ReportDeath(gameObject);

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (HasAnimationParameter("Death", AnimatorControllerParameterType.Trigger))
            anim.SetTrigger("Death");
        else if (HasAnimationParameter("Dead", AnimatorControllerParameterType.Bool))
            anim.SetBool("Dead", true);

        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        // Keep the corpse on the ground while the death animation plays.
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }

        Destroy(gameObject, 2f);
    }

    private bool HasAnimationParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (anim == null)
            return false;

        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.name == parameterName && parameter.type == type)
                return true;
        }

        return false;
    }
}

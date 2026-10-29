using UnityEngine;

public class Enemy_behaviour : MonoBehaviour
{
    public Transform rayCast;
    public LayerMask raycaskMask;
    public float rayCastLength = 2f;
    public float attackDistance = 1f;
    public float moveSpeed = 1f;
    public float timer = 2f;
    public int attackDamage = 10;
    public LayerMask groundLayer = 9;
    public float groundSnapDistance = 10f;
    public float groundClearance = 0.02f;

    private RaycastHit2D hit;
    private GameObject target;
    private Animator anim;
    private Rigidbody2D rb;
    private float distance;
    private bool inRange;
    private bool cooling;
    private bool isDead;
    private float intTimer;

    void Awake()
    {
        intTimer = timer;
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        SnapToGround();
    }

    void FixedUpdate()
    {
        if (!isDead)
            SnapToGround();
    }

    void SnapToGround()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        if (colliders.Length == 0)
            return;

        Bounds bounds = colliders[0].bounds;
        foreach (Collider2D collider in colliders)
            bounds.Encapsulate(collider.bounds);

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
                hit = Physics2D.Raycast(rayCast.position, Vector2.left, rayCastLength, raycaskMask);
                Debug.DrawLine(rayCast.position, rayCast.position + Vector3.left * rayCastLength, Color.red);
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
            target = trig.gameObject;
            inRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D trig)
    {
        if (trig.CompareTag("Player"))
        {
            target = null;
            inRange = false;
        }
    }

    void EnemyLogic()
    {
        distance = Vector2.Distance(transform.position, target.transform.position);

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
            anim.SetBool("Attack", false);
            Cooldown();
        }
    }

    void Move()
    {
        anim.SetBool("canWalk", true);

        // Don't move during attack animation
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Skel_attack"))
        {
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
        anim.SetBool("Attack", true);
        cooling = true;
        if (target != null)
        {
            Player playerHit = target.GetComponent<Player>();
            if (playerHit != null)
                playerHit.TakeDamage(attackDamage);
        }
    }

    void StopAttack()
    {
        anim.SetBool("Attack", false);
        cooling = false;
    }

    void Cooldown()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            cooling = false;
            timer = intTimer;
        }
    }
}

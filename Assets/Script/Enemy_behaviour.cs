using UnityEngine;

public class Enemy_behaviour : MonoBehaviour
{
    public Transform rayCast;
    public LayerMask raycaskMask;
    public float rayCastLength = 2f;
    public float attackDistance = 1f;
    public float moveSpeed = 1f;
    public float timer = 2f;

    private RaycastHit2D hit;
    private GameObject target;
    private Animator anim;
    private float distance;
    private bool attackmode;
    private bool inRange;
    private bool cooling;
    private float intTimer;

    void Awake()
    {
        intTimer = timer;
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (inRange)
        {
            hit = Physics2D.Raycast(rayCast.position, Vector2.left, rayCastLength, raycaskMask);
            Debug.DrawLine(rayCast.position, rayCast.position + Vector3.left * rayCastLength, Color.red);
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

        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Skel_attack"))
        {
            Vector2 targetPosition = new Vector2(target.transform.position.x, transform.position.y);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
    }

    void Attack()
    {
        timer = intTimer;
        attackmode = true;
        anim.SetBool("Attack", true);
    }

    void StopAttack()
    {
        cooling = false;
        attackmode = false;
        anim.SetBool("Attack", false);
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

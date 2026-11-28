using UnityEngine;

/// <summary>
/// Every enemy's sounds: footsteps timed to movement, idle noises, alert, attack swing, impacts,
/// hurt and death. The voice set is chosen from the enemy's AI type; bosses get their own roar.
/// Added automatically by EnemyCore, so every enemy in every level is covered.
/// </summary>
public class EnemyVoice : MonoBehaviour
{
    public enum Kind { Skeleton, Axe, Ninja, Knight }

    public Kind kind;
    public string roar;     // boss roar key, empty for regular enemies
    public float pitch = 1f;

    private Vector3 lastPos;
    private float stepTimer;
    private float idleTimer;
    private bool dead;

    public static EnemyVoice Of(GameObject go) => go != null ? go.GetComponent<EnemyVoice>() : null;

    private void Awake()
    {
        if (GetComponent<PatrolSkeletonAI>() != null) kind = Kind.Skeleton;
        else if (GetComponent<Enemy_behaviour>() != null) kind = Kind.Axe;
        else if (GetComponent<YellowNinja>() != null) kind = Kind.Ninja;
        else kind = Kind.Knight;
        float scale = Mathf.Abs(transform.localScale.y);
        pitch = Mathf.Clamp(1.15f - (scale - 1f) * 0.45f, 0.6f, 1.2f); // bigger enemies sound deeper
        lastPos = transform.position;
        idleTimer = Random.Range(4f, 10f);
    }

    public void SetBossRoar(string key) => roar = key;

    private void Update()
    {
        if (dead) return;
        Vector3 p = transform.position;
        float speed = Mathf.Abs(p.x - lastPos.x) / Mathf.Max(0.0001f, Time.deltaTime);
        lastPos = p;

        if (speed > 0.3f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer = Mathf.Clamp(1.1f / speed, 0.2f, 0.55f) / Mathf.Max(0.6f, pitch);
                Play(kind == Kind.Skeleton || kind == Kind.Axe ? "skel_step" : kind == Kind.Ninja ? "ninja_step" : MusicManager.Instance?.FootstepKey ?? "step_stone", 0.35f, 0.08f);
            }
        }

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            idleTimer = Random.Range(6f, 14f);
            if (kind == Kind.Skeleton) Play("bone_rattle", 0.4f, 0.1f);
            else if (kind == Kind.Axe) Play("ghoul_groan", 0.35f, 0.1f);
        }
    }

    public void Alert()
    {
        switch (kind)
        {
            case Kind.Ninja: Play("ninja_alert", 0.8f); Play("blade_draw", 0.5f); break;
            case Kind.Knight: break;
            default: Play("skel_alert", 0.75f); break;
        }
    }

    public void Swing(bool heavy = false)
    {
        Play(heavy || pitch < 0.9f ? "swing_heavy" : "swing", 0.75f, 0.08f);
    }

    public void Impact()
    {
        Play(kind == Kind.Knight || kind == Kind.Ninja ? "hit" : "hit_bone", 0.8f);
        if (pitch < 0.9f) Play("boss_slam", 0.6f);
    }

    public void Hurt()
    {
        Play(kind == Kind.Ninja ? "ninja_hurt" : kind == Kind.Knight ? "clank" : "skel_hurt", 0.7f, 0.1f);
    }

    public void Death()
    {
        dead = true;
        Play(kind == Kind.Ninja ? "ninja_death" : kind == Kind.Knight ? "player_death" : "skel_death", 0.9f, 0.05f);
    }

    public void Roar()
    {
        if (!string.IsNullOrEmpty(roar)) Play(roar, 1f, 0.02f, 0f, 40f);
    }

    private void Play(string key, float volume, float variance = 0.06f, float cooldown = 0.06f, float maxDistance = 22f)
    {
        var mm = MusicManager.Instance;
        if (mm == null) return;
        // Pitch identity: bigger and boss enemies sound deeper.
        mm.PlayAt(key + "", transform.position, volume, variance, cooldown, maxDistance, key.StartsWith("roar_") ? 1f : pitch);
    }
}

using UnityEngine;

/// <summary>
/// Final-encounter trigger: when the player enters, the boss bar appears with the boss's name
/// and tracks its health until it falls.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossEncounter : MonoBehaviour
{
    [SerializeField] private PatrolSkeletonAI boss;
    [SerializeField] private string bossName = "The Gravewarden";
    [SerializeField] private string arrivalMessage = "The Gravewarden rises to bar your path";

    private bool started;
    private bool finished;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (started || boss == null || !other.CompareTag("Player"))
            return;
        started = true;
        GameHUD.Instance?.ShowBoss(bossName);
        GameHUD.Instance?.ShowToast(arrivalMessage);
    }

    private void Update()
    {
        if (!started || finished)
            return;

        float hp = boss != null && !boss.IsDead ? boss.HealthNormalized : 0f;
        GameHUD.Instance?.SetBossHealth(hp);
        if (hp <= 0f)
        {
            finished = true;
            GameHUD.Instance?.HideBoss(true);
        }
    }
}

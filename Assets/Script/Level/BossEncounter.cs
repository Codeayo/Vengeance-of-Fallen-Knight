using UnityEngine;

/// <summary>
/// Final-encounter trigger: when the player enters, the boss bar appears with the boss's name
/// and tracks its health until it falls.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossEncounter : MonoBehaviour
{
    [SerializeField] private MonoBehaviour boss; // any IBossHealth (skeleton, ninja, Malvric)
    [SerializeField] private string bossName = "The Gravewarden";
    [SerializeField] private string arrivalMessage = "The Gravewarden rises to bar your path";
    [SerializeField] private string defeatTitle = "Gravewarden Vanquished";
    [SerializeField] private string defeatSubtitle = "The cursed seal is broken";

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

        var b = boss as IBossHealth;
        float hp = b != null && !b.IsDead ? b.HealthNormalized : 0f;
        GameHUD.Instance?.SetBossHealth(hp);
        if (hp <= 0f)
        {
            finished = true;
            GameHUD.Instance?.HideBoss(true, defeatTitle, defeatSubtitle);
        }
    }
}

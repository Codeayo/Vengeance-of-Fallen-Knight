using System;
using UnityEngine;

/// <summary>
/// Shared enemy data and services used by every AI: experience reward, loot drops,
/// ledge detection and the death notification. AIs call EnemyCore.ReportDeath when they die.
/// </summary>
public class EnemyCore : MonoBehaviour
{
    [Serializable]
    public struct Loot
    {
        public string itemId;
        [Range(0f, 1f)] public float chance;
        public int min;
        public int max;
    }

    [SerializeField] private string displayName = "Enemy";
    [SerializeField] private int xpReward = 15;
    [SerializeField] private int coinsMin = 1;
    [SerializeField] private int coinsMax = 3;
    [SerializeField] private Loot[] loot = new Loot[0];
    [SerializeField] private bool isBoss;

    private bool reported;

    /// <summary>Raised for every enemy death: (enemy, xp).</summary>
    public static event Action<EnemyCore> Killed;

    public string DisplayName => displayName;
    public bool IsBoss => isBoss;
    public bool IsDead => reported;

    private void Awake()
    {
        var voice = GetComponent<EnemyVoice>();
        if (voice == null) voice = gameObject.AddComponent<EnemyVoice>();
        if (isBoss) voice.SetBossRoar(RoarFor(displayName));
    }

    /// <summary>Each boss has its own voice.</summary>
    public static string RoarFor(string name)
    {
        if (string.IsNullOrEmpty(name)) return "roar_gravewarden";
        if (name.Contains("Kage")) return "roar_kage";
        if (name.Contains("Colossus")) return "roar_colossus";
        if (name.Contains("Frost")) return "roar_frost";
        if (name.Contains("Dravek")) return "roar_dravek";
        if (name.Contains("Malvric")) return "roar_malvric";
        return "roar_gravewarden";
    }

    public void Configure(string name, int xp, int coinsLow, int coinsHigh, bool boss, params Loot[] drops)
    {
        displayName = name;
        xpReward = xp;
        coinsMin = coinsLow;
        coinsMax = coinsHigh;
        isBoss = boss;
        loot = drops ?? new Loot[0];
    }

    /// <summary>Called once by the AI when it dies.</summary>
    public static void ReportDeath(GameObject enemy)
    {
        if (enemy == null)
            return;
        EnemyCore core = enemy.GetComponent<EnemyCore>();
        if (core == null || core.reported)
            return;
        core.reported = true;
        EnemyVoice.Of(enemy)?.Death();
        core.GrantRewards();
        Killed?.Invoke(core);
    }

    private void GrantRewards()
    {
        PlayerProgress.AddXp(xpReward);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        var coins = player != null ? player.GetComponent<Player_Collectables>() : null;
        int c = UnityEngine.Random.Range(coinsMin, coinsMax + 1);
        if (coins != null && c > 0)
            coins.AddCoins(c);

        Vector3 dropAt = transform.position + Vector3.up * 0.6f;
        foreach (Loot l in loot)
        {
            if (string.IsNullOrEmpty(l.itemId) || UnityEngine.Random.value > l.chance)
                continue;
            int n = UnityEngine.Random.Range(Mathf.Max(1, l.min), Mathf.Max(1, l.max) + 1);
            ItemPickup.Spawn(l.itemId, n, dropAt + new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), 0f, 0f), null);
        }

        if (isBoss)
            GameHUD.Instance?.ShowToast($"{displayName} has fallen.  +{xpReward} experience");
    }

    /// <summary>True when there is ground ahead of the given foot point in the move direction.</summary>
    public static bool GroundAhead(Vector2 feet, float direction, float lookAhead = 0.9f, float depth = 2.2f)
    {
        Vector2 probe = feet + new Vector2(Mathf.Sign(direction) * lookAhead, 0.3f);
        return Physics2D.Raycast(probe, Vector2.down, depth, LayerMask.GetMask("Ground"));
    }

    /// <summary>World-space feet position from the solid colliders of an enemy.</summary>
    public static Vector2 Feet(GameObject enemy)
    {
        bool has = false;
        Bounds b = default;
        foreach (Collider2D c in enemy.GetComponentsInChildren<Collider2D>())
        {
            if (c.isTrigger || !c.enabled) continue;
            if (!has) { b = c.bounds; has = true; } else b.Encapsulate(c.bounds);
        }

        return has ? new Vector2(b.center.x, b.min.y) : (Vector2)enemy.transform.position;
    }
}

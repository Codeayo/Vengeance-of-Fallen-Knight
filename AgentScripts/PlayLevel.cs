using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Generic play-mode check for any level built with the level kit.</summary>
public static class PlayLevel
{
    static Player P => GameObject.FindGameObjectWithTag("Player")?.GetComponent<Player>();
    static string R(bool ok, string w) => (ok ? "PASS " : "FAIL ") + w + "\n";
    static async Task W(float s) => await Task.Delay((int)(s * 1000));

    static void Teleport(Vector3 at, float lift = 0.5f)
    {
        var p = P; var rb = p.GetComponent<Rigidbody2D>();
        var hit = Physics2D.Raycast(new Vector2(at.x, at.y + 4f), Vector2.down, 30f, LayerMask.GetMask("Ground"));
        Vector3 pos = new Vector3(at.x, (hit ? hit.point.y : at.y) + 1.64f + lift, 0);
        rb.position = pos; p.transform.position = pos; rb.linearVelocity = Vector2.zero;
    }

    static void Swing(Transform target)
    {
        var p = P;
        p.GetComponent<SpriteRenderer>().flipX = target.position.x < p.transform.position.x;
        var f = typeof(Player).GetField("attackPointFacingRightPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        Vector3 b = (Vector3)f.GetValue(p);
        p.attackPoint.localPosition = new Vector3(Mathf.Abs(b.x) * (p.GetComponent<SpriteRenderer>().flipX ? -1 : 1), b.y, b.z);
        var m = typeof(Player).GetMethod("DoDamage", BindingFlags.NonPublic | BindingFlags.Instance);
        p.StartCoroutine((IEnumerator)m.Invoke(p, null));
    }

    public static async Task<string> Run()
    {
        var sb = new StringBuilder();
        string scene = SceneManager.GetActiveScene().name;
        await W(3f);
        var p = P;
        var col = p.GetComponent<Collider2D>();
        var g = Physics2D.Raycast(new Vector2(col.bounds.center.x, col.bounds.max.y), Vector2.down, 20f, LayerMask.GetMask("Ground"));
        sb.Append(R(g && Mathf.Abs(col.bounds.min.y - g.point.y) < 0.2f, $"[{scene}] player stands on the ground at spawn"));
        sb.Append(R(GameHUD.Instance != null, "HUD present"));

        // Inventory
        var inv = Object.FindFirstObjectByType<InventoryUI>();
        inv.Open(); await W(0.3f);
        bool opened = InventoryUI.IsOpen && Time.timeScale == 0f;
        inv.Close(); await W(0.2f);
        sb.Append(R(opened && !InventoryUI.IsOpen && Time.timeScale == 1f, "inventory opens (pauses) and closes"));

        // Item pickup
        var pick = Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None).OrderBy(i => i.transform.position.x).FirstOrDefault();
        if (pick != null)
        {
            string id = pick.ItemId; int before = Inventory.Count(id);
            Teleport(pick.transform.position, 0f); await W(1.2f);
            sb.Append(R(Inventory.Count(id) > before, $"picked up {ItemDatabase.Get(id).displayName} ({before} -> {Inventory.Count(id)})"));
        }

        // Regular enemy: engages, takes damage, dies, grants experience
        p.TryHeal(999);
        var enemy = Object.FindObjectsByType<EnemyCore>(FindObjectsSortMode.None).Where(e => !e.IsBoss && e.gameObject.activeInHierarchy)
            .OrderBy(e => e.transform.position.x).FirstOrDefault();
        if (enemy != null)
        {
            string ename = enemy.DisplayName;
            Teleport(enemy.transform.position + Vector3.left * 4.5f, 0f);
            int hp = p.CurrentHealth; int xpBefore = PlayerProgress.Xp + PlayerProgress.Level * 10000;
            await W(4f);
            sb.Append(R(p.CurrentHealth < hp, $"{ename} engages and hits the knight ({hp} -> {p.CurrentHealth})"));
            for (int i = 0; i < 25 && enemy != null && !enemy.IsDead; i++) { Swing(enemy.transform); p.TryHeal(999); await W(0.55f); }
            await W(0.5f);
            sb.Append(R(enemy == null || enemy.IsDead, $"{ename} dies to sword attacks"));
            sb.Append(R(PlayerProgress.Xp + PlayerProgress.Level * 10000 > xpBefore, $"experience gained (LV {PlayerProgress.Level}, {PlayerProgress.Xp} xp)"));
        }

        // Shrine saves the run
        var shrine = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).OrderBy(c => c.transform.position.x).First();
        Teleport(shrine.transform.position, 0.2f); await W(1.2f);
        sb.Append(R(Checkpoint.Active == shrine && GameSave.SavedScene == scene, $"shrine '{shrine.CheckpointName}' kindles and saves"));

        // Boss and exit
        var exit = Object.FindFirstObjectByType<LevelExit>();
        var enc = Object.FindFirstObjectByType<BossEncounter>();
        sb.Append(R(!exit.IsOpen, "exit sealed before the boss"));
        if (enc != null)
        {
            p.TryHeal(999);
            Teleport(enc.transform.position, 0.2f); await W(1.5f);
            var bg = typeof(GameHUD).GetField("bossGroup", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameHUD.Instance) as CanvasGroup;
            sb.Append(R(bg.alpha > 0.5f, "boss bar appears"));
            var req = (GameObject[])typeof(LevelExit).GetField("requiredDefeated", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(exit);
            for (int round = 0; round < 250; round++)
            {
                bool any = false;
                foreach (var e in req) if (e != null && e.activeInHierarchy && !(e.GetComponent<EnemyCore>()?.IsDead ?? false)) { any = true; e.SendMessage("TakeDamage", 25, SendMessageOptions.DontRequireReceiver); }
                p.TryHeal(999);
                if (!any) break;
                await W(0.1f);
            }
            await W(3.5f);
            sb.Append(R(exit.IsOpen, "exit opens after the final encounter"));
        }

        string next = (string)typeof(LevelExit).GetField("nextSceneName", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(exit);
        Teleport(exit.transform.position, 0.2f);
        for (int i = 0; i < 60 && SceneManager.GetActiveScene().name == scene; i++) await W(0.1f);
        await W(2f);
        sb.Append(R(SceneManager.GetActiveScene().name == next, $"exit leads to {next}"));
        return sb.ToString();
    }
}

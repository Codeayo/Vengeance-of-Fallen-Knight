using System.Collections;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

// Play-mode checks for level 1. Each entry returns a short PASS/FAIL report.
public static class PlayTests
{
    static Player P => GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    static Rigidbody2D RB => P.GetComponent<Rigidbody2D>();
    static async Task Wait(float s) { await Task.Delay((int)(s * 1000)); }
    static string R(bool ok, string what) => (ok ? "PASS " : "FAIL ") + what + "\n";

    static void Teleport(float x, float yAbove = 2f)
    {
        var hit = Physics2D.Raycast(new Vector2(x, 60), Vector2.down, 120, LayerMask.GetMask("Ground"));
        var p = P;
        var rb = RB;
        Vector3 pos = new Vector3(x, hit.point.y + 1.7f + yAbove, 0);
        rb.position = pos;
        p.transform.position = pos;
        rb.linearVelocity = Vector2.zero;
    }

    static void PlayerAttack()
    {
        var m = typeof(Player).GetMethod("DoDamage", BindingFlags.NonPublic | BindingFlags.Instance);
        P.StartCoroutine((IEnumerator)m.Invoke(P, null));
    }

    static void Face(float dir)
    {
        P.GetComponent<SpriteRenderer>().flipX = dir < 0;
        // Re-run the attack point mirroring Move() does each frame.
        var ap = P.attackPoint;
        var f = typeof(Player).GetField("attackPointFacingRightPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        Vector3 basePos = (Vector3)f.GetValue(P);
        ap.localPosition = new Vector3(Mathf.Abs(basePos.x) * Mathf.Sign(dir), basePos.y, basePos.z);
    }

    public static async Task<string> Spawn()
    {
        var sb = new StringBuilder();
        await Wait(2.5f);
        var p = P;
        var col = p.GetComponent<Collider2D>();
        float groundY = Physics2D.Raycast(new Vector2(col.bounds.center.x, 10), Vector2.down, 40, LayerMask.GetMask("Ground")).point.y;
        sb.Append(R(Mathf.Abs(col.bounds.min.y - groundY) < 0.15f, $"player stands on ground at spawn (feet {col.bounds.min.y:F2}, ground {groundY:F2})"));
        sb.Append(R(Mathf.Abs(RB.linearVelocity.y) < 0.05f, "player at rest after spawn"));
        sb.Append(R(GameHUD.Instance != null, "HUD present"));
        sb.Append(R(p.CurrentHealth == p.maxHealth, $"health {p.CurrentHealth}/{p.maxHealth}"));
        sb.Append(R(SceneManager.GetActiveScene().name == "Graveyard", "scene is Graveyard"));
        return sb.ToString();
    }

    public static async Task<string> Walls()
    {
        var sb = new StringBuilder();
        var p = P;
        p.enabled = false; // drive the body directly
        Teleport(1f, 0.2f);
        await Wait(0.5f);
        for (int i = 0; i < 60; i++) { RB.linearVelocity = new Vector2(-8f, RB.linearVelocity.y); await Wait(0.02f); }
        sb.Append(R(RB.position.x > -3.5f && RB.position.y > -2f, $"west wall stops the player (x={RB.position.x:F2}, y={RB.position.y:F2})"));
        Teleport(433f, 0.2f);
        await Wait(0.5f);
        for (int i = 0; i < 80; i++) { RB.linearVelocity = new Vector2(8f, RB.linearVelocity.y); await Wait(0.02f); }
        sb.Append(R(RB.position.x < 437f && RB.position.y > 9f, $"east wall stops the player (x={RB.position.x:F2}, y={RB.position.y:F2})"));
        p.enabled = true;
        Teleport(3f, 0.2f);
        await Wait(0.5f);
        return sb.ToString();
    }

    public static async Task<string> PitAndCheckpoint()
    {
        var sb = new StringBuilder();
        var p = P;
        // Kindle the first shrine.
        Teleport(101.5f, 0.3f);
        await Wait(1.2f);
        sb.Append(R(Checkpoint.Active != null && Checkpoint.Active.name.Contains("Fallen"), "first shrine becomes the active checkpoint"));
        int hp = p.CurrentHealth;
        // Drop into the pit just after it.
        Teleport(117f, 3f);
        await Wait(2.5f);
        sb.Append(R(p.CurrentHealth == hp - 20, $"spike pit deals 20 damage ({hp} -> {p.CurrentHealth})"));
        sb.Append(R(Mathf.Abs(p.transform.position.x - 103.6f) < 1.5f, $"player respawned at the shrine (x={p.transform.position.x:F1})"));
        return sb.ToString();
    }

    public static async Task<string> SpikeTrap()
    {
        var sb = new StringBuilder();
        var p = P;
        p.TryHeal(100);
        int hp = p.CurrentHealth;
        p.enabled = false;
        Teleport(216f, 0.1f);
        float t = 0;
        while (t < 5f) { RB.linearVelocity = new Vector2(0, RB.linearVelocity.y); await Wait(0.05f); t += 0.05f; }
        p.enabled = true;
        sb.Append(R(p.CurrentHealth < hp, $"spike trap damages the player while raised ({hp} -> {p.CurrentHealth})"));
        p.TryHeal(100);
        return sb.ToString();
    }

    public static async Task<string> Combat()
    {
        var sb = new StringBuilder();
        var p = P;
        p.TryHeal(100);
        var skel = GameObject.Find("Enemies/Restless Skeleton").GetComponent<PatrolSkeletonAI>();
        float sx = skel.transform.position.x;
        Teleport(sx - 4f, 0.1f);
        Face(1);
        int hp = p.CurrentHealth;
        await Wait(4f);
        sb.Append(R(p.CurrentHealth < hp, $"skeleton engages and damages the player ({hp} -> {p.CurrentHealth})"));
        float before = skel.HealthNormalized;
        for (int i = 0; i < 8 && skel != null && !skel.IsDead; i++)
        {
            Face(Mathf.Sign(skel.transform.position.x - p.transform.position.x));
            PlayerAttack();
            await Wait(0.6f);
        }
        sb.Append(R(skel == null || skel.IsDead, $"player attacks kill the skeleton (hp {before:F2} -> {(skel == null ? 0 : skel.HealthNormalized):F2})"));
        p.TryHeal(100);

        var axe = GameObject.Find("Enemies/Axe Skeleton");
        var axeAi = axe.GetComponent<Enemy_behaviour>();
        Teleport(axe.transform.position.x - 4f, 0.1f);
        Face(1);
        hp = p.CurrentHealth;
        await Wait(4f);
        sb.Append(R(p.CurrentHealth < hp, $"axe skeleton detects and damages the player ({hp} -> {p.CurrentHealth})"));
        for (int i = 0; i < 8 && axe != null; i++)
        {
            Face(Mathf.Sign(axe.transform.position.x - p.transform.position.x));
            PlayerAttack();
            await Wait(0.6f);
        }
        await Wait(2.5f);
        sb.Append(R(axe == null, "axe skeleton dies and is removed"));
        p.TryHeal(100);
        return sb.ToString();
    }

    public static async Task<string> Boss()
    {
        var sb = new StringBuilder();
        var p = P;
        var exit = Object.FindFirstObjectByType<LevelExit>();
        sb.Append(R(!exit.IsOpen, "exit is sealed before the final encounter"));
        Teleport(353f, 0.1f); // shrine before the warden
        await Wait(1f);
        p.TryHeal(100);
        Teleport(358.5f, 0.1f);
        await Wait(1.5f);
        var bossBar = typeof(GameHUD).GetField("bossGroup", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameHUD.Instance) as CanvasGroup;
        sb.Append(R(bossBar != null && bossBar.alpha > 0.5f, "boss bar appears on entering the arena"));
        ScreenCapture.CaptureScreenshot("AgentScripts/shots/play_boss.png");
        await Wait(0.3f);

        // Kill everything in the arena with direct damage (combat itself is covered by Combat()).
        foreach (string n in new[] { "Crypt Guard (West)", "Crypt Guard (East)", "The Gravewarden" })
        {
            var e = GameObject.Find("Enemies/" + n);
            for (int i = 0; i < 30 && e != null && !e.GetComponent<PatrolSkeletonAI>().IsDead; i++)
            {
                e.SendMessage("TakeDamage", 10);
                await Wait(0.05f);
            }
        }
        await Wait(3.5f);
        sb.Append(R(exit.IsOpen, "exit opens once the Gravewarden and guards are dead"));
        return sb.ToString();
    }

    public static async Task<string> DeathAndRevive()
    {
        var sb = new StringBuilder();
        var p = P;
        p.TakeDamage(999, true);
        await Wait(3.5f);
        sb.Append(R(p.IsDead && GameHUD.IsGameOverShowing, "death shows the Game Over screen"));
        ScreenCapture.CaptureScreenshot("AgentScripts/shots/play_gameover.png");
        await Wait(0.3f);
        var rise = typeof(GameHUD).GetField("riseAgainButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameHUD.Instance) as UnityEngine.UI.Button;
        rise.onClick.Invoke();
        await Wait(2f);
        sb.Append(R(!p.IsDead && p.CurrentHealth == p.maxHealth && p.enabled, $"Rise Again revives at full health (hp {p.CurrentHealth})"));
        sb.Append(R(Checkpoint.Active != null && Vector2.Distance(p.transform.position, Checkpoint.Active.RespawnPosition) < 2f, "revived at the last shrine"));
        return sb.ToString();
    }

    public static async Task<string> Pause()
    {
        var sb = new StringBuilder();
        var pm = Object.FindFirstObjectByType<PauseMenu>();
        pm.SetPaused(true);
        await Wait(0.5f);
        sb.Append(R(PauseMenu.IsPaused && Time.timeScale == 0f, "pause freezes time"));
        ScreenCapture.CaptureScreenshot("AgentScripts/shots/play_pause.png");
        await Wait(0.3f);
        pm.SetPaused(false);
        await Wait(0.2f);
        sb.Append(R(!PauseMenu.IsPaused && Time.timeScale == 1f, "resume restores time"));
        return sb.ToString();
    }

    public static async Task<string> HudHealth()
    {
        var sb = new StringBuilder();
        var p = P;
        p.TryHeal(100);
        await Wait(0.8f);
        var fill = typeof(GameHUD).GetField("healthFill", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameHUD.Instance) as UnityEngine.UI.Image;
        var text = typeof(GameHUD).GetField("healthText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GameHUD.Instance) as TMPro.TMP_Text;
        p.TakeDamage(30, true);
        await Wait(1.2f);
        sb.Append(R(Mathf.Abs(fill.fillAmount - p.CurrentHealth / (float)p.maxHealth) < 0.02f && text.text.StartsWith(p.CurrentHealth + " "), $"health bar follows damage ({text.text}, fill {fill.fillAmount:F2})"));
        p.TryHeal(25);
        await Wait(1.2f);
        sb.Append(R(Mathf.Abs(fill.fillAmount - p.CurrentHealth / (float)p.maxHealth) < 0.02f, $"health bar follows healing ({text.text})"));
        p.TryHeal(100);
        return sb.ToString();
    }

    public static async Task<string> Exit()
    {
        var sb = new StringBuilder();
        Teleport(429.5f, 0.1f);
        var p = P;
        p.enabled = false;
        for (int i = 0; i < 60 && SceneManager.GetActiveScene().name == "Graveyard"; i++)
        {
            if (RB != null) RB.linearVelocity = new Vector2(6f, RB.linearVelocity.y);
            await Wait(0.05f);
        }
        await Wait(3f);
        sb.Append(R(SceneManager.GetActiveScene().name == "Woodcamp", $"walking into the open gate loads the next level (scene: {SceneManager.GetActiveScene().name})"));
        sb.Append(R(GameSave.SavedScene == "Woodcamp" && GameSave.IsLevelUnlocked(1), "progress saved and chapter II unlocked"));
        return sb.ToString();
    }

    public static string Screenshot(string path)
    {
        ScreenCapture.CaptureScreenshot(path);
        return path;
    }
}
public static class PlayTests2 {
  public static async System.Threading.Tasks.Task<string> WardenMelee(){
    var p=GameObject.FindGameObjectWithTag("Player").GetComponent<Player>(); var rb=p.GetComponent<Rigidbody2D>();
    var w=GameObject.Find("Enemies/The Gravewarden").GetComponent<PatrolSkeletonAI>();
    var hit=Physics2D.Raycast(new Vector2(w.transform.position.x-5,60),Vector2.down,120,LayerMask.GetMask("Ground"));
    rb.position=new Vector2(w.transform.position.x-5,hit.point.y+1.7f); rb.linearVelocity=Vector2.zero; p.TryHeal(100);
    int hp=p.CurrentHealth; await System.Threading.Tasks.Task.Delay(4000);
    string a=(p.CurrentHealth<hp?"PASS":"FAIL")+$" Gravewarden damages the player ({hp} -> {p.CurrentHealth})\n";
    float before=w.HealthNormalized; var m=typeof(Player).GetMethod("DoDamage",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
    for(int i=0;i<4;i++){ p.GetComponent<SpriteRenderer>().flipX = w.transform.position.x < p.transform.position.x; p.StartCoroutine((System.Collections.IEnumerator)m.Invoke(p,null)); p.TryHeal(100); await System.Threading.Tasks.Task.Delay(600);} 
    a+=(w.HealthNormalized<before?"PASS":"FAIL")+$" player's sword damages the Gravewarden ({before:F2} -> {w.HealthNormalized:F2})\n";
    return a;
  }
}

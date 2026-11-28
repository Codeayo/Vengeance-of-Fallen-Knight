using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Boss encounter: the boss waits dormant until the knight enters the arena. Then a short intro
/// (camera reveal, entrance roar and attack, name card, dialogue), boss music and a boss bar that
/// stays up until the boss falls. Music returns to the chapter theme afterwards.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossEncounter : MonoBehaviour
{
    [SerializeField] private MonoBehaviour boss; // any IBossHealth (skeleton, ninja, Malvric)
    [SerializeField] private string bossName = "The Gravewarden";
    [SerializeField] private string arrivalMessage = "The Gravewarden rises to bar your path";
    [SerializeField] private string defeatTitle = "Gravewarden Vanquished";
    [SerializeField] private string defeatSubtitle = "The cursed seal is broken";
    [Header("Introduction")]
    [SerializeField] private string cardName = "";   // e.g. KAGE
    [SerializeField] private string cardTitle = "";  // e.g. MALVRIC'S BLADE
    [SerializeField] private string dialogue = "";   // spoken line, empty for silent bosses

    private bool started;
    private bool finished;

    public void ConfigureIntro(string name, string title, string line)
    {
        cardName = name; cardTitle = title; dialogue = line;
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        // The boss stays dormant (no patrol, no aggression) until its introduction.
        if (boss != null) boss.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (started || boss == null || !other.CompareTag("Player"))
            return;
        started = true;
        StartCoroutine(Intro());
    }

    private IEnumerator Intro()
    {
        var b = boss as IBossHealth;
        var bossGo = boss.gameObject;
        var mm = MusicManager.Instance;
        mm?.OverrideMusic(null, 0.8f);

        if (Cinematic.Instance != null && b != null && !b.IsDead)
        {
            Cinematic.Begin();
            Vector3 at = bossGo.transform.position + new Vector3(0f, 1.5f, 0f);
            Cinematic.FocusCamera(at);
            yield return Cinematic.Step(1.1f);

            // Entrance: roar, shake, light flare and an attack flourish.
            EnemyVoice.Of(bossGo)?.Roar();
            ScreenShake.Shake(0.45f);
            ScreenShake.Rumble(0.06f);
            var anim = bossGo.GetComponent<Animator>();
            if (anim != null)
            {
                foreach (var p in anim.parameters)
                    if (p.name == "Attack" && p.type == AnimatorControllerParameterType.Trigger) anim.SetTrigger("Attack");
                    else if (p.name == "Attack" && p.type == AnimatorControllerParameterType.Bool) StartCoroutine(PulseBool(anim, "Attack"));
            }
            StartCoroutine(Flare(bossGo.transform.position + Vector3.up * 2f));

            string card = string.IsNullOrEmpty(cardName) ? bossName.ToUpperInvariant() : cardName;
            yield return Cinematic.Title(card, cardTitle, 1.3f, 0.6f);
            ScreenShake.Rumble(0f);
            if (!string.IsNullOrEmpty(dialogue))
                yield return Cinematic.Say(string.IsNullOrEmpty(cardName) ? bossName : Capitalise(cardName), dialogue, 1.2f);

            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) yield return Cinematic.PanCamera(playerGo.transform.position + Vector3.up * 1.5f, 0.9f);
            Cinematic.End();
        }
        else if (!string.IsNullOrEmpty(arrivalMessage))
        {
            GameHUD.Instance?.ShowToast(arrivalMessage);
        }

        boss.enabled = true;
        mm?.PlayBossMusic(bossName);
        GameHUD.Instance?.ShowBoss(bossName);
    }

    private static IEnumerator PulseBool(Animator a, string name)
    {
        a.SetBool(name, true);
        yield return new WaitForSecondsRealtime(0.8f);
        if (a != null) a.SetBool(name, false);
    }

    private static IEnumerator Flare(Vector3 at)
    {
        var go = new GameObject("Boss Flare");
        go.transform.position = at;
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = new Color(1f, 0.35f, 0.3f);
        l.pointLightOuterRadius = 10f;
        float t = 0f;
        while (t < 1.6f)
        {
            t += Time.unscaledDeltaTime;
            l.intensity = 3f * Mathf.Sin(Mathf.Clamp01(t / 1.6f) * Mathf.PI);
            yield return null;
        }
        Destroy(go);
    }

    private static string Capitalise(string s) => s.Length <= 1 ? s : char.ToUpper(s[0]) + s.Substring(1).ToLower();

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
            MusicManager.Instance?.RestoreSceneMusic(4f);
        }
    }
}

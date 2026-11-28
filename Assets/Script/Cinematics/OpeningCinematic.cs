using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// New Game opening in the Graveyard: darkness, storm and the Watcher's narration, the title,
/// a slow pan to Aldric's grave and its epitaph, then Aldric clawing his way out of the earth and
/// taking up his blade. Plays once per New Game; skippable (hold Esc / Enter / Start).
/// </summary>
public class OpeningCinematic : MonoBehaviour
{
    [SerializeField] private float ghostOffsetX = 9f;
    [SerializeField] private ParticleSystem dirtBurst;
    [SerializeField] private Light2D graveLight;

    public static bool Running { get; private set; }

    /// <summary>Editor testing: force the opening the next time the Graveyard starts.</summary>
    public static bool ForceNext;

    private void Start()
    {
        if (!(GameSave.ConsumeIntro() || ForceNext))
            return;
        ForceNext = false;
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        Running = true;
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        var player = playerGo.GetComponent<Player>();
        var rb = playerGo.GetComponent<Rigidbody2D>();
        var sr = playerGo.GetComponent<SpriteRenderer>();
        var anim = playerGo.GetComponent<Animator>();
        Vector3 standPos = playerGo.transform.position;
        Vector3 buried = standPos + Vector3.down * 3.1f;
        string layer = sr.sortingLayerName;
        int order = sr.sortingOrder;

        // Wait for the HUD's Cinematic layer to exist.
        yield return null;
        Cinematic.Begin();
        Cinematic.SetBlack(1f);
        var mm = MusicManager.Instance;
        mm?.OverrideMusic(null, 0.5f);

        // Aldric lies beneath the earth, hidden behind the ground tiles.
        rb.simulated = false;
        sr.sortingLayerName = "Backtiles";
        sr.sortingOrder = 50;
        playerGo.transform.position = buried;
        anim.SetBool("IsDead", true);

        Vector3 ghostPos = standPos + new Vector3(ghostOffsetX, 1.6f, 0f);
        Cinematic.FocusCamera(ghostPos + new Vector3(0f, 1.2f, 0f));

        // 1. Darkness, wind and a distant storm.
        yield return Cinematic.Step(1.4f);
        mm?.Play("thunder", 0.8f, 0.02f, 0f);
        if (!Cinematic.Skipping) yield return Cinematic.Flash();
        yield return Cinematic.Step(1.6f);

        // 2. The Watcher appears.
        var ghost = GhostNarrator.Spawn(ghostPos);
        StartCoroutine(ghost.Appear(2.6f));
        yield return Cinematic.Black(0f, 3f);
        if (ghost != null) ghost.Whisper();
        yield return Cinematic.Say(GhostNarrator.SpeakerName, "Once, this realm had a shield. Sir Aldric, Knight-Captain of the Order, sworn protector of the old king.", 1.2f);
        if (ghost != null) ghost.Whisper();
        yield return Cinematic.Say(GhostNarrator.SpeakerName, "Lord Malvric could never best him with a blade. So he bought knives in the dark.", 1.1f);
        yield return Cinematic.Say(GhostNarrator.SpeakerName, "The eleven of the king's guard died that night. Malvric took the throne, and named Aldric traitor.", 1.1f);
        if (ghost != null) ghost.Whisper();
        yield return Cinematic.Say(GhostNarrator.SpeakerName, "They buried him in an unmarked grave. But some oaths are stronger than death.", 1.4f);

        // 3. Title.
        mm?.OverrideMusic(AudioCatalog.Instance != null ? AudioCatalog.Instance.openingMusic : null, 2.5f);
        yield return Cinematic.Title("VENGEANCE OF THE FALLEN KNIGHT", "", 2.4f, 1.6f);

        // 4. Pan to the grave as the Watcher fades.
        if (ghost != null) StartCoroutine(ghost.Vanish(2.2f));
        yield return Cinematic.PanCamera(standPos + new Vector3(1f, 1.6f, 0f), 3.8f);
        yield return Cinematic.Title("HERE LIES SIR ALDRIC", "\"Fallen in the King's service\"", 2.2f, 1f);

        // 5. The earth heaves and Aldric claws his way out.
        mm?.Play("dirt_burst", 1f, 0.02f, 0f);
        ScreenShake.Rumble(0.07f);
        if (graveLight != null) graveLight.intensity = 0f;
        yield return Cinematic.Step(1.2f);
        if (dirtBurst != null) dirtBurst.Play();
        anim.SetBool("IsDead", false);
        anim.speed = 0.55f;
        player.PlayRise();
        mm?.Play("rise", 0.9f, 0.02f, 0f);
        float t = 0f;
        while (t < 2.6f && !Cinematic.Skipping)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / 2.6f);
            playerGo.transform.position = Vector3.Lerp(buried, standPos, k);
            if (graveLight != null) graveLight.intensity = 1.6f * k;
            yield return null;
        }

        playerGo.transform.position = standPos;
        ScreenShake.Rumble(0f);
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        mm?.Play("thunder", 0.9f, 0.02f, 0f);
        ScreenShake.Shake(0.35f);
        if (!Cinematic.Skipping) yield return Cinematic.Flash();

        // 6. He takes up his blade.
        yield return Cinematic.Step(0.8f);
        mm?.Play("sword_ring", 0.8f, 0.02f, 0f);
        yield return Cinematic.Say("Sir Aldric", "...Malvric.", 1.4f);

        Finish(playerGo, rb, sr, anim, standPos, layer, order);
    }

    private void Finish(GameObject playerGo, Rigidbody2D rb, SpriteRenderer sr, Animator anim, Vector3 standPos, string layer, int order)
    {
        // Applied after a skip too, so the game always continues from the same state.
        playerGo.transform.position = standPos;
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        anim.speed = 1f;
        anim.SetBool("IsDead", false);
        foreach (var g in FindObjectsByType<GhostNarrator>(FindObjectsSortMode.None)) Destroy(g.gameObject);
        Cinematic.SetBlack(0f);
        Cinematic.End();
        MusicManager.Instance?.RestoreSceneMusic(2f);
        Running = false;
    }
}

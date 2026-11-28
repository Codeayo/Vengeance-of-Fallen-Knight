using System.Collections;
using UnityEngine;

/// <summary>
/// After Lord Malvric falls: the Watcher returns, the epilogue is told, the Shard of the Crown is
/// returned, and Aldric lays down his sword before the empty throne and is allowed to rest.
/// Then the Credits.
/// </summary>
public class EpilogueCinematic : MonoBehaviour
{
    [SerializeField] private GameObject finalBoss;
    [SerializeField] private Transform throne;

    private bool playing;

    private void OnEnable() => EnemyCore.Killed += OnKilled;
    private void OnDisable() => EnemyCore.Killed -= OnKilled;

    private void OnKilled(EnemyCore core)
    {
        if (playing || finalBoss == null || core.gameObject != finalBoss) return;
        playing = true;
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        yield return new WaitForSecondsRealtime(2.8f);
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        var player = playerGo.GetComponent<Player>();
        var anim = playerGo.GetComponent<Animator>();
        var mm = MusicManager.Instance;

        Cinematic.Begin();
        mm?.OverrideMusic(AudioCatalog.Instance != null ? AudioCatalog.Instance.epilogueMusic : null, 3f);
        Vector3 focus = throne != null ? Vector3.Lerp(playerGo.transform.position, throne.position, 0.5f) : playerGo.transform.position;
        Cinematic.FocusCamera(focus + Vector3.up * 1.8f);
        yield return Cinematic.Step(1.2f);

        var ghost = GhostNarrator.Spawn((throne != null ? throne.position : playerGo.transform.position) + new Vector3(-2f, 2.2f, 0f));
        yield return ghost.Appear(2.2f);
        if (ghost != null) ghost.Whisper();
        yield return Cinematic.Say(GhostNarrator.SpeakerName, "Malvric lies dead at the foot of the throne he stole.", 1.2f);
        yield return Cinematic.Say(GhostNarrator.SpeakerName, "The eleven are avenged. The truth of the night you fell will spread to every corner of the realm.", 1.2f);
        if (ghost != null) ghost.Whisper();
        Inventory.Add("frozen_crown", 1, "crown_shard", false);
        yield return Cinematic.Say(GhostNarrator.SpeakerName, "The Shard of the Crown returns to the old king's line.", 1.2f);
        yield return Cinematic.Say(GhostNarrator.SpeakerName, "Lay down your sword, Aldric. At last... you may rest.", 1.8f);

        // Aldric lays down his blade and is allowed to rest.
        mm?.Play("sword_ring", 0.6f, 0.02f, 0f);
        if (anim != null) { anim.speed = 0.45f; anim.SetBool("IsDead", true); }
        if (ghost != null) StartCoroutine(ghost.Vanish(3f));
        yield return Cinematic.Step(2.5f);
        yield return Cinematic.Black(1f, 3f);
        yield return Cinematic.Title("THE END", "Sir Aldric, Knight-Captain of the Order, rests at last.", 2.5f, 1.4f);

        if (anim != null) anim.speed = 1f;
        Cinematic.SetBlack(1f);
        Cinematic.End();
        Cinematic.SetBlack(1f);
        SceneFader.LoadScene("Credits");
    }
}

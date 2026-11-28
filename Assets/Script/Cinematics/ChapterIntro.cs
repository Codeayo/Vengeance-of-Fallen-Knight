using System.Collections;
using UnityEngine;

/// <summary>
/// Chapter opening: letterbox, a camera sweep from the road ahead back to the knight, and the
/// chapter card. Optionally the Watcher appears with a few lines (used before the final chapter).
/// Plays when a level starts at its beginning, not on respawn or when continuing at a shrine.
/// </summary>
public class ChapterIntro : MonoBehaviour
{
    [SerializeField] private string chapter = "CHAPTER I";
    [SerializeField] private string title = "The Forsaken Graveyard";
    [SerializeField] private string description = "";
    [SerializeField] private float lookAhead = 16f;
    [SerializeField] private string[] ghostLines = new string[0];

    public void Configure(string chapterLabel, string chapterTitle, string desc, params string[] lines)
    {
        chapter = chapterLabel; title = chapterTitle; description = desc; ghostLines = lines ?? new string[0];
    }

    private IEnumerator Start()
    {
        // Let checkpoints (Continue) and the opening cinematic claim the scene first.
        yield return null;
        yield return null;
        while (OpeningCinematic.Running) yield return null;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || Vector2.Distance(player.transform.position, transform.position) > 8f)
            yield break;

        // The intro replaces the plain area banner at the spawn point.
        foreach (var b in FindObjectsByType<AreaBanner>(FindObjectsSortMode.None))
            if (Mathf.Abs(b.transform.position.x - transform.position.x) < 6f) b.gameObject.SetActive(false);

        Cinematic.Begin();
        Vector3 home = player.transform.position + new Vector3(2f, 1.8f, 0f);
        Cinematic.FocusCamera(home + new Vector3(lookAhead, 1.5f, 0f));
        yield return Cinematic.Step(0.4f);
        StartCoroutine(Cinematic.PanCamera(home, 4.2f));
        yield return Cinematic.Title(title.ToUpperInvariant(), "<size=80%><color=#E8C27A>" + chapter + "</color></size>\n" + description, 2.6f, 1.1f);

        if (ghostLines != null && ghostLines.Length > 0 && !Cinematic.Skipping)
        {
            var ghost = GhostNarrator.Spawn(player.transform.position + new Vector3(5f, 1.6f, 0f));
            yield return ghost.Appear(1.8f);
            foreach (string line in ghostLines)
            {
                if (ghost != null) ghost.Whisper();
                yield return Cinematic.Say(GhostNarrator.SpeakerName, line, 1.2f);
            }
            if (ghost != null) StartCoroutine(ghost.Vanish(1.8f));
            yield return Cinematic.Step(0.8f);
        }

        foreach (var g in FindObjectsByType<GhostNarrator>(FindObjectsSortMode.None))
            if (Cinematic.Skipping) Destroy(g.gameObject);
        Cinematic.End();
    }
}

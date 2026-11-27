using UnityEngine;

/// <summary>Shows an area title card the first time the player walks through this trigger.</summary>
[RequireComponent(typeof(Collider2D))]
public class AreaBanner : MonoBehaviour
{
    [SerializeField] private string title = "The Forsaken Graveyard";
    [SerializeField] private string subtitle = "";

    private bool shown;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (shown || !other.CompareTag("Player"))
            return;
        shown = true;
        GameHUD.Instance?.ShowAreaTitle(title, subtitle);
    }
}

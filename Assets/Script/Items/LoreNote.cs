using UnityEngine;

/// <summary>A readable gravestone, letter or inscription. Reading it opens the parchment panel.</summary>
public class LoreNote : Interactable
{
    [SerializeField] private string title = "Weathered Gravestone";
    [SerializeField, TextArea(4, 12)] private string body = "";
    [SerializeField] private string verb = "Read";
    [SerializeField] private GameObject unreadMarker;

    public override string Prompt => $"<color=#E8C27A>[E]</color>  {verb}";

    public void Configure(string noteTitle, string noteBody, string promptVerb = "Read")
    {
        title = noteTitle;
        body = noteBody;
        verb = promptVerb;
    }

    public override void Interact(Player player)
    {
        ReadPanel.Show(title, body);
        if (unreadMarker != null)
            unreadMarker.SetActive(false);
    }
}

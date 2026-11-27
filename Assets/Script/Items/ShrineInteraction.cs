/// <summary>Lets the knight rest and upgrade at a shrine once it has been kindled.</summary>
public class ShrineInteraction : Interactable
{
    private Checkpoint checkpoint;

    private void Awake()
    {
        checkpoint = GetComponent<Checkpoint>();
        range = 3f;
    }

    public override bool CanInteract => checkpoint != null && Checkpoint.Active == checkpoint;
    public override string Prompt => "<color=#E8C27A>[E]</color>  Rest at the shrine";

    public override void Interact(Player player)
    {
        checkpoint.Rest(player);
        ShrineMenu.Open(player);
    }
}

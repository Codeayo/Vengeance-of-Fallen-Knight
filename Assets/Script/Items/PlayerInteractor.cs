using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Finds the nearest Interactable, shows its prompt on the HUD and triggers it on E / gamepad Y.</summary>
[RequireComponent(typeof(Player))]
public class PlayerInteractor : MonoBehaviour
{
    private Player player;
    private Interactable current;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        if (player.IsDead || Cinematic.IsPlaying || PauseMenu.IsPaused || InventoryUI.IsOpen || ReadPanel.IsOpen || ShrineMenu.IsOpen)
        {
            GameHUD.Instance?.SetPrompt(null);
            return;
        }

        current = null;
        float best = float.MaxValue;
        Vector2 at = GetComponent<Collider2D>() != null ? (Vector2)GetComponent<Collider2D>().bounds.center : (Vector2)transform.position;
        foreach (Interactable i in Interactable.All)
        {
            if (i == null || !i.CanInteract) continue;
            float d = Vector2.Distance(at, i.transform.position);
            if (d <= i.Range && d < best)
            {
                best = d;
                current = i;
            }
        }

        GameHUD.Instance?.SetPrompt(current != null ? current.Prompt : null);

        bool pressed = (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame)) ||
                       (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
        if (pressed && current != null)
            current.Interact(player);
    }
}

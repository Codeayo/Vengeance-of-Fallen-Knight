using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>One inventory grid cell. Hover / keyboard / gamepad selection shows the item's details.</summary>
public class InventorySlot : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    public Image frame;
    public Image icon;
    public TMP_Text count;
    public GameObject equippedMark;
    public Image rarityEdge;

    [HideInInspector] public string itemId;
    public System.Action<InventorySlot> Selected;

    private static readonly Color Normal = new Color(0.1f, 0.08f, 0.14f, 0.95f);
    private static readonly Color Highlight = new Color(0.42f, 0.28f, 0.12f, 1f);

    public void Bind(ItemDefinition def, int amount, bool equipped)
    {
        itemId = def != null ? def.id : null;
        icon.enabled = def != null;
        if (def != null)
        {
            icon.sprite = def.icon;
            icon.color = def.iconTint;
            icon.preserveAspect = true;
        }

        count.text = def != null && def.IsStackable && amount > 1 ? amount.ToString() : "";
        if (equippedMark != null) equippedMark.SetActive(equipped);
        if (rarityEdge != null)
            rarityEdge.color = def != null ? ItemDefinition.RarityColor(def.rarity) * new Color(1, 1, 1, 0.9f) : new Color(0.3f, 0.25f, 0.2f, 0.5f);
        frame.color = Normal;
    }

    public void OnSelect(BaseEventData e)
    {
        frame.color = Highlight;
        UIAudio.Hover();
        Selected?.Invoke(this);
    }

    public void OnDeselect(BaseEventData e)
    {
        frame.color = Normal;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }
}

using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The inventory screen (I / Tab / gamepad View): item grid, details, Use / Equip / Discard,
/// equipped gear and character stats. Pauses the game while open.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform grid;
    [SerializeField] private InventorySlot slotTemplate;
    [SerializeField] private int slotCount = 24;

    [Header("Details")]
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailMeta;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text detailEffect;
    [SerializeField] private Button useButton;
    [SerializeField] private TMP_Text useLabel;
    [SerializeField] private Button discardButton;
    [SerializeField] private TMP_Text messageText;

    [Header("Character")]
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image armorIcon;
    [SerializeField] private Image charmIcon;
    [SerializeField] private TMP_Text equipmentText;

    private readonly List<InventorySlot> slots = new List<InventorySlot>();
    private InventorySlot current;
    private Player player;
    private float openedAt;

    private void Awake()
    {
        IsOpen = false;
        SetVisible(false);
        slotTemplate.gameObject.SetActive(false);
        for (int i = 0; i < slotCount; i++)
        {
            InventorySlot s = Instantiate(slotTemplate, grid);
            s.gameObject.SetActive(true);
            s.name = "Slot " + i;
            s.Selected = OnSlotSelected;
            slots.Add(s);
        }

        useButton.onClick.AddListener(UseOrEquip);
        discardButton.onClick.AddListener(Discard);
        Inventory.Changed += Refresh;
        PlayerProgress.Changed += Refresh;
    }

    private void OnDestroy()
    {
        Inventory.Changed -= Refresh;
        PlayerProgress.Changed -= Refresh;
        if (IsOpen) Time.timeScale = 1f;
        IsOpen = false;
    }

    private void Update()
    {
        bool toggle = (Keyboard.current != null && (Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame)) ||
                      (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame);
        bool back = IsOpen && Time.unscaledTime - openedAt > 0.15f &&
                    ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                     (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame));

        if (IsOpen && (toggle || back))
            Close();
        else if (!IsOpen && toggle && CanOpen())
            Open();

        // Keep a selection for keyboard / gamepad after mouse clicks on empty space.
        if (IsOpen && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            Select(current != null ? current.gameObject : slots[0].gameObject);
    }

    private static bool CanOpen() => !Cinematic.IsPlaying && !PauseMenu.IsPaused && !ReadPanel.IsOpen && !ShrineMenu.IsOpen && !GameHUD.IsGameOverShowing && !SceneFader.IsLoading;

    public void Open()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        player = p != null ? p.GetComponent<Player>() : null;
        if (player == null || player.IsDead)
            return;
        IsOpen = true;
        openedAt = Time.unscaledTime;
        Time.timeScale = 0f;
        messageText.text = "";
        SetVisible(true);
        Refresh();
        Select(slots[0].gameObject);
        UIAudio.Confirm();
    }

    public void Close()
    {
        IsOpen = false;
        Time.timeScale = 1f;
        SetVisible(false);
        UIAudio.Back();
    }

    private void Refresh()
    {
        var items = Inventory.Items
            .OrderBy(x => x.def.category)
            .ThenByDescending(x => x.def.rarity)
            .ThenBy(x => x.def.displayName)
            .ToList();

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < items.Count)
                slots[i].Bind(items[i].def, items[i].count, Inventory.IsEquipped(items[i].def.id));
            else
                slots[i].Bind(null, 0, false);
        }

        RefreshCharacter();
        OnSlotSelected(current);
    }

    private void RefreshCharacter()
    {
        if (statsText != null)
        {
            Player p = player;
            int hp = p != null ? p.CurrentHealth : 0;
            int max = p != null ? p.MaxHealth : 0;
            int coins = p != null && p.GetComponent<Player_Collectables>() != null ? p.GetComponent<Player_Collectables>().CurrentCoins : 0;
            statsText.text =
                $"<color=#E8C27A>LEVEL {PlayerProgress.Level}</color>\n" +
                $"<size=80%>Experience  {PlayerProgress.Xp} / {PlayerProgress.XpToNext}</size>\n\n" +
                $"Health   <color=#FFFFFF>{hp} / {max}</color>\n" +
                $"Attack   <color=#FFFFFF>{(p != null ? p.AttackDamage : 0)}</color>\n" +
                $"Defense  <color=#FFFFFF>{(p != null ? p.Defense : 0)}</color>\n" +
                $"Coins    <color=#FFD27A>{coins}</color>";
        }

        SetEquipIcon(weaponIcon, Inventory.EquippedWeapon);
        SetEquipIcon(armorIcon, Inventory.EquippedArmor);
        SetEquipIcon(charmIcon, Inventory.EquippedCharm);
        if (equipmentText != null)
            equipmentText.text =
                $"{Name(Inventory.EquippedWeapon)}{(Inventory.WeaponTemper > 0 ? $" +{Inventory.WeaponTemper}" : "")}\n" +
                $"{Name(Inventory.EquippedArmor)}{(Inventory.ArmorTemper > 0 ? $" +{Inventory.ArmorTemper}" : "")}\n" +
                $"{(string.IsNullOrEmpty(Inventory.EquippedCharm) ? "<color=#777777>No charm</color>" : Name(Inventory.EquippedCharm))}";
    }

    private static string Name(string id) => ItemDatabase.Get(id)?.displayName ?? "-";

    private static void SetEquipIcon(Image img, string id)
    {
        if (img == null) return;
        ItemDefinition d = ItemDatabase.Get(id);
        img.enabled = d != null;
        if (d != null) { img.sprite = d.icon; img.color = d.iconTint; img.preserveAspect = true; }
    }

    private void OnSlotSelected(InventorySlot slot)
    {
        current = slot;
        ItemDefinition def = slot != null ? ItemDatabase.Get(slot.itemId) : null;
        detailIcon.enabled = def != null;
        if (def == null)
        {
            detailName.text = "Empty";
            detailMeta.text = "";
            detailDescription.text = "";
            detailEffect.text = "";
            useButton.gameObject.SetActive(false);
            discardButton.gameObject.SetActive(false);
            return;
        }

        detailIcon.sprite = def.icon;
        detailIcon.color = def.iconTint;
        detailIcon.preserveAspect = true;
        detailName.text = def.displayName;
        detailName.color = ItemDefinition.RarityColor(def.rarity);
        detailMeta.text = $"{def.rarity}  {CategoryName(def.category)}" + (def.IsStackable ? $"   x{Inventory.Count(def.id)}" : "");
        detailDescription.text = def.description;
        detailEffect.text = EffectLine(def);

        bool equip = def.IsEquipment;
        bool usable = def.IsUsable;
        useButton.gameObject.SetActive(equip || usable);
        useLabel.text = equip ? (Inventory.IsEquipped(def.id) ? (def.category == ItemCategory.Charm ? "Unequip" : "Equipped") : "Equip") : "Use";
        useButton.interactable = !(equip && Inventory.IsEquipped(def.id) && def.category != ItemCategory.Charm);
        bool discardable = def.category == ItemCategory.Consumable || def.category == ItemCategory.Material ||
                           (equip && !Inventory.IsEquipped(def.id) && def.id != Inventory.StartingWeapon && def.id != Inventory.StartingArmor);
        discardButton.gameObject.SetActive(discardable);
        WireNavigation();
    }

    private void WireNavigation()
    {
        // From the grid, Right on the last column reaches the action buttons; Left returns.
        var nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = current != null ? current.GetComponent<Selectable>() : null };
        nav.selectOnDown = discardButton.gameObject.activeSelf ? discardButton : null;
        useButton.navigation = nav;
        var nav2 = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = nav.selectOnLeft, selectOnUp = useButton.gameObject.activeSelf ? useButton : null };
        discardButton.navigation = nav2;
    }

    private static string CategoryName(ItemCategory c) => c == ItemCategory.Key ? "Key" : c == ItemCategory.Quest ? "Relic" : c.ToString();

    private static string EffectLine(ItemDefinition d)
    {
        var parts = new List<string>();
        if (d.heal > 0) parts.Add($"Restores {d.heal} health");
        if (d.wardSeconds > 0) parts.Add($"Halves damage taken for {d.wardSeconds:0}s");
        if (d.attackBonus > 0) parts.Add($"+{d.attackBonus} attack");
        if (d.defenseBonus > 0) parts.Add($"+{d.defenseBonus} defense");
        if (d.maxHealthBonus > 0) parts.Add(d.category == ItemCategory.Charm ? $"+{d.maxHealthBonus} max health while worn" : $"Permanently +{d.maxHealthBonus} max health");
        if (d.xpGrant > 0) parts.Add($"+{d.xpGrant} experience");
        if (d.category == ItemCategory.Material) parts.Add("Offer at a shrine to upgrade your gear");
        if (d.category == ItemCategory.Key) parts.Add("Opens a locked gate");
        return string.Join("   •   ", parts);
    }

    private void UseOrEquip()
    {
        ItemDefinition def = current != null ? ItemDatabase.Get(current.itemId) : null;
        if (def == null) return;
        string msg;
        if (def.IsEquipment)
        {
            Inventory.Equip(def.id);
            msg = Inventory.IsEquipped(def.id) ? $"Equipped {def.displayName}" : $"Removed {def.displayName}";
            UIAudio.Confirm();
        }
        else
        {
            msg = Inventory.Use(def.id, player) ?? "Nothing happens";
        }

        messageText.text = msg;
        KeepSelection();
    }

    private void Discard()
    {
        ItemDefinition def = current != null ? ItemDatabase.Get(current.itemId) : null;
        if (def == null) return;
        Inventory.Remove(def.id);
        messageText.text = $"Discarded {def.displayName}";
        UIAudio.Back();
        KeepSelection();
    }

    private void KeepSelection()
    {
        int index = current != null ? slots.IndexOf(current) : 0;
        Refresh();
        while (index > 0 && slots[index].itemId == null) index--;
        Select(slots[Mathf.Max(0, index)].gameObject);
    }

    private static void Select(GameObject go)
    {
        if (EventSystem.current == null || go == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(go);
    }

    private void SetVisible(bool v)
    {
        group.alpha = v ? 1f : 0f;
        group.blocksRaycasts = v;
        group.interactable = v;
    }
}

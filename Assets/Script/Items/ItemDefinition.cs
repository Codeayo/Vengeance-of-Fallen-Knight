using UnityEngine;

public enum ItemCategory { Consumable, Weapon, Armor, Charm, Material, Key, Quest, Collectible }
public enum ItemRarity { Common, Uncommon, Rare, Legendary }

/// <summary>Static data for one item type. All items live in the ItemDatabase asset.</summary>
[System.Serializable]
public class ItemDefinition
{
    public string id;
    public string displayName;
    [TextArea(2, 5)] public string description;
    public ItemCategory category;
    public ItemRarity rarity;
    public Sprite icon;
    public Color iconTint = Color.white;
    public int maxStack = 99;

    [Header("Effects")]
    public int heal;              // consumables
    public int maxHealthBonus;    // charms (while equipped) or heart vessels (permanent on use)
    public int attackBonus;       // weapons
    public int defenseBonus;      // armor
    public int xpGrant;           // consumed for experience
    public float wardSeconds;     // purple draught: damage halved for this long

    public bool IsEquipment => category == ItemCategory.Weapon || category == ItemCategory.Armor || category == ItemCategory.Charm;
    public bool IsUsable => category == ItemCategory.Consumable || (category == ItemCategory.Collectible && (maxHealthBonus > 0 || xpGrant > 0));
    public bool IsStackable => maxStack > 1;

    public static Color RarityColor(ItemRarity r)
    {
        switch (r)
        {
            case ItemRarity.Uncommon: return new Color(0.55f, 0.9f, 0.55f);
            case ItemRarity.Rare: return new Color(0.45f, 0.72f, 1f);
            case ItemRarity.Legendary: return new Color(1f, 0.72f, 0.3f);
            default: return new Color(0.86f, 0.82f, 0.76f);
        }
    }
}

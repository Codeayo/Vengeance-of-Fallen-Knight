using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Creates Assets/Resources/ItemDatabase.asset with every item in the game.</summary>
public static class BuildItems
{
    const string Items = "Assets/Coins/platformer items - free assets/static_items.png";
    const string Gui = "Assets/Main menu/tinyRPG_manaSoulGUI_v_1_0/";

    static Sprite Static(int i) => AssetDatabase.LoadAllAssetsAtPath(Items).OfType<Sprite>().FirstOrDefault(s => s.name == "static_items_" + i);
    static Sprite Frame(string file) => AssetDatabase.LoadAllAssetsAtPath(Gui + file).OfType<Sprite>().OrderBy(s => s.name.Length).ThenBy(s => s.name).FirstOrDefault();

    static ItemDefinition D(string id, string name, ItemCategory cat, ItemRarity rar, Sprite icon, string desc, Color? tint = null, int stack = 1)
        => new ItemDefinition { id = id, displayName = name, category = cat, rarity = rar, icon = icon, description = desc, iconTint = tint ?? Color.white, maxStack = stack };

    public static string Build()
    {
        Sprite weapon = Frame("20250423weaponFrame-Sheet.png");
        Sprite chest = Frame("20250423chestFrame-Sheet.png");
        Sprite ring = Frame("20250423ringFrame-Sheet.png");
        Sprite neck = Frame("20250423neckFrame-Sheet.png");
        Sprite shield = Frame("20250423shieldFrame-Sheet.png");
        Sprite helmet = Frame("20250423helmetFrame-Sheet.png");

        var list = new System.Collections.Generic.List<ItemDefinition>();

        // Consumables
        var flask = D("crimson_flask", "Crimson Flask", ItemCategory.Consumable, ItemRarity.Common, Static(8),
            "A flask of the knight's order, filled with blood-red restorative. Shrines refill it.", null, 9); flask.heal = 35; list.Add(flask);
        var tonic = D("verdant_tonic", "Verdant Tonic", ItemCategory.Consumable, ItemRarity.Uncommon, Static(9),
            "Brewed from grove herbs by the hermits who still remember the old king.", null, 9); tonic.heal = 70; list.Add(tonic);
        var draught = D("violet_draught", "Violet Draught", ItemCategory.Consumable, ItemRarity.Rare, Static(10),
            "A witch's ward. For a short while every blow lands softer.", null, 5); draught.wardSeconds = 20; draught.heal = 15; list.Add(draught);
        var ember = D("ember_of_vengeance", "Ember of Vengeance", ItemCategory.Consumable, ItemRarity.Rare, Static(14),
            "A spark of the fury that raised you from the grave. Consume it to grow stronger.", null, 9); ember.xpGrant = 120; list.Add(ember);

        // Collectibles / upgrades
        var heart = D("heart_vessel", "Heart Vessel", ItemCategory.Collectible, ItemRarity.Rare, Static(1),
            "A still-beating heart of the realm's fallen. Use it to permanently raise your maximum health.", null, 9); heart.maxHealthBonus = 15; list.Add(heart);
        list.Add(D("prism_relic", "Prism Relic", ItemCategory.Collectible, ItemRarity.Legendary, Static(6),
            "One of the six royal relics scattered when the crown fell. Collect them all.", null, 6));

        // Materials
        list.Add(D("soul_shard", "Soul Shard", ItemCategory.Material, ItemRarity.Uncommon, Static(12),
            "Crystallised remnant of a restless soul. Offer two at a shrine to temper your blade.", null, 99));
        list.Add(D("verdant_shard", "Verdant Shard", ItemCategory.Material, ItemRarity.Uncommon, Static(13),
            "Living stone from the grove's heart. Offer two at a shrine to reinforce your armor.", null, 99));

        // Keys
        list.Add(D("crypt_key", "Crypt Key", ItemCategory.Key, ItemRarity.Uncommon, Static(3),
            "A bronze key taken from the Gravewarden's ring. It fits the sealed crypts of the graveyard and the camp's stockade.", null, 9));
        list.Add(D("frost_key", "Frost Key", ItemCategory.Key, ItemRarity.Uncommon, Static(7),
            "Ice-cold iron. It opens the frozen gates of the taiga.", null, 9));
        list.Add(D("royal_key", "Royal Key", ItemCategory.Key, ItemRarity.Rare, Static(11),
            "The seal of the royal household. The castle's inner doors answer to it.", null, 9));

        // Weapons (attack bonus over the base 10 damage)
        Color steel = new Color(0.85f, 0.88f, 0.95f);
        var w0 = D("knights_longsword", "Knight's Longsword", ItemCategory.Weapon, ItemRarity.Common, weapon,
            "The blade you were buried with. Notched, but faithful.", steel); list.Add(w0);
        var w1 = D("gravewarden_cleaver", "Gravewarden's Cleaver", ItemCategory.Weapon, ItemRarity.Uncommon, weapon,
            "A heavy cleaver pried from the Gravewarden's bones.", new Color(0.9f, 0.75f, 0.6f)); w1.attackBonus = 5; list.Add(w1);
        var w2 = D("assassins_fang", "Assassin's Fang", ItemCategory.Weapon, ItemRarity.Uncommon, weapon,
            "The blade that struck you down. Now it serves your vengeance.", new Color(1f, 0.85f, 0.35f)); w2.attackBonus = 8; list.Add(w2);
        var w3 = D("sporebane", "Sporebane", ItemCategory.Weapon, ItemRarity.Rare, weapon,
            "A hermit's blade, quenched in grove sap. Rot cannot cling to it.", new Color(0.6f, 1f, 0.6f)); w3.attackBonus = 11; list.Add(w3);
        var w4 = D("frostbrand", "Frostbrand", ItemCategory.Weapon, ItemRarity.Rare, weapon,
            "Forged in the taiga by smiths who refused the usurper.", new Color(0.6f, 0.85f, 1f)); w4.attackBonus = 15; list.Add(w4);
        var w5 = D("kingsbane", "Kingsbane", ItemCategory.Weapon, ItemRarity.Legendary, weapon,
            "The old king's own sword, hidden in the castle vault. It hungers for the usurper.", new Color(1f, 0.7f, 0.3f)); w5.attackBonus = 20; list.Add(w5);

        // Armor (flat damage reduction)
        var a0 = D("tattered_mail", "Tattered Mail", ItemCategory.Armor, ItemRarity.Common, chest,
            "Your burial mail, rusted through.", steel); list.Add(a0);
        var a1 = D("boneplate_cuirass", "Boneplate Cuirass", ItemCategory.Armor, ItemRarity.Uncommon, chest,
            "Plates of bone lashed over leather. Ugly, but it holds.", new Color(0.95f, 0.9f, 0.8f)); a1.defenseBonus = 2; list.Add(a1);
        var a2 = D("ranger_leathers", "Ranger's Leathers", ItemCategory.Armor, ItemRarity.Uncommon, chest,
            "Supple leathers from the camp's quartermaster.", new Color(0.85f, 0.65f, 0.45f)); a2.defenseBonus = 3; list.Add(a2);
        var a3 = D("frostguard_mail", "Frostguard Mail", ItemCategory.Armor, ItemRarity.Rare, chest,
            "Mail worn by the taiga's watch, rimed with frost that never melts.", new Color(0.65f, 0.85f, 1f)); a3.defenseBonus = 5; list.Add(a3);
        var a4 = D("royal_guard_plate", "Royal Guard Plate", ItemCategory.Armor, ItemRarity.Legendary, chest,
            "Plate of the king's personal guard. You once wore its twin.", new Color(1f, 0.8f, 0.45f)); a4.defenseBonus = 8; list.Add(a4);

        // Charms
        var c1 = D("ring_of_the_fallen", "Ring of the Fallen", ItemCategory.Charm, ItemRarity.Rare, ring,
            "Every knight of the order wore one. Yours was buried with you.", new Color(1f, 0.85f, 0.5f)); c1.maxHealthBonus = 20; list.Add(c1);
        var c2 = D("frost_amulet", "Amulet of the North", ItemCategory.Charm, ItemRarity.Rare, neck,
            "A taiga talisman that steadies a failing heart.", new Color(0.7f, 0.9f, 1f)); c2.maxHealthBonus = 35; list.Add(c2);

        // Story relics
        list.Add(D("broken_signet", "Broken Signet", ItemCategory.Quest, ItemRarity.Rare, neck,
            "The signet of Sir Aldric, Knight-Captain of the Order. It was cut from your hand the night you died.", new Color(0.95f, 0.8f, 0.5f)));
        list.Add(D("blood_contract", "Blood Contract", ItemCategory.Quest, ItemRarity.Rare, Static(2),
            "A contract for your murder, signed in the usurper's own hand: Lord Malvric.", new Color(0.9f, 0.6f, 0.6f)));
        list.Add(D("hermits_map", "Hermit's Map", ItemCategory.Quest, ItemRarity.Uncommon, shield,
            "A hand-drawn map of the hidden pass through the taiga to the castle's postern gate.", new Color(0.85f, 0.95f, 0.75f)));
        list.Add(D("frozen_crown", "Shard of the Crown", ItemCategory.Quest, ItemRarity.Legendary, helmet,
            "A shard of the true king's crown, broken when Malvric seized the throne.", new Color(1f, 0.85f, 0.4f)));

        string path = "Assets/Resources/ItemDatabase.asset";
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(db, path);
        }

        db.items = list;
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        int missing = list.Count(i => i.icon == null);
        return $"{list.Count} items, {missing} missing icons";
    }
}

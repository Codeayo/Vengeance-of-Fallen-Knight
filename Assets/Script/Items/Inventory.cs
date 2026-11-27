using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The knight's inventory and equipment. Lives for the whole run (static), is saved with the run
/// by GameSave and restored on Continue.
/// </summary>
public static class Inventory
{
    [Serializable]
    private class Entry { public string id; public int count; }

    [Serializable]
    private class Data
    {
        public List<Entry> items = new List<Entry>();
        public string weapon;
        public string armor;
        public string charm;
        public int weaponTemper;
        public int armorTemper;
        public List<string> collectedUnique = new List<string>();
    }

    private static Data data = NewData();

    /// <summary>Raised after any change to items or equipment.</summary>
    public static event Action Changed;
    /// <summary>Raised when items are picked up: (definition, amount).</summary>
    public static event Action<ItemDefinition, int> Collected;

    public const string StartingWeapon = "knights_longsword";
    public const string StartingArmor = "tattered_mail";
    public const int MaxTemper = 5;

    private static Data NewData()
    {
        var d = new Data();
        d.items.Add(new Entry { id = StartingWeapon, count = 1 });
        d.items.Add(new Entry { id = StartingArmor, count = 1 });
        d.items.Add(new Entry { id = "crimson_flask", count = 2 });
        d.weapon = StartingWeapon;
        d.armor = StartingArmor;
        return d;
    }

    // ---------------- QUERIES ----------------
    public static IEnumerable<(ItemDefinition def, int count)> Items =>
        data.items.Where(e => e.count > 0)
            .Select(e => (ItemDatabase.Get(e.id), e.count))
            .Where(x => x.Item1 != null);

    public static int Count(string id) => data.items.FirstOrDefault(e => e.id == id)?.count ?? 0;
    public static bool Has(string id) => Count(id) > 0;
    public static string EquippedWeapon => data.weapon;
    public static string EquippedArmor => data.armor;
    public static string EquippedCharm => data.charm;
    public static int WeaponTemper => data.weaponTemper;
    public static int ArmorTemper => data.armorTemper;
    public static bool IsEquipped(string id) => id == data.weapon || id == data.armor || id == data.charm;
    public static bool WasCollected(string uniqueId) => !string.IsNullOrEmpty(uniqueId) && data.collectedUnique.Contains(uniqueId);

    public static int AttackBonus => (ItemDatabase.Get(data.weapon)?.attackBonus ?? 0) + data.weaponTemper * 2;
    public static int DefenseBonus => (ItemDatabase.Get(data.armor)?.defenseBonus ?? 0) + data.armorTemper;
    public static int MaxHealthBonus => ItemDatabase.Get(data.charm)?.maxHealthBonus ?? 0;

    // ---------------- CHANGES ----------------
    public static bool Add(string id, int amount = 1, string uniqueId = null, bool notify = true)
    {
        ItemDefinition def = ItemDatabase.Get(id);
        if (def == null || amount <= 0)
        {
            Debug.LogWarning($"Unknown item '{id}'.");
            return false;
        }

        if (WasCollected(uniqueId))
            return false;

        Entry e = data.items.FirstOrDefault(x => x.id == id);
        if (e == null)
            data.items.Add(e = new Entry { id = id, count = 0 });
        e.count = Mathf.Min(def.maxStack, e.count + amount);

        if (!string.IsNullOrEmpty(uniqueId))
            data.collectedUnique.Add(uniqueId);

        // First weapon / armor / charm of a better grade is equipped automatically.
        if (def.category == ItemCategory.Weapon && (ItemDatabase.Get(data.weapon)?.attackBonus ?? -1) < def.attackBonus)
            data.weapon = id;
        if (def.category == ItemCategory.Armor && (ItemDatabase.Get(data.armor)?.defenseBonus ?? -1) < def.defenseBonus)
            data.armor = id;
        if (def.category == ItemCategory.Charm && string.IsNullOrEmpty(data.charm))
            data.charm = id;

        if (notify)
            Collected?.Invoke(def, amount);
        Changed?.Invoke();
        return true;
    }

    /// <summary>Records a one-time world event (opened gate, read relic) so it persists in the save.</summary>
    public static void MarkCollected(string uniqueId)
    {
        if (!string.IsNullOrEmpty(uniqueId) && !data.collectedUnique.Contains(uniqueId))
            data.collectedUnique.Add(uniqueId);
    }

    public static bool Remove(string id, int amount = 1)
    {
        Entry e = data.items.FirstOrDefault(x => x.id == id);
        if (e == null || e.count < amount)
            return false;
        e.count -= amount;
        if (e.count <= 0)
        {
            data.items.Remove(e);
            if (data.weapon == id) data.weapon = StartingWeapon;
            if (data.armor == id) data.armor = StartingArmor;
            if (data.charm == id) data.charm = null;
        }

        Changed?.Invoke();
        return true;
    }

    public static bool Equip(string id)
    {
        ItemDefinition def = ItemDatabase.Get(id);
        if (def == null || !def.IsEquipment || !Has(id))
            return false;

        switch (def.category)
        {
            case ItemCategory.Weapon: data.weapon = id; break;
            case ItemCategory.Armor: data.armor = id; break;
            case ItemCategory.Charm: data.charm = data.charm == id ? null : id; break;
        }

        Changed?.Invoke();
        return true;
    }

    /// <summary>Uses a consumable on the player. Returns a message for the UI, or null if nothing happened.</summary>
    public static string Use(string id, Player player)
    {
        ItemDefinition def = ItemDatabase.Get(id);
        if (def == null || !def.IsUsable || !Has(id) || player == null || player.IsDead)
            return null;

        bool used = false;
        string message = null;

        if (def.heal > 0 && player.TryHeal(def.heal))
        {
            used = true;
            message = $"Restored {def.heal} health";
        }

        if (def.wardSeconds > 0f)
        {
            player.GrantWard(def.wardSeconds);
            used = true;
            message = $"Warded for {def.wardSeconds:0} seconds";
        }

        if (def.maxHealthBonus > 0 && def.category == ItemCategory.Collectible)
        {
            PlayerProgress.AddPermanentHealth(def.maxHealthBonus);
            used = true;
            message = $"Maximum health +{def.maxHealthBonus}";
        }

        if (def.xpGrant > 0)
        {
            PlayerProgress.AddXp(def.xpGrant);
            used = true;
            message = $"+{def.xpGrant} experience";
        }

        if (!used)
            return def.heal > 0 ? "Already at full health" : null;

        Remove(id);
        MusicManager.Instance?.PlayHealSfx();
        return message;
    }

    public static bool TemperWeapon(string materialId, int cost)
    {
        if (data.weaponTemper >= MaxTemper || Count(materialId) < cost)
            return false;
        Remove(materialId, cost);
        data.weaponTemper++;
        Changed?.Invoke();
        return true;
    }

    public static bool TemperArmor(string materialId, int cost)
    {
        if (data.armorTemper >= MaxTemper || Count(materialId) < cost)
            return false;
        Remove(materialId, cost);
        data.armorTemper++;
        Changed?.Invoke();
        return true;
    }

    // ---------------- SAVE ----------------
    public static string ToJson() => JsonUtility.ToJson(data);

    public static void LoadJson(string json)
    {
        Data loaded = null;
        if (!string.IsNullOrEmpty(json))
        {
            try { loaded = JsonUtility.FromJson<Data>(json); }
            catch (Exception ex) { Debug.LogWarning("Inventory save was unreadable and has been reset: " + ex.Message); }
        }

        data = loaded ?? NewData();
        data.items.RemoveAll(e => e == null || ItemDatabase.Get(e.id) == null || e.count <= 0);
        if (!Has(StartingWeapon)) data.items.Add(new Entry { id = StartingWeapon, count = 1 });
        if (!Has(StartingArmor)) data.items.Add(new Entry { id = StartingArmor, count = 1 });
        if (!Has(data.weapon)) data.weapon = StartingWeapon;
        if (!Has(data.armor)) data.armor = StartingArmor;
        if (!string.IsNullOrEmpty(data.charm) && !Has(data.charm)) data.charm = null;
        data.weaponTemper = Mathf.Clamp(data.weaponTemper, 0, MaxTemper);
        data.armorTemper = Mathf.Clamp(data.armorTemper, 0, MaxTemper);
        Changed?.Invoke();
    }

    public static void ResetToNew()
    {
        data = NewData();
        Changed?.Invoke();
    }
}

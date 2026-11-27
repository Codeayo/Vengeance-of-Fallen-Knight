using System.Collections.Generic;
using UnityEngine;

/// <summary>All item definitions. Loaded from Resources/ItemDatabase at runtime.</summary>
[CreateAssetMenu(menuName = "Vengeance/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemDefinition> items = new List<ItemDefinition>();

    private static ItemDatabase instance;
    private Dictionary<string, ItemDefinition> lookup;

    public static ItemDatabase Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<ItemDatabase>("ItemDatabase");
            return instance;
        }
    }

    public static ItemDefinition Get(string id)
    {
        ItemDatabase db = Instance;
        if (db == null || string.IsNullOrEmpty(id))
            return null;

        if (db.lookup == null)
        {
            db.lookup = new Dictionary<string, ItemDefinition>();
            foreach (ItemDefinition d in db.items)
                if (d != null && !string.IsNullOrEmpty(d.id))
                    db.lookup[d.id] = d;
        }

        db.lookup.TryGetValue(id, out ItemDefinition def);
        return def;
    }
}

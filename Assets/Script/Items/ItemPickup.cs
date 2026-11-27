using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A world item the knight collects by touching it. Unique pickups (keys, relics, heart vessels,
/// equipment) remember they were taken so they never respawn in the same run.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private string itemId = "crimson_flask";
    [SerializeField] private int amount = 1;
    [SerializeField] private string uniqueId;
    [SerializeField] private float bobHeight = 0.15f;

    private SpriteRenderer sr;
    private Vector3 basePos;
    private float phase;
    private bool taken;

    public string ItemId => itemId;

    public static ItemPickup Spawn(string id, int count, Vector3 position, string unique)
    {
        var go = new GameObject("Pickup - " + id);
        go.transform.position = position;
        go.AddComponent<CircleCollider2D>();
        var p = go.AddComponent<ItemPickup>();
        p.itemId = id;
        p.amount = count;
        p.uniqueId = unique;
        p.Setup();
        return p;
    }

    public void Configure(string id, int count, string unique)
    {
        itemId = id;
        amount = count;
        uniqueId = unique;
    }

    private void Awake()
    {
        Setup();
    }

    private void Setup()
    {
        if (sr != null)
            return;
        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.55f;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        ItemDefinition def = ItemDatabase.Get(itemId);
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();
        if (def != null)
        {
            sr.sprite = def.icon;
            sr.color = def.iconTint;
            float size = def.IsEquipment || def.category == ItemCategory.Quest ? 1.1f : 0.8f;
            float px = def.icon != null ? Mathf.Max(def.icon.bounds.size.x, def.icon.bounds.size.y) : 1f;
            transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, px));
            if (def.rarity >= ItemRarity.Rare && GetComponentInChildren<Light2D>() == null)
            {
                var lg = new GameObject("Glow").AddComponent<Light2D>();
                lg.transform.SetParent(transform, false);
                lg.lightType = Light2D.LightType.Point;
                lg.color = ItemDefinition.RarityColor(def.rarity);
                lg.intensity = 0.9f;
                lg.pointLightOuterRadius = 2.5f;
            }
        }

        sr.sortingLayerName = "player";
        sr.sortingOrder = 4;
        basePos = transform.position;
        phase = Random.value * 6f;
    }

    private void Start()
    {
        basePos = transform.position;
        if (Inventory.WasCollected(uniqueId))
            Destroy(gameObject);
    }

    private void Update()
    {
        phase += Time.deltaTime * 2.4f;
        transform.position = basePos + Vector3.up * (Mathf.Sin(phase) * bobHeight);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (taken || !other.CompareTag("Player"))
            return;

        if (Inventory.Add(itemId, amount, string.IsNullOrEmpty(uniqueId) ? null : uniqueId))
        {
            taken = true;
            MusicManager.Instance?.PlayPickupSfx();
            Destroy(gameObject);
        }
    }
}

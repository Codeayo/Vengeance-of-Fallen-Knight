using UnityEngine;
using UnityEngine.UI;

public class Player_Collectables : MonoBehaviour
{
    private const string HealingPotionTag = "HealingPotion";

    private int currentCoins;
    private int healingFlasks;

    public Text currentCoin_Text;
    [SerializeField] private Sprite healingFlaskSprite;
    [SerializeField, Min(1)] private int startingHealingFlasks = 1;
    [SerializeField, Min(1)] private int healingAmount = 35;

    /// <summary>(coins, healing flasks) after any change.</summary>
    public event System.Action<int, int> CountsChanged;

    public int CurrentCoins => currentCoins;
    public int HealingFlasks => healingFlasks;

    private Text inventoryHudText;

    private void Start()
    {
        currentCoins = GameSave.LoadCoins();
        healingFlasks = GameSave.LoadHealingFlasks(startingHealingFlasks);
        GameSave.SaveHealingFlasks(healingFlasks);

        UpdateCoinText();
        SpawnLevelHealingFlasks();
        if (GameHUD.Instance == null)
            CreateInventoryHud();
        UpdateInventoryText();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(HealingPotionTag))
        {
            healingFlasks++;
            GameSave.SaveHealingFlasks(healingFlasks);
            UpdateInventoryText();
            MusicManager.Instance?.PlayPickupSfx();
            Destroy(other.gameObject);
            return;
        }

        if (!other.CompareTag("Coin"))
            return;

        currentCoins++;
        UpdateCoinText();
        other.enabled = false;

        Animator anim = other.GetComponentInChildren<Animator>();
        if (anim != null)
            anim.SetTrigger("Collect");

        MusicManager.Instance?.PlayPickupSfx();
        Destroy(other.gameObject, 1f);
    }

    public bool TryUseHealingFlask(Player player)
    {
        if (player == null || healingFlasks <= 0 || !player.TryHeal(healingAmount))
            return false;

        healingFlasks--;
        GameSave.SaveHealingFlasks(healingFlasks);
        UpdateInventoryText();
        MusicManager.Instance?.PlayHealSfx();
        return true;
    }

    private void UpdateCoinText()
    {
        if (currentCoin_Text != null)
            currentCoin_Text.text = currentCoins.ToString();
        CountsChanged?.Invoke(currentCoins, healingFlasks);
    }

    private void UpdateInventoryText()
    {
        UpdateCoinText();

        if (inventoryHudText != null)
            inventoryHudText.text = $"<color=#F2C562>INVENTORY</color>\nHealing Flask x{healingFlasks}  [H] Use";
    }

    private void CreateInventoryHud()
    {
        GameObject canvasObject = new GameObject("Inventory HUD", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObject = new GameObject("Inventory Label", typeof(RectTransform), typeof(Text), typeof(Outline));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(canvas.transform, false);
        textRect.anchorMin = Vector2.one;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = Vector2.one;
        textRect.anchoredPosition = new Vector2(-76f, -24f);
        textRect.sizeDelta = new Vector2(240f, 62f);

        inventoryHudText = textObject.GetComponent<Text>();
        inventoryHudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (inventoryHudText.font == null)
        {
            Debug.LogError("The built-in Arial font could not be loaded for the inventory HUD.", this);
            Destroy(canvasObject);
            inventoryHudText = null;
            return;
        }

        inventoryHudText.fontSize = 18;
        inventoryHudText.fontStyle = FontStyle.Bold;
        inventoryHudText.alignment = TextAnchor.UpperLeft;
        inventoryHudText.supportRichText = true;
        inventoryHudText.color = Color.white;
        inventoryHudText.raycastTarget = false;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        if (healingFlaskSprite != null)
        {
            GameObject iconObject = new GameObject("Healing Flask Icon", typeof(RectTransform), typeof(Image));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(canvas.transform, false);
            iconRect.anchorMin = Vector2.one;
            iconRect.anchorMax = Vector2.one;
            iconRect.pivot = Vector2.one;
            iconRect.anchoredPosition = new Vector2(-24f, -25f);
            iconRect.sizeDelta = new Vector2(42f, 42f);

            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = healingFlaskSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }
    }

    private void SpawnLevelHealingFlasks()
    {
        if (healingFlaskSprite == null)
        {
            Debug.LogWarning("Healing flask sprite is not assigned on Player_Collectables.", this);
            return;
        }

        Vector2[] positions;
        switch (gameObject.scene.name)
        {
            case "Graveyard":
                positions = new[] { new Vector2(103f, 0.5f), new Vector2(341f, 12.4f) };
                break;
            case "Woodcamp":
                positions = new[] { new Vector2(26f, 3.4f), new Vector2(65f, 3.4f) };
                break;
            case "Grove":
                positions = new[] { new Vector2(52f, 3.2f), new Vector2(121f, 5.2f) };
                break;
            case "Taiga":
                positions = new[] { new Vector2(66f, 1.6f), new Vector2(139f, 2.6f) };
                break;
            case "Castle":
                positions = new[] { new Vector2(65f, 2f), new Vector2(148f, 3f) };
                break;
            case "ThroneRoom":
                positions = new[] { new Vector2(66f, 3.8f), new Vector2(145f, 3.8f) };
                break;
            default:
                return;
        }

        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        foreach (Vector2 position in positions)
        {
            GameObject pickup = new GameObject("Healing Flask");
            pickup.tag = HealingPotionTag;
            pickup.transform.position = new Vector3(position.x, position.y, transform.position.z);

            SpriteRenderer renderer = pickup.AddComponent<SpriteRenderer>();
            renderer.sprite = healingFlaskSprite;
            renderer.sortingOrder = playerRenderer != null ? playerRenderer.sortingOrder + 1 : 1;
            if (playerRenderer != null)
                renderer.sortingLayerID = playerRenderer.sortingLayerID;

            CircleCollider2D pickupCollider = pickup.AddComponent<CircleCollider2D>();
            pickupCollider.isTrigger = true;
            pickupCollider.radius = 0.45f;
        }
    }

}

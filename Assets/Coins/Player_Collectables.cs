using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coins (the run's currency) and healing flasks. Flasks are now Crimson Flask items in the
/// Inventory, so healing, the HUD counter and the inventory screen all share one count.
/// </summary>
public class Player_Collectables : MonoBehaviour
{
    public const string FlaskId = "crimson_flask";
    private const string HealingPotionTag = "HealingPotion";

    private int currentCoins;

    public Text currentCoin_Text;
    [SerializeField] private Sprite healingFlaskSprite;
    [SerializeField, Min(1)] private int startingHealingFlasks = 1;
    [SerializeField, Min(1)] private int healingAmount = 35;

    /// <summary>(coins, healing flasks) after any change.</summary>
    public event System.Action<int, int> CountsChanged;

    public int CurrentCoins => currentCoins;
    public int HealingFlasks => Inventory.Count(FlaskId);

    private void Awake()
    {
        Inventory.Changed += Notify;
    }

    private void OnDestroy()
    {
        Inventory.Changed -= Notify;
    }

    private void Start()
    {
        currentCoins = GameSave.LoadCoins();
        Notify();
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || currentCoins < amount)
            return false;
        currentCoins -= amount;
        Notify();
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        currentCoins += amount;
        Notify();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(HealingPotionTag))
        {
            // Legacy flask pickups placed in older scenes.
            Inventory.Add(FlaskId);
            MusicManager.Instance?.PlayPickupSfx();
            Destroy(other.gameObject);
            return;
        }

        if (!other.CompareTag("Coin"))
            return;

        currentCoins++;
        Notify();
        other.enabled = false;

        Animator anim = other.GetComponentInChildren<Animator>();
        if (anim != null)
            anim.SetTrigger("Collect");

        MusicManager.Instance?.PlayPickupSfx();
        Destroy(other.gameObject, 1f);
    }

    public bool TryUseHealingFlask(Player player)
    {
        if (player == null || !Inventory.Has(FlaskId))
        {
            GameHUD.Instance?.ShowToast("No Crimson Flasks left");
            return false;
        }

        string result = Inventory.Use(FlaskId, player);
        if (result == null || result.StartsWith("Already"))
            return false;
        return true;
    }

    private void Notify()
    {
        if (currentCoin_Text != null)
            currentCoin_Text.text = currentCoins.ToString();
        CountsChanged?.Invoke(currentCoins, HealingFlasks);
    }
}

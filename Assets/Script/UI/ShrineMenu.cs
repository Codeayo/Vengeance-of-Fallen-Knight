using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Shrine rest menu: the knight is healed, flasks refilled and the run saved; Soul Shards temper
/// the blade (+2 attack) and Verdant Shards reinforce armor (+1 defense).
/// </summary>
public class ShrineMenu : MonoBehaviour
{
    public const string BladeMaterial = "soul_shard";
    public const string ArmorMaterial = "verdant_shard";
    public const int Cost = 2;

    private static ShrineMenu instance;
    public static bool IsOpen { get; private set; }

    [SerializeField] private CanvasGroup group;
    [SerializeField] private Button temperButton;
    [SerializeField] private Button reinforceButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TMP_Text temperDetail;
    [SerializeField] private TMP_Text reinforceDetail;
    [SerializeField] private TMP_Text statusText;

    private Player player;
    private float openedAt;

    private void Awake()
    {
        instance = this;
        IsOpen = false;
        SetVisible(false);
        if (temperButton != null) temperButton.onClick.AddListener(Temper);
        if (reinforceButton != null) reinforceButton.onClick.AddListener(Reinforce);
        if (leaveButton != null) leaveButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            if (IsOpen) Time.timeScale = 1f;
            IsOpen = false;
        }
    }

    public static void Open(Player p)
    {
        if (instance == null)
            return;
        instance.player = p;
        instance.openedAt = Time.unscaledTime;
        IsOpen = true;
        Time.timeScale = 0f;
        instance.Refresh("You rest. Wounds close and your flasks are refilled. Progress saved.");
        instance.SetVisible(true);
        EventSystem.current?.SetSelectedGameObject(null);
        EventSystem.current?.SetSelectedGameObject(instance.temperButton.gameObject);
        UIAudio.Confirm();
    }

    private void Update()
    {
        if (!IsOpen || Time.unscaledTime - openedAt < 0.2f)
            return;
        if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))
            Close();
    }

    private void Temper()
    {
        if (Inventory.TemperWeapon(BladeMaterial, Cost))
        {
            UIAudio.Confirm();
            Refresh($"Your blade is tempered (+{Inventory.WeaponTemper * 2} attack).");
        }
        else
        {
            UIAudio.Back();
            Refresh(Inventory.WeaponTemper >= Inventory.MaxTemper ? "Your blade can be tempered no further." : $"You need {Cost} Soul Shards.");
        }
    }

    private void Reinforce()
    {
        if (Inventory.TemperArmor(ArmorMaterial, Cost))
        {
            UIAudio.Confirm();
            Refresh($"Your armor is reinforced (+{Inventory.ArmorTemper} defense).");
        }
        else
        {
            UIAudio.Back();
            Refresh(Inventory.ArmorTemper >= Inventory.MaxTemper ? "Your armor can be reinforced no further." : $"You need {Cost} Verdant Shards.");
        }
    }

    private void Close()
    {
        IsOpen = false;
        Time.timeScale = 1f;
        SetVisible(false);
        UIAudio.Back();
    }

    private void Refresh(string status)
    {
        if (temperDetail != null)
            temperDetail.text = $"Blade +{Inventory.WeaponTemper}/{Inventory.MaxTemper}   Cost: {Cost} Soul Shards (have {Inventory.Count(BladeMaterial)})";
        if (reinforceDetail != null)
            reinforceDetail.text = $"Armor +{Inventory.ArmorTemper}/{Inventory.MaxTemper}   Cost: {Cost} Verdant Shards (have {Inventory.Count(ArmorMaterial)})";
        if (statusText != null)
            statusText.text = status;
    }

    private void SetVisible(bool v)
    {
        if (group == null) return;
        group.alpha = v ? 1f : 0f;
        group.blocksRaycasts = v;
        group.interactable = v;
    }
}

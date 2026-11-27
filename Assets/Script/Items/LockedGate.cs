using System.Collections;
using UnityEngine;

/// <summary>An iron gate that needs a key. Unlocking consumes the key and lowers the bars.</summary>
public class LockedGate : Interactable
{
    [SerializeField] private string keyItemId = "crypt_key";
    [SerializeField] private Transform bars;
    [SerializeField] private Collider2D blocker;
    [SerializeField] private string gateId;

    private bool open;

    public override bool CanInteract => !open;

    public override string Prompt
    {
        get
        {
            ItemDefinition key = ItemDatabase.Get(keyItemId);
            string keyName = key != null ? key.displayName : "a key";
            return Inventory.Has(keyItemId)
                ? $"<color=#E8C27A>[E]</color>  Unlock with {keyName}"
                : $"Locked  -  requires the {keyName}";
        }
    }

    public void Configure(string key, Transform barsTransform, Collider2D block, string id)
    {
        keyItemId = key;
        bars = barsTransform;
        blocker = block;
        gateId = id;
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(gateId) && Inventory.WasCollected("gate:" + gateId))
            SetOpen(true);
    }

    public override void Interact(Player player)
    {
        if (open)
            return;
        if (!Inventory.Has(keyItemId))
        {
            GameHUD.Instance?.ShowToast("It will not budge. A key is needed.");
            return;
        }

        Inventory.Remove(keyItemId);
        if (!string.IsNullOrEmpty(gateId))
            Inventory.MarkCollected("gate:" + gateId);
        GameHUD.Instance?.ShowToast("The gate grinds open");
        StartCoroutine(Lower());
    }

    private IEnumerator Lower()
    {
        open = true;
        if (blocker != null) blocker.enabled = false;
        if (bars == null) yield break;
        Vector3 start = bars.localPosition;
        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            bars.localPosition = start + Vector3.down * (3.2f * Mathf.SmoothStep(0f, 1f, t / 1.2f));
            yield return null;
        }
    }

    private void SetOpen(bool value)
    {
        open = value;
        if (blocker != null) blocker.enabled = !value;
        if (bars != null) bars.gameObject.SetActive(!value);
    }
}

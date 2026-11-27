using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Shrine checkpoint. Touching it sets the player's respawn point, refills health to at least
/// half and lights up its flame. Hazards and Game Over "Rise Again" return the player here.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private string checkpointName = "Shrine";
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Light2D flameLight;
    [SerializeField] private ParticleSystem flameParticles;
    [SerializeField] private SpriteRenderer[] glowSprites;
    [SerializeField] private float litIntensity = 1.6f;
    [SerializeField] private float unlitIntensity = 0.25f;

    private bool lit;
    private float flicker;

    public static Checkpoint Active { get; private set; }
    public Vector3 RespawnPosition => respawnPoint != null ? respawnPoint.position : transform.position + Vector3.up;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        SetLit(false, true);
    }

    private void Start()
    {
        // Continue: resume at the shrine the run was saved at.
        string pending = GameSave.ConsumePendingCheckpoint(gameObject.scene.name);
        if (pending == checkpointName)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            Player player = p != null ? p.GetComponent<Player>() : null;
            if (player != null)
            {
                player.SetRespawnPoint(RespawnPosition);
                player.TeleportTo(RespawnPosition);
                Active = this;
                SetLit(true, true);
            }
        }
    }

    public string CheckpointName => checkpointName;

    private void OnDestroy()
    {
        if (Active == this)
            Active = null;
    }

    private void Update()
    {
        if (flameLight == null)
            return;
        flicker += Time.deltaTime * 9f;
        float target = lit ? litIntensity : unlitIntensity;
        flameLight.intensity = target * (0.9f + 0.1f * Mathf.PerlinNoise(flicker, 0.37f));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null || player.IsDead)
            return;

        player.SetRespawnPoint(RespawnPosition);
        player.RestoreHealthTo(Mathf.CeilToInt(player.MaxHealth * 0.5f));

        if (Active == this)
            return;

        if (Active != null)
            Active.SetLit(false, false);
        Active = this;
        SetLit(true, false);

        GameHUD.Instance?.ShowToast($"{checkpointName} kindled  -  progress saved");
        MusicManager.Instance?.PlayHealSfx();
        SaveHere(player);
    }

    private void SaveHere(Player player)
    {
        var c = player.GetComponent<Player_Collectables>();
        GameSave.SaveCheckpoint(gameObject.scene.name, checkpointName, c != null ? c.CurrentCoins : 0, player.CurrentHealth);
    }

    /// <summary>Resting at an already-lit shrine (interaction) refills flasks to 2 and saves.</summary>
    public void Rest(Player player)
    {
        if (player == null || player.IsDead) return;
        player.RestoreHealthTo(player.MaxHealth);
        if (Inventory.Count("crimson_flask") < 2)
            Inventory.Add("crimson_flask", 2 - Inventory.Count("crimson_flask"), null, false);
        SaveHere(player);
    }

    private void SetLit(bool on, bool instant)
    {
        lit = on;
        if (flameParticles != null)
        {
            if (on && !flameParticles.isPlaying) flameParticles.Play();
            if (!on) flameParticles.Stop(true, instant ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }

        if (glowSprites != null)
            foreach (SpriteRenderer sr in glowSprites)
                if (sr != null) sr.enabled = on;
    }
}

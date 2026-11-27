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
        player.RestoreHealthTo(Mathf.CeilToInt(player.maxHealth * 0.5f));

        if (Active == this)
            return;

        if (Active != null)
            Active.SetLit(false, false);
        Active = this;
        SetLit(true, false);

        GameHUD.Instance?.ShowToast($"{checkpointName} kindled");
        MusicManager.Instance?.PlayHealSfx();
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

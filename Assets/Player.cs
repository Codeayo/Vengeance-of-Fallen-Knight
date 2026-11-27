using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Animator))]
public class Player : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private KeyCode menuKey = KeyCode.P;
    [SerializeField] private bool autoDisableMenu = true;
    [SerializeField] private float autoDisableSeconds = 5f;
    private Coroutine autoDisableCoroutine;

    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Jump")]
    public float jumpForce = 14f;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private int maxJumps = 2;

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 12f;
    [SerializeField] private float rollDuration = 0.35f;
    [SerializeField] private float rollCooldown = 0.8f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;

    [Header("Combat")]
    public Transform attackPoint;
    public float attackRange = 0.8f;
    public LayerMask enemyLayer;
    public int attackDamage = 10;
    public float attackDelay = 0.12f;
    public float attackCooldown = 0.5f;
    [SerializeField, Range(0f, 1f)] private float blockedDamageMultiplier = 0.25f;

    [Header("Health")]
    public Slider healthSlider;
    public int maxHealth = 100;
    [SerializeField] private float invulnerabilityAfterHit = 0.6f;
    [SerializeField] private Color hurtFlashColor = new Color(1f, 0.35f, 0.35f, 1f);

    /// <summary>(current, max) after any health change.</summary>
    public event System.Action<int, int> HealthChanged;
    /// <summary>Raised once when the player dies.</summary>
    public event System.Action Died;

    // Animator parameters
    const string RUN = "IsRunning";
    const string ATTACK = "Attack";
    const string JUMP = "Jump";
    const string ROLL = "Roll";
    const string HURT = "Hurt";
    const string HEAL = "IsHealing";
    const string BLOCK = "IsBlocking";
    const string DEATH = "Death";
    const string IS_DEAD = "IsDead";

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;
    Player_Collectables collectables;
    Vector3 attackPointFacingRightPosition;

    float moveInput;
    float lastAttackTime;
    float lastRollTime;
    int currentHealth;
    bool isDead = false;
    bool isRespawning;
    float invulnerableUntil;
    Vector3 respawnPoint;
    Color baseColor = Color.white;
    bool isBlocking;
    bool isRolling;
    bool warnedAboutGroundLayer;
    bool warnedAboutMissingGroundCollider;
    int jumpsRemaining;
    readonly List<Collider2D> groundHits = new List<Collider2D>(4);
    ContactFilter2D groundContactFilter;

    public int CurrentHealth => currentHealth;
    /// <summary>Base health plus character level, Heart Vessels and equipped charm.</summary>
    public int MaxHealth => maxHealth + PlayerProgress.LevelHealthBonus + Inventory.MaxHealthBonus;
    /// <summary>Base damage plus character level, weapon and tempering.</summary>
    public int AttackDamage => attackDamage + PlayerProgress.LevelAttackBonus + Inventory.AttackBonus;
    public int Defense => Inventory.DefenseBonus;
    public bool IsWarded => Time.time < wardUntil;
    float wardUntil;

    /// <summary>Set by cinematics: the knight ignores input while true.</summary>
    public static bool ControlLocked;
    int comboStep;
    float knockbackUntil;
    bool wasGrounded = true;
    float lowestAirVelocity;
    float footstepTimer;
    ParticleSystem dust;
    public bool IsDead => isDead;
    public bool IsRespawning => isRespawning;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        collectables = GetComponent<Player_Collectables>();

        groundContactFilter.SetLayerMask(groundLayer);
        groundContactFilter.useTriggers = false;

        if (attackPoint != null)
            attackPointFacingRightPosition = attackPoint.localPosition;

        if (sr != null)
            baseColor = sr.color;
        respawnPoint = transform.position;
    }

    void Start()
    {
        jumpsRemaining = Mathf.Max(1, maxJumps);

        currentHealth = GameSave.LoadHealth(MaxHealth);
        PlayerProgress.LeveledUp += OnLeveledUp;
        Inventory.Changed += OnInventoryChanged;

        if (healthSlider != null)
        {
            healthSlider.maxValue = MaxHealth;
            healthSlider.value = currentHealth;
        }
        HealthChanged?.Invoke(currentHealth, MaxHealth);

        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    void Update()
    {
        UpdateAirState();
        if (isDead || isRespawning) return;

        if (ControlLocked)
        {
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic && Time.time >= knockbackUntil)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (anim != null) { anim.SetBool(RUN, false); anim.SetBool(BLOCK, false); }
            isBlocking = false;
            return;
        }

        if (PauseMenu.IsPaused || InventoryUI.IsOpen || ReadPanel.IsOpen)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (anim != null) anim.SetBool(RUN, false);
            return;
        }

        if (menuPanel != null && Input.GetKeyDown(menuKey))
            ToggleMenu();

        if (menuPanel != null && menuPanel.activeSelf)
            return;

        moveInput = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(moveInput) < 0.1f)
            moveInput = PadMove();

        Move();
        Jump();
        Attack();
        Roll();
        Block();
        Heal();
    }

    void ToggleMenu()
    {
        if (menuPanel == null)
        {
            Debug.LogWarning("Menu panel is not assigned to Player.");
            return;
        }

        bool willOpen = !menuPanel.activeSelf;
        menuPanel.SetActive(willOpen);

        if (willOpen)
        {
            Time.timeScale = 0f;

            if (autoDisableMenu)
            {
                if (autoDisableCoroutine != null)
                    StopCoroutine(autoDisableCoroutine);

                autoDisableCoroutine = StartCoroutine(AutoDisableMenuRoutine(autoDisableSeconds));
            }
        }
        else
        {
            Time.timeScale = 1f;

            if (autoDisableCoroutine != null)
            {
                StopCoroutine(autoDisableCoroutine);
                autoDisableCoroutine = null;
            }
        }
    }

    private IEnumerator AutoDisableMenuRoutine(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        if (menuPanel != null && menuPanel.activeSelf)
        {
            menuPanel.SetActive(false);
            Time.timeScale = 1f;
            autoDisableCoroutine = null;
        }
    }

    void OnDestroy()
    {
        PlayerProgress.LeveledUp -= OnLeveledUp;
        Inventory.Changed -= OnInventoryChanged;
    }

    void OnLeveledUp(int level)
    {
        if (isDead) return;
        currentHealth = MaxHealth;
        HealthChanged?.Invoke(currentHealth, MaxHealth);
        GameHUD.Instance?.ShowAreaTitle($"Level {level}", $"+{PlayerProgress.HealthPerLevel} health   +{PlayerProgress.AttackPerLevel} attack");
        MusicManager.Instance?.PlayHealSfx();
    }

    void OnInventoryChanged()
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth, MaxHealth);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void GrantWard(float seconds)
    {
        wardUntil = Mathf.Max(wardUntil, Time.time + seconds);
    }

    // ---------------- INPUT (keyboard + gamepad) -----------------
    static Gamepad Pad => Gamepad.current;
    static float PadMove()
    {
        if (Pad == null) return 0f;
        float x = Pad.leftStick.x.ReadValue();
        if (Mathf.Abs(x) < 0.3f) x = Pad.dpad.x.ReadValue();
        return Mathf.Abs(x) < 0.3f ? 0f : Mathf.Sign(x);
    }

    private void OnDisable()
    {
        if (menuPanel != null && menuPanel.activeSelf)
            menuPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    // ---------------- MOVEMENT -----------------
    void Move()
    {
        if (rb == null || isRolling || Time.time < knockbackUntil)
            return;

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        if (anim != null)
            anim.SetBool(RUN, Mathf.Abs(moveInput) > 0.1f);

        if (sr != null)
        {
            if (moveInput > 0) sr.flipX = false;
            if (moveInput < 0) sr.flipX = true;
        }

        if (attackPoint != null && sr != null)
        {
            float direction = sr.flipX ? -1f : 1f;
            attackPoint.localPosition = new Vector3(
                Mathf.Abs(attackPointFacingRightPosition.x) * direction,
                attackPointFacingRightPosition.y,
                attackPointFacingRightPosition.z
            );
        }
    }

    // ---------------- JUMP -----------------
    void Jump()
    {
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(jumpKey) ||
                           (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

        try
        {
            jumpPressed |= Input.GetButtonDown("Jump");
        }
        catch (UnityException)
        {
            // The legacy input action may not exist when only the Input System is enabled.
        }

        bool grounded = IsGrounded();
        if (grounded)
            jumpsRemaining = Mathf.Max(1, maxJumps);

        if (jumpPressed && jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            if (anim != null)
                anim.SetTrigger(JUMP);
            jumpsRemaining--;
            MusicManager.Instance?.Play("jump", 0.45f);
            if (grounded) Dust(6);
        }
    }

    bool IsGrounded()
    {
        if (groundLayer.value == 0)
        {
            if (!warnedAboutGroundLayer)
            {
                Debug.LogWarning("Player Ground Layer is empty. Assign the platform layer in the Inspector.", this);
                warnedAboutGroundLayer = true;
            }

            return false;
        }

        Collider2D playerCollider = GetComponent<Collider2D>();
        groundHits.Clear();
        groundContactFilter.SetLayerMask(groundLayer);
        groundContactFilter.useTriggers = false;

        if (groundCheck != null)
        {
            Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundContactFilter,
                groundHits
            );
        }
        else if (playerCollider != null)
        {
            // Thin box just under the collider's feet; follows the collider when the sprite flips.
            Bounds b = playerCollider.bounds;
            Vector2 origin = new Vector2(b.center.x, b.min.y - 0.06f);
            Physics2D.OverlapBox(
                origin,
                new Vector2(b.size.x * 0.85f, 0.12f),
                0f,
                groundContactFilter,
                groundHits
            );
        }
        else
        {
            return false;
        }

        foreach (Collider2D hit in groundHits)
        {
            if (hit != null && hit.attachedRigidbody != rb)
                return true;
        }

        if (!warnedAboutMissingGroundCollider && rb.linearVelocity.y < -5f)
        {
            Debug.LogWarning(
                $"Player is falling without detecting ground in scene '{gameObject.scene.name}'. Check this scene's solid colliders and Ground layer.",
                this
            );
            warnedAboutMissingGroundCollider = true;
        }

        return false;
    }

    // ---------------- ATTACK -----------------
    void Attack()
    {
        bool attackPressed = Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0) ||
                             (Pad != null && Pad.buttonWest.wasPressedThisFrame);
        // Three-hit combo: quick follow-ups inside the combo window, the third blow is a heavy finisher.
        float cooldown = comboStep == 2 ? attackCooldown : attackCooldown * 0.6f;
        if (attackPoint != null && attackPressed &&
            Time.time > lastAttackTime + cooldown && !isRolling)
        {
            comboStep = Time.time - lastAttackTime < 0.85f ? (comboStep + 1) % 3 : 0;
            if (anim != null)
            {
                anim.SetInteger("CombpStep", comboStep);
                anim.SetTrigger(ATTACK);
            }
            lastAttackTime = Time.time;
            MusicManager.Instance?.Play(comboStep == 2 ? "swing_heavy" : "swing", 0.7f, 0.1f);
            StartCoroutine(DoDamage(comboStep));
        }
    }

    IEnumerator DoDamage() { return DoDamage(0); }

    IEnumerator DoDamage(int step)
    {
        yield return new WaitForSeconds(step == 2 ? attackDelay + 0.1f : attackDelay);

        if (attackPoint == null || isDead)
            yield break;

        float range = step == 2 ? attackRange * 1.3f : attackRange;
        int damage = step == 2 ? Mathf.RoundToInt(AttackDamage * 1.6f) : AttackDamage;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, range, enemyLayer);

        var damagedTargets = new System.Collections.Generic.HashSet<Transform>();
        bool landed = false;
        foreach (var h in hits)
        {
            Transform target = h.transform.root;
            if (!damagedTargets.Add(target))
                continue;
            h.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            landed = true;
            bool bony = target.GetComponentInChildren<PatrolSkeletonAI>() != null || target.GetComponentInChildren<Enemy_behaviour>() != null;
            bool armored = target.GetComponentInChildren<BossKnightAI>() != null;
            MusicManager.Instance?.PlayAt(armored ? "clank" : bony ? "hit_bone" : "hit", h.bounds.center, 0.85f);
        }

        if (landed)
        {
            ScreenShake.Shake(step == 2 ? 0.22f : 0.08f);
            if (step == 2) StartCoroutine(HitStop(0.06f));
        }
    }

    IEnumerator HitStop(float seconds)
    {
        if (Time.timeScale < 0.99f) yield break;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(seconds);
        if (!PauseMenu.IsPaused && !InventoryUI.IsOpen && !ReadPanel.IsOpen && !ShrineMenu.IsOpen)
            Time.timeScale = 1f;
    }

    // ---------------- ANIMATION STATE, LANDING, FOOTSTEPS -----------------
    void UpdateAirState()
    {
        if (anim == null || rb == null) return;
        bool grounded = !isDead && IsGroundedQuiet();
        anim.SetBool("IsGrounded", grounded);
        anim.SetFloat("YVel", rb.linearVelocity.y);
        if (!grounded) lowestAirVelocity = Mathf.Min(lowestAirVelocity, rb.linearVelocity.y);
        if (grounded && !wasGrounded && lowestAirVelocity < -6f)
        {
            MusicManager.Instance?.Play("land", Mathf.Clamp01(-lowestAirVelocity / 22f) + 0.2f);
            Dust(Mathf.Clamp((int)(-lowestAirVelocity), 4, 14));
            if (lowestAirVelocity < -18f) ScreenShake.Shake(0.12f);
        }
        if (grounded) lowestAirVelocity = 0f;
        wasGrounded = grounded;

        bool running = grounded && !isRolling && Mathf.Abs(rb.linearVelocity.x) > 1f && !ControlLocked;
        if (running)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                footstepTimer = 0.3f;
                MusicManager.Instance?.Play(MusicManager.Instance.FootstepKey, 0.32f, 0.1f, 0.05f);
            }
        }
        else footstepTimer = 0.05f;
    }

    bool IsGroundedQuiet()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null || !c.enabled) return false;
        Bounds b = c.bounds;
        return Physics2D.OverlapBox(new Vector2(b.center.x, b.min.y - 0.06f), new Vector2(b.size.x * 0.85f, 0.12f), 0f, groundLayer);
    }

    void Dust(int count)
    {
        if (dust == null)
        {
            var assets = CinematicAssets.Instance;
            var go = new GameObject("Dust");
            go.transform.SetParent(transform, false);
            dust = go.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (assets != null) r.sharedMaterial = assets.particleMaterial;
            r.sortingLayerName = "player"; r.sortingOrder = 5;
            var main = dust.main;
            main.playOnAwake = false; main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startColor = new Color(0.75f, 0.7f, 0.65f, 0.55f);
            main.gravityModifier = 0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = dust.emission; em.rateOverTime = 0f;
            var sh = dust.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 70f; sh.rotation = new Vector3(-90f, 0, 0); sh.radius = 0.4f;
            var tsa = dust.textureSheetAnimation; tsa.enabled = true; tsa.mode = ParticleSystemAnimationMode.Sprites;
            if (assets != null) tsa.SetSprite(0, assets.softGlow);
            var col = dust.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
        }
        Collider2D c = GetComponent<Collider2D>();
        dust.transform.position = c != null ? new Vector3(c.bounds.center.x, c.bounds.min.y + 0.1f, 0f) : transform.position;
        dust.Emit(count);
    }

    // ---------------- ROLL -----------------
    void Roll()
    {
        bool rollPressed = Input.GetKeyDown(KeyCode.LeftShift) || (Pad != null && Pad.buttonEast.wasPressedThisFrame);
        if (!isRolling && rollPressed &&
            Time.time >= lastRollTime + rollCooldown)
            StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        isRolling = true;
        lastRollTime = Time.time;

        if (anim != null)
            anim.SetTrigger(ROLL);
        MusicManager.Instance?.Play("roll", 0.5f);

        float direction = sr != null && sr.flipX ? -1f : 1f;
        rb.linearVelocity = new Vector2(direction * rollSpeed, rb.linearVelocity.y);
        yield return new WaitForSeconds(rollDuration);

        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        isRolling = false;
    }

    // ---------------- BLOCK -----------------
    void Block()
    {
        isBlocking = Input.GetMouseButton(1) || Input.GetKey(KeyCode.K) || (Pad != null && Pad.leftTrigger.isPressed);
        if (anim != null)
            anim.SetBool(BLOCK, isBlocking);
    }

    // ---------------- HEAL -----------------
    void Heal()
    {
        bool healPressed = Input.GetKeyDown(KeyCode.H) || (Pad != null && Pad.rightShoulder.wasPressedThisFrame);
        if (healPressed &&
            collectables != null &&
            collectables.TryUseHealingFlask(this))
        {
            if (anim != null)
                anim.SetBool(HEAL, true);
            StartCoroutine(StopHealAnim());
        }
    }

    IEnumerator StopHealAnim()
    {
        yield return new WaitForSeconds(0.5f);
        if (anim != null)
            anim.SetBool(HEAL, false);
    }

    public bool TryHeal(int amount)
    {
        if (isDead || amount <= 0 || currentHealth >= MaxHealth)
            return false;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);

        if (healthSlider != null)
            healthSlider.value = currentHealth;
        HealthChanged?.Invoke(currentHealth, MaxHealth);

        return currentHealth > previousHealth;
    }

    /// <summary>Raises health to at least the given amount (used by checkpoints).</summary>
    public void RestoreHealthTo(int amount)
    {
        if (isDead || currentHealth >= amount)
            return;
        TryHeal(amount - currentHealth);
    }

    // ---------------- TAKE DAMAGE -----------------
    public void TakeDamage(int amount)
    {
        TakeDamage(amount, false);
    }

    /// <summary>Damage from an attacker: also knocks the knight back away from the blow.</summary>
    public void TakeDamageFrom(int amount, Vector3 source)
    {
        int before = currentHealth;
        TakeDamage(amount, false);
        if (currentHealth < before && !isDead && rb != null && !isBlocking)
        {
            float dir = Mathf.Sign(transform.position.x - source.x);
            if (dir == 0f) dir = 1f;
            rb.linearVelocity = new Vector2(dir * 7f, 5f);
            knockbackUntil = Time.time + 0.22f;
        }
    }

    public void TakeDamage(int amount, bool ignoreInvulnerability)
    {
        if (isDead || isRespawning || amount <= 0) return;
        if (!ignoreInvulnerability && (isRolling || Time.time < invulnerableUntil)) return;
        invulnerableUntil = Time.time + invulnerabilityAfterHit;

        int reduced = Mathf.Max(1, amount - Defense);
        if (IsWarded)
            reduced = Mathf.Max(1, Mathf.CeilToInt(reduced * 0.5f));
        int appliedDamage = isBlocking
            ? Mathf.Max(1, Mathf.CeilToInt(reduced * blockedDamageMultiplier))
            : reduced;
        currentHealth = Mathf.Max(0, currentHealth - appliedDamage);

        if (isBlocking)
        {
            MusicManager.Instance?.Play("block", 0.8f);
            ScreenShake.Shake(0.06f);
        }
        else
        {
            if (anim != null)
                anim.SetTrigger(HURT);
            MusicManager.Instance?.Play("player_hurt", 0.75f, 0.08f);
            ScreenShake.Shake(0.14f);
        }

        if (healthSlider != null)
            healthSlider.value = currentHealth;
        HealthChanged?.Invoke(currentHealth, MaxHealth);

        if (currentHealth <= 0)
            Die();
        else
            StartCoroutine(HurtFlash());
    }

    IEnumerator HurtFlash()
    {
        if (sr == null)
            yield break;
        float end = Time.time + invulnerabilityAfterHit;
        while (Time.time < end && !isDead)
        {
            sr.color = hurtFlashColor;
            yield return new WaitForSeconds(0.06f);
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.55f);
            yield return new WaitForSeconds(0.06f);
        }
        sr.color = baseColor;
    }

    // ---------------- RESPAWN -----------------
    public void SetRespawnPoint(Vector3 position)
    {
        respawnPoint = position;
    }

    public void TeleportTo(Vector3 position)
    {
        transform.position = position;
        if (rb != null)
        {
            rb.position = position;
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>Fade out, move to the last checkpoint, fade back in (used by hazards).</summary>
    public IEnumerator RespawnRoutine()
    {
        if (isRespawning)
            yield break;
        isRespawning = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (GameHUD.Instance != null)
            yield return GameHUD.Instance.FadeScreen(1f, 0.25f);
        else
            yield return new WaitForSeconds(0.25f);

        transform.position = respawnPoint;
        if (rb != null)
        {
            rb.position = respawnPoint;
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }
        invulnerableUntil = Time.time + 1f;
        isRespawning = false;
        if (anim != null) anim.SetTrigger("Rise");
        MusicManager.Instance?.Play("rise", 0.5f, 0.02f);
        Dust(8);

        if (GameHUD.Instance != null)
            yield return GameHUD.Instance.FadeScreen(0f, 0.35f);
    }

    /// <summary>Bring a dead player back at the last checkpoint with full health.</summary>
    public void Revive()
    {
        StopAllCoroutines();
        isDead = false;
        isRespawning = false;
        enabled = true;
        foreach (Collider2D c in GetComponents<Collider2D>())
            c.enabled = true;

        if (anim != null)
        {
            anim.SetBool(IS_DEAD, false);
            anim.Rebind();
            anim.Update(0f);
            anim.SetTrigger("Rise");
        }
        MusicManager.Instance?.Play("rise", 0.8f, 0.02f);
        Dust(12);

        if (sr != null)
            sr.color = baseColor;

        transform.position = respawnPoint;
        if (rb != null)
        {
            rb.simulated = true;
            rb.position = respawnPoint;
            rb.linearVelocity = Vector2.zero;
        }

        currentHealth = MaxHealth;
        if (healthSlider != null)
            healthSlider.value = currentHealth;
        HealthChanged?.Invoke(currentHealth, MaxHealth);
        invulnerableUntil = Time.time + 1.5f;
    }

    // ---------------- DEATH -----------------
    void Die()
    {
        isDead = true;
        MusicManager.Instance?.Play("player_death", 0.9f, 0.02f);
        ScreenShake.Shake(0.3f);
        StartCoroutine(DeathSlowMotion());
        if (anim != null)
        {
            anim.SetBool(IS_DEAD, true);
            anim.SetTrigger(DEATH);
        }

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        foreach (Collider2D c in GetComponents<Collider2D>())
            c.enabled = false;

        if (sr != null)
            sr.color = baseColor;

        Died?.Invoke();
        if (GameHUD.Instance != null)
            GameHUD.Instance.ShowGameOver(this);
        else if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOverBackground();

        // The player object is kept (not destroyed) so it can rise again at the last checkpoint.
        this.enabled = false;
    }

    IEnumerator DeathSlowMotion()
    {
        Time.timeScale = 0.35f;
        yield return new WaitForSecondsRealtime(0.9f);
        if (!PauseMenu.IsPaused) Time.timeScale = 1f;
    }

    /// <summary>Cinematic resurrection: plays the rising animation (the death animation in reverse).</summary>
    public void PlayRise()
    {
        if (anim != null) anim.SetTrigger("Rise");
    }

    // ---------------- GIZMOS -----------------
    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}

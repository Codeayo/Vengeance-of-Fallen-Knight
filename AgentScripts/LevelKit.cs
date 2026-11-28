using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Shared editor-time level construction toolkit used by every level builder.</summary>
public static class LevelKit
{
    public static StringBuilder Log = new StringBuilder();
    public static Transform Root;
    public static Transform Terrain, Enemies, Content, Decor, Fx;
    public static int GroundLayer => LayerMask.NameToLayer("Ground");
    public static int GroundMask => LayerMask.GetMask("Ground");
    public static float MinX, MaxX;
    static readonly List<(float x0, float x1, float y)> grounds = new List<(float, float, float)>();

    // ------------------------------------------------------------------ styles
    public class Style
    {
        public Sprite top;            // tiled horizontally along the surface
        public float surfaceInset;    // distance from the sprite's top edge down to the walkable surface
        public Color topTint = Color.white;
        public Sprite body;           // tiled below the top strip (null = solid colour)
        public Color bodyColor = new Color(0.05f, 0.04f, 0.05f);
        public Sprite platform;       // floating jump-through ledges
        public float platformInset = 0.15f;
        public float platformHeight;  // 0 = sprite height
        public string sortingLayer = "Ground";
    }

    public static Sprite SpriteAt(string texPath, string name) =>
        AssetDatabase.LoadAllAssetsAtPath(texPath).OfType<Sprite>().FirstOrDefault(s => s.name == name);

    public static Sprite SpriteFile(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

    /// <summary>Tiled SpriteRenderers need FullRect meshes.</summary>
    public static void EnsureFullRect(Sprite s)
    {
        if (s == null) return;
        string p = AssetDatabase.GetAssetPath(s);
        var ti = AssetImporter.GetAtPath(p) as TextureImporter;
        if (ti == null) return;
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        if (settings.spriteMeshType != SpriteMeshType.FullRect || ti.wrapMode != TextureWrapMode.Clamp)
        {
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }
    }

    // ------------------------------------------------------------------ scene
    public static Scene Begin(string scenePath, string rootName)
    {
        Log = new StringBuilder();
        grounds.Clear();
        UIKit.EnsureGenerated();
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects().Where(g => g.name.StartsWith("[L]")).ToArray())
            Object.DestroyImmediate(go);
        Root = new GameObject("[L] " + rootName).transform;
        Terrain = Child(Root, "Terrain");
        Enemies = Child(Root, "Enemies");
        Content = Child(Root, "Gameplay");
        Decor = Child(Root, "Decor");
        Fx = Child(Root, "Atmosphere");
        return scene;
    }

    /// <summary>Creates the content containers in the already-open scene (for levels built elsewhere).</summary>
    public static void Attach(string rootName)
    {
        Log = new StringBuilder();
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name.StartsWith("[L]")).ToArray())
            Object.DestroyImmediate(go);
        Root = new GameObject("[L] " + rootName).transform;
        Terrain = Child(Root, "Terrain"); Enemies = Child(Root, "Enemies"); Content = Child(Root, "Gameplay"); Decor = Child(Root, "Decor"); Fx = Child(Root, "Atmosphere");
    }

    /// <summary>Adds rewards to an enemy that was placed by another builder.</summary>
    public static void Reward(GameObject enemy, string name, int xp, bool boss, params EnemyCore.Loot[] loot)
    {
        var core = enemy.GetComponent<EnemyCore>(); if (core == null) core = enemy.AddComponent<EnemyCore>();
        core.Configure(name, xp, boss ? 15 : 1, boss ? 25 : 4, boss, loot.Length > 0 ? loot : new[] { L("crimson_flask", 0.12f), L("soul_shard", 0.2f), L("verdant_shard", 0.1f) });
        EditorUtility.SetDirty(core);
        if (PrefabUtility.IsPartOfPrefabInstance(enemy)) PrefabUtility.RecordPrefabInstancePropertyModifications(core);
    }

    public static void Save(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static Transform Child(Transform parent, string name, Vector3? pos = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (pos.HasValue) go.transform.position = pos.Value;
        return go.transform;
    }

    /// <summary>Disables the old broken terrain, legacy canvas, scene switchers and stray coins.</summary>
    public static void RetireLegacy(params string[] tilemapNames)
    {
        foreach (var tm in Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (tilemapNames.Contains(tm.name))
                tm.gameObject.SetActive(false);

        foreach (var name in new[] { "Canvas", "LevelLoader", "NextLevel", "DeathBoxx", "GameObject", "SceneSwitcher" })
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name == name))
                go.SetActive(false);

        foreach (var sm in Object.FindObjectsByType<SceneManagement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(sm);

        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name.StartsWith("Coin") || g.name.StartsWith("Enemy_1")).ToArray())
            Object.DestroyImmediate(go);
        foreach (var h in Object.FindObjectsByType<GameHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(h.gameObject);
    }

    // ------------------------------------------------------------------ terrain
    /// <summary>Solid ground from x0 to x1 with its walkable surface at y. Extends down to the bottom.</summary>
    public static GameObject Ground(float x0, float x1, float y, Style st, float depth = 14f)
    {
        var go = new GameObject($"Ground {x0:F0}-{x1:F0} @{y:F1}");
        go.transform.SetParent(Terrain, false);
        go.layer = GroundLayer;
        go.transform.position = new Vector3((x0 + x1) * 0.5f, y, 0);
        float w = x1 - x0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, depth);
        col.offset = new Vector2(0, -depth * 0.5f);

        float topH = st.top.bounds.size.y;
        var top = Tiled(go.transform, "Surface", st.top, new Vector3(0, -topH * 0.5f + st.surfaceInset, 0), new Vector2(w, topH), st.sortingLayer, 2, st.topTint);
        float bodyTop = -(topH - st.surfaceInset) + 0.05f;
        float bodyH = depth + bodyTop;
        if (bodyH > 0.1f)
        {
            if (st.body != null)
                Tiled(go.transform, "Body", st.body, new Vector3(0, bodyTop - bodyH * 0.5f, 0), new Vector2(w, bodyH), st.sortingLayer, 1, Color.white);
            else
                Solid(go.transform, "Body", new Vector3(0, bodyTop - bodyH * 0.5f, 0), new Vector2(w, bodyH), st.sortingLayer, 1, st.bodyColor);
        }

        grounds.Add((x0, x1, y));
        MinX = Mathf.Min(MinX, x0);
        MaxX = Mathf.Max(MaxX, x1);
        return go;
    }

    /// <summary>Floating ledge the knight can jump up through and stand on.</summary>
    public static GameObject Ledge(float x0, float x1, float y, Style st)
    {
        var sprite = st.platform ?? st.top;
        var go = new GameObject($"Ledge {x0:F0}-{x1:F0} @{y:F1}");
        go.transform.SetParent(Terrain, false);
        go.layer = GroundLayer;
        go.transform.position = new Vector3((x0 + x1) * 0.5f, y, 0);
        float w = x1 - x0;
        float h = st.platformHeight > 0 ? st.platformHeight : sprite.bounds.size.y;
        Tiled(go.transform, "Surface", sprite, new Vector3(0, -h * 0.5f + st.platformInset, 0), new Vector2(w, h), st.sortingLayer, 3, st.topTint);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, 0.4f);
        col.offset = new Vector2(0, -0.2f);
        col.usedByEffector = true;
        var eff = go.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.surfaceArc = 160f;
        return go;
    }

    /// <summary>Plain colour block (a scaled sprite, not a tiled one).</summary>
    public static SpriteRenderer Solid(Transform parent, string name, Vector3 localPos, Vector2 size, string layer, int order, Color tint)
    {
        var px = UIKit.Gen("WhitePixel");
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x / px.bounds.size.x, size.y / px.bounds.size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = px;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = tint;
        return sr;
    }

    public static SpriteRenderer Tiled(Transform parent, string name, Sprite sprite, Vector3 localPos, Vector2 size, string layer, int order, Color tint)
    {
        EnsureFullRect(sprite);
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = tint;
        return sr;
    }

    /// <summary>Boundary walls, pit hazards between ground segments and an abyss safety volume.</summary>
    public static void FinishTerrain(Style st, float wallHeight = 14f)
    {
        var sorted = grounds.OrderBy(g => g.x0).ToList();
        var first = sorted.First();
        var last = sorted.Last();
        Wall(first.x0 - 3f, first.x0, first.y, wallHeight, st);
        Wall(last.x1, last.x1 + 3f, last.y, wallHeight, st);

        var hz = Child(Content, "Hazards");
        int pits = 0;
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            float gapStart = sorted[i].x1, gapEnd = sorted[i + 1].x0;
            if (gapEnd - gapStart < 0.6f) continue;
            float rim = Mathf.Min(sorted[i].y, sorted[i + 1].y);
            var zone = Child(hz, $"Pit {++pits}", new Vector3((gapStart + gapEnd) * 0.5f, rim - 6f, 0));
            zone.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            var box = zone.gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(gapEnd - gapStart + 0.6f, 6f);
            var h = zone.gameObject.AddComponent<HazardZone>();
            SetS(h, "message", "You fall into the darkness");
            SetI(h, "damage", 15);
        }

        float minY = sorted.Min(g => g.y);
        var abyss = Child(hz, "Abyss", new Vector3((MinX + MaxX) * 0.5f, minY - 22f, 0));
        abyss.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        var ab = abyss.gameObject.AddComponent<BoxCollider2D>();
        ab.isTrigger = true;
        ab.size = new Vector2(MaxX - MinX + 40f, 8f);
        var az = abyss.gameObject.AddComponent<HazardZone>();
        SetI(az, "damage", 20);
        Log.AppendLine($"terrain: {sorted.Count} ground segments x {MinX:F0}..{MaxX:F0}, {pits} pits with respawn hazards");
    }

    static void Wall(float x0, float x1, float baseY, float height, Style st)
    {
        var go = Ground(x0, x1, baseY + height, st, height + 14f);
        go.name = "Boundary Wall";
        grounds.RemoveAt(grounds.Count - 1);
    }

    public static float SurfaceY(float x)
    {
        Physics2D.SyncTransforms();
        var hit = Physics2D.Raycast(new Vector2(x, 80f), Vector2.down, 200f, GroundMask);
        return hit ? hit.point.y : 0f;
    }

    /// <summary>Surface of solid ground only (ignores floating ledges).</summary>
    public static float FloorY(float x)
    {
        Physics2D.SyncTransforms();
        foreach (var h in Physics2D.RaycastAll(new Vector2(x, 80f), Vector2.down, 200f, GroundMask).OrderByDescending(h => h.point.y))
            if (h.collider.GetComponent<PlatformEffector2D>() == null) return h.point.y;
        return 0f;
    }

    // ------------------------------------------------------------------ camera, background, light
    public static void Camera(float lens = 8.5f, float topY = 30f, Color? sky = null)
    {
        var cm = Object.FindFirstObjectByType<CinemachineCamera>();
        var lensSettings = cm.Lens;
        lensSettings.OrthographicSize = lens;
        cm.Lens = lensSettings;
        var player = GameObject.FindGameObjectWithTag("Player");
        cm.Follow = player.transform;
        var pos = cm.GetComponent<CinemachinePositionComposer>();
        if (pos != null)
        {
            pos.Damping = new Vector3(0.6f, 0.9f, 0f);
            var comp = pos.Composition;
            comp.ScreenPosition = new Vector2(-0.08f, 0.2f);
            pos.Composition = comp;
            pos.Lookahead.Enabled = true;
            pos.Lookahead.Time = 0.25f;
            pos.Lookahead.Smoothing = 8f;
        }

        // Old camera confiners in these scenes were solid colliders that trapped enemies and players.
        foreach (var old in Object.FindObjectsByType<PolygonCollider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (old.name != "[L] Camera Bounds")
            {
                old.isTrigger = true;
                old.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }

        var conf2d = cm.GetComponent<CinemachineConfiner2D>();
        if (conf2d == null) conf2d = cm.gameObject.AddComponent<CinemachineConfiner2D>();
        var confGo = GameObject.Find("[L] Camera Bounds") ?? new GameObject("[L] Camera Bounds");
        var poly = confGo.GetComponent<PolygonCollider2D>(); if (poly == null) poly = confGo.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        confGo.layer = LayerMask.NameToLayer("Ignore Raycast");
        float minY = grounds.Min(g => g.y) - 9f;
        poly.pathCount = 1;
        poly.SetPath(0, new[] { new Vector2(MinX - 3.5f, minY), new Vector2(MaxX + 3.5f, minY), new Vector2(MaxX + 3.5f, topY), new Vector2(MinX - 3.5f, topY) });
        conf2d.BoundingShape2D = poly;
        conf2d.InvalidateBoundingShapeCache();

        var cam = UnityEngine.Camera.main;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = sky ?? new Color(0.04f, 0.04f, 0.07f);
    }

    /// <summary>Camera-following background layer tiled across the level.</summary>
    public static void Backdrop(Sprite sprite, float follow, float height, float y, string layer, int order, Color tint)
    {
        EnsureFullRect(sprite);
        var go = new GameObject("Backdrop " + sprite.name);
        go.transform.SetParent(Decor, false);
        float scale = height / sprite.bounds.size.y;
        float len = (MaxX - MinX) * (1f - follow) + 90f;
        go.transform.position = new Vector3(MinX + len * 0.5f - 45f, y, 0);
        go.transform.localScale = new Vector3(scale, scale, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(len / scale, sprite.bounds.size.y);
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = tint;
        var pf = go.AddComponent<ParallaxFollow>();
        SetF(pf, "followX", follow);
        SetF(pf, "followY", Mathf.Clamp01(follow + 0.05f));
    }

    public static void Atmosphere(Color ambient, float intensity, Color fog, float fogAlpha, VolumeLook look, string particles, Color particleA, Color particleB)
    {
        UIKit.Light("Ambient (Global)", Fx, Light2D.LightType.Global, Vector3.zero, ambient, intensity);
        var sorted = grounds.OrderBy(g => g.x0).ToList();
        // Fog bands follow the terrain height in 60-unit chunks.
        for (float x = MinX; x < MaxX; x += 60f)
        {
            float x1 = Mathf.Min(MaxX, x + 60f);
            var inRange = sorted.Where(g => g.x1 > x && g.x0 < x1).ToList();
            float y = inRange.Count > 0 ? inRange.Average(g => g.y) : 0f;
            var back = UIKit.Particles($"Fog {x:F0}", Fx, new Vector3((x + x1) * 0.5f, y + 0.8f, 0), UIKit.Gen("Mist"), "Backtiles", 10);
            UIKit.Fog(back, new Vector3(x1 - x, 1.8f, 1f), (x1 - x) * 0.06f, new Color(fog.r, fog.g, fog.b, fogAlpha), new Vector2(5f, 9f), 0.25f);
            var front = UIKit.Particles($"Low Mist {x:F0}", Fx, new Vector3((x + x1) * 0.5f, y - 0.7f, 0), UIKit.Gen("Mist"), "Grill", 25);
            UIKit.Fog(front, new Vector3(x1 - x, 0.4f, 1f), (x1 - x) * 0.035f, new Color(fog.r, fog.g, fog.b, fogAlpha * 0.6f), new Vector2(3.5f, 6f), 0.4f);
        }

        float midY = sorted.Average(g => g.y);
        var ps = UIKit.Particles(particles, Fx, new Vector3((MinX + MaxX) * 0.5f, midY + 8f, 0), UIKit.Gen("SoftCircle"), "Grill", 6);
        if (particles == "Snow")
        {
            UIKit.Embers(ps, new Vector3(MaxX - MinX + 40f, 1f, 1f), (MaxX - MinX) * 0.35f, particleA, particleB, 0.16f);
            var vel = ps.velocityOverLifetime;
            vel.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.2f);
            vel.y = new ParticleSystem.MinMaxCurve(-1.6f, -0.8f);
            ps.transform.position += Vector3.up * 10f;
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 14f);
        }
        else
        {
            UIKit.Embers(ps, new Vector3(MaxX - MinX + 20f, 18f, 1f), (MaxX - MinX) * 0.08f, particleA, particleB, 0.2f);
        }

        UIKit.GlobalVolume(UIKit.Profile(look.name, look.vignette, look.bloom, look.contrast, look.saturation, look.tint)).transform.SetParent(Fx, false);
    }

    public struct VolumeLook
    {
        public string name; public float vignette, bloom, contrast, saturation; public Color tint;
        public VolumeLook(string n, float v, float b, float c, float s, Color t) { name = n; vignette = v; bloom = b; contrast = c; saturation = s; tint = t; }
    }

    public static Light2D PointLight(float x, float yAboveGround, Color c, float intensity, float radius)
    {
        return UIKit.Light("Light", Fx, Light2D.LightType.Point, new Vector3(x, SurfaceY(x) + yAboveGround, 0), c, intensity, radius, radius * 0.15f);
    }

    // ------------------------------------------------------------------ player, UI
    public static void Player(float spawnX, string music = null)
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        p.layer = LayerMask.NameToLayer("Player");
        var rb = p.GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Art/Generated/PlayerNoFriction.physicsMaterial2D");
        var box = p.GetComponent<BoxCollider2D>();
        box.sharedMaterial = mat;
        box.size = new Vector2(2.0f, 3.25f);
        box.offset = new Vector2(0.92f, 0f);
        if (p.GetComponent<FlipColliderSync>() == null) p.AddComponent<FlipColliderSync>();
        if (p.GetComponent<PlayerInteractor>() == null) p.AddComponent<PlayerInteractor>();
        var player = p.GetComponent<global::Player>();
        player.groundLayer = GroundMask;
        player.groundCheck = null;
        var so = new SerializedObject(player);
        so.FindProperty("menuPanel").objectReferenceValue = null;
        so.FindProperty("healthSlider").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();
        Set(p.GetComponent<Player_Collectables>(), "currentCoin_Text", null);
        float y = SurfaceY(spawnX);
        p.transform.position = new Vector3(spawnX, y + 1.64f, 0);
        if (PrefabUtility.IsPartOfPrefabInstance(p))
            foreach (Object o in new Object[] { p, p.transform, rb, box, player })
                PrefabUtility.RecordPrefabInstancePropertyModifications(o);
    }

    public static void Hud()
    {
        foreach (var h in Object.FindObjectsByType<GameHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(h.gameObject);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildHud.PrefabPath);
        var hud = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        hud.name = "[L] HUD";
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            // The module creates its default UI actions itself when none are assigned.
            new GameObject("[L] EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }

    // ------------------------------------------------------------------ gameplay objects
    public static Checkpoint Shrine(string name, float x)
    {
        float gy = SurfaceY(x);
        var go = Child(Content, name, new Vector3(x, gy, 0)).gameObject;
        Sprite(go.transform, "Statue", SpriteFile("Assets/Art/Graveyard/Salt.png"), new Vector3(x, gy + 3.0f, 0), 3.0f, "Backtiles", 6, Color.white);
        Sprite(go.transform, "Brazier", UIKit.Gen("Diamond"), new Vector3(x + 1.3f, gy + 0.35f, 0), 0.6f, "Backtiles", 7, new Color(0.35f, 0.3f, 0.28f)).transform.localScale = new Vector3(0.6f, 0.35f, 1);
        var glow = Sprite(go.transform, "Glow", UIKit.Gen("SoftCircle"), new Vector3(x + 1.3f, gy + 0.9f, 0), 2.4f, "Backtiles", 8, new Color(1f, 0.55f, 0.2f, 0.55f));
        var flame = UIKit.Particles("Flame", go.transform, new Vector3(x + 1.3f, gy + 0.55f, 0), UIKit.Gen("SoftCircle"), "Grill", 5);
        var main = flame.main;
        main.prewarm = false; main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.15f, 0.9f), new Color(1f, 0.85f, 0.4f, 0.9f));
        main.startSpeed = 0f;
        var em = flame.emission; em.rateOverTime = 28f;
        var sh = flame.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(0.4f, 0.05f, 0.1f);
        var vel = flame.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f); vel.y = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var sz = flame.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.1f));
        var light = UIKit.Light("Flame Light", go.transform, Light2D.LightType.Point, new Vector3(x + 1.3f, gy + 1.2f, 0), new Color(1f, 0.6f, 0.3f), 0.25f, 6f, 0.4f);
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true; box.offset = new Vector2(0.6f, 2f); box.size = new Vector2(3f, 4f);
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        var cp = go.AddComponent<Checkpoint>();
        var respawn = Child(go.transform, "Respawn Point", new Vector3(x + 2.6f, gy + 1.7f, 0));
        var so = new SerializedObject(cp);
        so.FindProperty("checkpointName").stringValue = name;
        so.FindProperty("respawnPoint").objectReferenceValue = respawn;
        so.FindProperty("flameLight").objectReferenceValue = light;
        so.FindProperty("flameParticles").objectReferenceValue = flame;
        var gs = so.FindProperty("glowSprites"); gs.arraySize = 1; gs.GetArrayElementAtIndex(0).objectReferenceValue = glow;
        so.ApplyModifiedPropertiesWithoutUndo();
        go.AddComponent<ShrineInteraction>();
        return cp;
    }

    public static void Banner(float x, string title, string sub)
    {
        var go = Child(Content, "Banner: " + title, new Vector3(x, SurfaceY(x) + 4f, 0)).gameObject;
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        var b = go.AddComponent<BoxCollider2D>(); b.isTrigger = true; b.size = new Vector2(1f, 14f);
        var ab = go.AddComponent<AreaBanner>();
        SetS(ab, "title", title); SetS(ab, "subtitle", sub);
    }

    public static void Note(float x, string title, string body, Sprite visual, float scale = 1f, string verb = "Read")
    {
        float gy = SurfaceY(x);
        var go = Child(Content, "Note: " + title, new Vector3(x, gy + 1.2f, 0)).gameObject;
        if (visual != null)
        {
            var sr = Sprite(go.transform, "Visual", visual, new Vector3(x, gy + visual.bounds.extents.y * scale - 0.05f, 0), scale, "Backtiles", 12, Color.white);
        }
        var marker = Sprite(go.transform, "Glint", UIKit.Gen("SoftCircle"), new Vector3(x, gy + 1.6f * Mathf.Max(1f, scale), 0), 0.9f, "Grill", 3, new Color(1f, 0.85f, 0.5f, 0.6f));
        var n = go.AddComponent<LoreNote>();
        n.Configure(title, body, verb);
        Set(n, "unreadMarker", marker.gameObject);
    }

    public static void Pickup(string id, float x, float yAboveGround = 1.2f, int amount = 1, string unique = null)
    {
        var p = ItemPickup.Spawn(id, amount, new Vector3(x, SurfaceY(x) + yAboveGround, 0), unique);
        p.transform.SetParent(Content, true);
        var so = new SerializedObject(p);
        so.FindProperty("itemId").stringValue = id;
        so.FindProperty("amount").intValue = amount;
        so.FindProperty("uniqueId").stringValue = unique ?? "";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void Coins(float x0, float x1, float yAbove, int count)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Coin.prefab");
        for (int i = 0; i < count; i++)
        {
            float x = count == 1 ? x0 : Mathf.Lerp(x0, x1, i / (float)(count - 1));
            var c = (GameObject)PrefabUtility.InstantiatePrefab(prefab, Content);
            c.transform.position = new Vector3(x, SurfaceY(x) + yAbove + Mathf.Sin(i * 0.9f) * 0.3f, 0);
            var rb = c.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; PrefabUtility.RecordPrefabInstancePropertyModifications(rb); }
        }
    }

    public static void Gate(float x, string keyId, string id, Sprite bars, float height = 4.5f)
    {
        float gy = SurfaceY(x);
        var go = Child(Content, "Locked Gate (" + keyId + ")", new Vector3(x, gy, 0)).gameObject;
        var barsT = Child(go.transform, "Bars", new Vector3(x, gy, 0));
        int n = Mathf.CeilToInt(height / bars.bounds.size.y);
        for (int i = 0; i < n; i++)
            Sprite(barsT, "Bar", bars, new Vector3(x, gy + bars.bounds.extents.y + i * bars.bounds.size.y, 0), 1f, "Grill", 5, new Color(0.8f, 0.8f, 0.9f));
        var block = go.AddComponent<BoxCollider2D>();
        block.offset = new Vector2(0, height * 0.5f);
        block.size = new Vector2(0.8f, height);
        go.layer = GroundLayer;
        var g = go.AddComponent<LockedGate>();
        g.Configure(keyId, barsT, block, id);
    }

    public static void Trap(float x, float delay, Sprite[] tips, Sprite[] bars)
    {
        float gy = SurfaceY(x);
        var go = Child(Content, $"Spike Trap x{x:F0}", new Vector3(x, gy, 0)).gameObject;
        var spikes = Child(go.transform, "Spikes", new Vector3(x, gy - 1.6f, 0));
        for (int i = 0; i < 4; i++)
        {
            Sprite(spikes, "Tip " + i, tips[i], new Vector3(x - 1.5f + i, gy - 1.65f, 0), 1f, "Backtiles", 9, Color.white);
            Sprite(spikes, "Bar " + i, bars[i], new Vector3(x - 1.5f + i, gy - 2.65f, 0), 1f, "Backtiles", 9, Color.white);
        }
        var dmg = Child(go.transform, "Damage Area", new Vector3(x, gy + 0.6f, 0)).gameObject;
        dmg.layer = LayerMask.NameToLayer("Ignore Raycast");
        var box = dmg.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(3.4f, 1.1f);
        var trap = dmg.AddComponent<SpikeTrap>();
        var so = new SerializedObject(trap);
        so.FindProperty("spikes").objectReferenceValue = spikes;
        so.FindProperty("damageArea").objectReferenceValue = box;
        so.FindProperty("raisedHeight").floatValue = 1.55f;
        so.FindProperty("startDelay").floatValue = delay;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static LevelExit Exit(float x, string next, GameObject[] required, Color sealedColor, string sealedMessage)
    {
        float gy = SurfaceY(x);
        var gate = Child(Content, "Exit -> " + next, new Vector3(x, gy, 0)).gameObject;
        var statue = SpriteFile("Assets/Art/Graveyard/Salt.png");
        Sprite(gate.transform, "Statue West", statue, new Vector3(x - 2.9f, gy + 3.3f, 0), 3.3f, "Backtiles", 6, Color.white);
        Sprite(gate.transform, "Statue East", statue, new Vector3(x + 2.9f, gy + 3.3f, 0), 3.3f, "Backtiles", 6, Color.white).flipX = true;
        var sealedFx = Child(gate.transform, "Sealed", new Vector3(x, gy, 0)).gameObject;
        Sprite(sealedFx.transform, "Curse Glow", UIKit.Gen("SoftCircle"), new Vector3(x, gy + 2.5f, 0), 1f, "Backtiles", 7, new Color(sealedColor.r, sealedColor.g, sealedColor.b, 0.55f)).transform.localScale = new Vector3(4f, 7f, 1);
        UIKit.Light("Curse Light", sealedFx.transform, Light2D.LightType.Point, new Vector3(x, gy + 2.5f, 0), sealedColor, 1.1f, 5f, 0.5f);
        var openFx = Child(gate.transform, "Open", new Vector3(x, gy, 0)).gameObject;
        Sprite(openFx.transform, "Beam", UIKit.Gen("SoftCircle"), new Vector3(x, gy + 4f, 0), 1f, "Backtiles", 7, new Color(1f, 0.82f, 0.45f, 0.65f)).transform.localScale = new Vector3(3.2f, 12f, 1);
        UIKit.Light("Gate Light", openFx.transform, Light2D.LightType.Point, new Vector3(x, gy + 2.5f, 0), new Color(1f, 0.85f, 0.55f), 1.8f, 7f, 1f);
        var motes = UIKit.Particles("Rising Light", openFx.transform, new Vector3(x, gy + 0.3f, 0), UIKit.Gen("SoftCircle"), "Grill", 4);
        UIKit.Embers(motes, new Vector3(2.5f, 0.3f, 1f), 18f, new Color(1f, 0.9f, 0.6f, 0.9f), new Color(1f, 0.7f, 0.3f, 0.9f), 0.25f);
        gate.layer = LayerMask.NameToLayer("Ignore Raycast");
        var box = gate.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.offset = new Vector2(0, 3f); box.size = new Vector2(2.6f, 6f);
        var exit = gate.AddComponent<LevelExit>();
        var so = new SerializedObject(exit);
        so.FindProperty("nextSceneName").stringValue = next;
        so.FindProperty("sealedMessage").stringValue = sealedMessage;
        var req = so.FindProperty("requiredDefeated");
        req.arraySize = required?.Length ?? 0;
        for (int i = 0; i < req.arraySize; i++) req.GetArrayElementAtIndex(i).objectReferenceValue = required[i];
        so.FindProperty("sealedVisual").objectReferenceValue = sealedFx;
        so.FindProperty("openVisual").objectReferenceValue = openFx;
        so.ApplyModifiedPropertiesWithoutUndo();
        openFx.SetActive(false);
        return exit;
    }

    public static void BossArena(float x, MonoBehaviour boss, string name, string arrival, string defeatTitle, string defeatSub,
        string cardName = "", string cardTitle = "", string dialogue = "")
    {
        var go = Child(Content, "Boss Trigger: " + name, new Vector3(x, SurfaceY(x) + 4f, 0)).gameObject;
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        var b = go.AddComponent<BoxCollider2D>(); b.isTrigger = true; b.size = new Vector2(6f, 14f);
        var enc = go.AddComponent<BossEncounter>();
        var so = new SerializedObject(enc);
        so.FindProperty("boss").objectReferenceValue = boss;
        so.FindProperty("bossName").stringValue = name;
        so.FindProperty("arrivalMessage").stringValue = arrival;
        so.FindProperty("defeatTitle").stringValue = defeatTitle;
        so.FindProperty("defeatSubtitle").stringValue = defeatSub;
        so.FindProperty("cardName").stringValue = cardName;
        so.FindProperty("cardTitle").stringValue = cardTitle;
        so.FindProperty("dialogue").stringValue = dialogue;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Chapter card and camera sweep when the level starts at its beginning.</summary>
    public static void Chapter(string label, string title, string description, params string[] ghostLines)
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        var go = Child(Content, "Chapter Intro", p.transform.position).gameObject;
        go.AddComponent<ChapterIntro>().Configure(label, title, description, ghostLines);
    }

    /// <summary>Wall torch / brazier: flickering flame, embers and warm light.</summary>
    public static void Torch(float x, float yAboveGround, float scale = 1f)
    {
        float gy = FloorY(x);
        var root = Child(Decor, "Torch", new Vector3(x, gy + yAboveGround, 0));
        Sprite(root, "Sconce", UIKit.Gen("Diamond"), root.position + Vector3.down * 0.25f * scale, 0.5f * scale, "Backtiles", 20, new Color(0.3f, 0.25f, 0.22f)).transform.localScale = new Vector3(0.45f, 0.3f, 1) * scale;
        Sprite(root, "Halo", UIKit.Gen("SoftCircle"), root.position, 2.2f * scale, "Backtiles", 19, new Color(1f, 0.55f, 0.2f, 0.45f));
        var ps = UIKit.Particles("Flame", root, root.position, UIKit.Gen("SoftCircle"), "Backtiles", 21);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f * scale, 0.42f * scale);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.1f, 0.95f), new Color(1f, 0.85f, 0.35f, 0.95f));
        var em = ps.emission; em.rateOverTime = 24f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(0.2f * scale, 0.05f, 0.1f);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f); vel.y = new ParticleSystem.MinMaxCurve(0.9f, 1.6f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.1f));
        var l = UIKit.Light("Torch Light", root, Light2D.LightType.Point, root.position + Vector3.up * 0.3f, new Color(1f, 0.6f, 0.3f), 1.1f, 5.5f * scale, 0.4f);
        l.gameObject.AddComponent<LightFlicker>();
    }

    /// <summary>A column of rising smoke (burned camp, braziers).</summary>
    public static void Smoke(float x, float yAboveGround, float width = 1.2f)
    {
        float gy = FloorY(x);
        var ps = UIKit.Particles("Smoke", Fx, new Vector3(x, gy + yAboveGround, 0), UIKit.Gen("Mist"), "Backtiles", 15);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.25f, 0.22f, 0.22f, 0.35f), new Color(0.4f, 0.36f, 0.34f, 0.25f));
        main.startRotation = new ParticleSystem.MinMaxCurve(-1f, 1f);
        var em = ps.emission; em.rateOverTime = 3.5f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(width, 0.2f, 0.1f);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0.2f, 0.6f); vel.y = new ParticleSystem.MinMaxCurve(0.8f, 1.4f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 2.2f));
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0, 1) });
        col.color = g;
    }

    /// <summary>A layer of distant silhouettes (sprites tinted toward the sky colour) following the camera.</summary>
    public static void Skyline(Sprite[] sprites, float spacing, float y, float scale, float follow, string layer, int order, Color tint)
    {
        var root = Child(Decor, "Skyline " + order);
        root.position = new Vector3(MinX, 0, 0);
        int i = 0;
        for (float x = MinX - 20f; x < MaxX * (1f - follow) + MinX + 60f; x += spacing, i++)
        {
            var sp = sprites[i % sprites.Length];
            var sr = Sprite(root, "Silhouette", sp, new Vector3(x + (i % 3) * spacing * 0.2f, y, 0), scale * (0.85f + (i % 4) * 0.08f), layer, order, tint);
            sr.flipX = i % 2 == 1;
        }
        var pf = root.gameObject.AddComponent<ParallaxFollow>();
        SetF(pf, "followX", follow);
        SetF(pf, "followY", Mathf.Clamp01(follow + 0.05f));
    }

    // ------------------------------------------------------------------ enemies
    public enum Kind { Skeleton, EliteSkeleton, GiantSkeleton, Axe, Ninja, EliteNinja, NinjaMaster }

    static GameObject Prefab(Kind k)
    {
        switch (k)
        {
            case Kind.Axe: return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemies/Prefabs/AxeSkeleton.prefab");
            case Kind.Ninja: case Kind.EliteNinja: case Kind.NinjaMaster: return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemies/Prefabs/YellowNinja.prefab");
            default: return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemies/Prefabs/PatrolSkeleton.prefab");
        }
    }

    public static EnemyCore.Loot L(string id, float chance, int min = 1, int max = 1) => new EnemyCore.Loot { itemId = id, chance = chance, min = min, max = max };

    /// <summary>Spawns and fully configures an enemy of the given archetype on the ground at x.</summary>
    public static GameObject Enemy(Kind kind, float x, string name, Color tint, int hp = 0, int dmg = 0, int xp = 0, bool faceLeft = true, params EnemyCore.Loot[] loot)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(kind), Enemies);
        go.name = name;
        go.layer = LayerMask.NameToLayer("Enemy");
        foreach (Transform t in go.GetComponentsInChildren<Transform>()) if (t.gameObject.layer == 0) t.gameObject.layer = LayerMask.NameToLayer("Enemy");
        var core = go.GetComponent<EnemyCore>(); if (core == null) core = go.AddComponent<EnemyCore>();
        if (go.GetComponent<DamageFlash>() == null) go.AddComponent<DamageFlash>();
        var rb = go.GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        foreach (var sr0 in go.GetComponentsInChildren<SpriteRenderer>()) { sr0.sortingLayerName = "player"; sr0.sortingOrder = -1; sr0.color = tint; }
        bool boss = kind == Kind.GiantSkeleton || kind == Kind.NinjaMaster;

        if (kind == Kind.Skeleton || kind == Kind.EliteSkeleton || kind == Kind.GiantSkeleton)
        {
            Vector2 scale = kind == Kind.Skeleton ? new Vector2(1.15f, 1.2f) : kind == Kind.EliteSkeleton ? new Vector2(1.32f, 1.4f) : new Vector2(1.7f, 1.8f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1);
            var box = go.GetComponent<BoxCollider2D>();
            box.size = new Vector2(1.1f, 3.8f); box.offset = new Vector2(0.4f, -0.05f);
            var gc = go.transform.Find("GroundCheck");
            if (gc != null) gc.localPosition = new Vector3(1.0f, -box.size.y * 0.5f + box.offset.y, 0);
            var ai = go.GetComponent<PatrolSkeletonAI>();
            var so = new SerializedObject(ai);
            so.FindProperty("maxHealth").intValue = hp > 0 ? hp : kind == Kind.Skeleton ? 50 : kind == Kind.EliteSkeleton ? 95 : 220;
            so.ApplyModifiedPropertiesWithoutUndo();
            ai.attackDamage = dmg > 0 ? dmg : kind == Kind.Skeleton ? 10 : kind == Kind.EliteSkeleton ? 14 : 20;
            ai.attackRange = kind == Kind.GiantSkeleton ? 4.3f : kind == Kind.EliteSkeleton ? 3.7f : 3.4f;
            ai.chaseRange = kind == Kind.GiantSkeleton ? 11f : 7.5f;
            ai.chaseSpeed = kind == Kind.Skeleton ? 2.2f : kind == Kind.EliteSkeleton ? 2.0f : 2.5f;
            ai.attackCooldown = kind == Kind.Skeleton ? 1.1f : kind == Kind.EliteSkeleton ? 1.3f : 1.25f;
            ai.attackWindup = kind == Kind.GiantSkeleton ? 0.5f : 0.38f;
            ai.enrageAtHalf = kind == Kind.GiantSkeleton;
            ai.groundLayer = GroundMask;
            ai.groundCheckDistance = 2.5f;
            if (go.GetComponent<FlipColliderSync>() == null) go.AddComponent<FlipColliderSync>();
            float gy = SurfaceY(x);
            go.transform.position = new Vector3(x, gy + box.size.y * scale.y * 0.5f - box.offset.y * scale.y + 0.05f, 0);
            go.GetComponent<SpriteRenderer>().flipX = faceLeft;
            Record(go, box, ai, gc);
        }
        else if (kind == Kind.Axe)
        {
            go.transform.position = new Vector3(x, SurfaceY(x) + 1.5f, 0);
            var eb = go.GetComponent<Enemy_behaviour>();
            eb.maxHealth = hp > 0 ? hp : 40;
            eb.attackDamage = dmg > 0 ? dmg : 9;
            eb.moveSpeed = 2.6f;
            Record(go, eb);
        }
        else
        {
            float s = kind == Kind.Ninja ? 1f : kind == Kind.EliteNinja ? 1.15f : 1.5f;
            go.transform.localScale = new Vector3(s, s, 1);
            var n = go.GetComponent<YellowNinja>();
            n.maxHealth = hp > 0 ? hp : kind == Kind.Ninja ? 32 : kind == Kind.EliteNinja ? 60 : 240;
            n.attackDamage = dmg > 0 ? dmg : kind == Kind.Ninja ? 9 : kind == Kind.EliteNinja ? 13 : 17;
            n.chaseSpeed = kind == Kind.Ninja ? 3.0f : kind == Kind.EliteNinja ? 3.6f : 4.2f;
            n.walkSpeed = 1.6f;
            n.attackCooldown = kind == Kind.Ninja ? 1.0f : kind == Kind.EliteNinja ? 0.8f : 0.65f;
            n.detectionRadius = kind == Kind.NinjaMaster ? 12f : 7f;
            n.attackRadius = 1.3f * s;
            n.attackTriggerDistance = 2.6f * s;
            n.whatIsGround = GroundMask;
            n.whatIsPlayer = LayerMask.GetMask("Player", "Default");
            n.groundCheckDistance = 1.2f;
            var box = go.GetComponent<BoxCollider2D>();
            go.transform.position = new Vector3(x, SurfaceY(x) + box.size.y * s * 0.5f + 0.1f, 0);
            Record(go, n, box);
        }

        int defaultXp = kind switch { Kind.Skeleton => 22, Kind.EliteSkeleton => 45, Kind.GiantSkeleton => 220, Kind.Axe => 18, Kind.Ninja => 20, Kind.EliteNinja => 40, _ => 240 };
        core.Configure(name, xp > 0 ? xp : defaultXp, boss ? 15 : 1, boss ? 25 : 4, boss,
            loot.Length > 0 ? loot : new[] { L("crimson_flask", 0.12f), L("soul_shard", kind == Kind.EliteSkeleton || kind == Kind.EliteNinja ? 0.45f : 0.15f), L("verdant_shard", kind == Kind.EliteSkeleton || kind == Kind.EliteNinja ? 0.3f : 0.1f) });
        EditorUtility.SetDirty(core);
        Record(go, core, rb, go.transform);
        return go;
    }

    static void Record(GameObject go, params Object[] objs)
    {
        if (!PrefabUtility.IsPartOfPrefabInstance(go)) return;
        foreach (var o in objs) if (o != null) PrefabUtility.RecordPrefabInstancePropertyModifications(o);
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>()) PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
    }

    // ------------------------------------------------------------------ decor
    public static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Vector3 pos, float scale, string layer, int order, Color tint)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = tint;
        return sr;
    }

    /// <summary>Places a prop standing on the ground at x (bottom of sprite on the surface).</summary>
    public static SpriteRenderer Prop(Sprite s, float x, float scale = 1f, string layer = "Backtiles", int order = 4, bool flip = false, float sink = 0.1f, Color? tint = null)
    {
        if (s == null) { Debug.LogWarning("Missing prop sprite"); return null; }
        float gy = SurfaceY(x);
        float bottom = (s.bounds.center.y - s.bounds.extents.y) * scale;
        var sr = Sprite(Decor, s.name, s, new Vector3(x, gy - bottom - sink, 0), scale, layer, order, tint ?? Color.white);
        sr.flipX = flip;
        return sr;
    }

    // ------------------------------------------------------------------ serialized helpers
    public static void Set(Object c, string prop, Object v) { var so = new SerializedObject(c); so.FindProperty(prop).objectReferenceValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    public static void SetF(Object c, string prop, float v) { var so = new SerializedObject(c); so.FindProperty(prop).floatValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    public static void SetI(Object c, string prop, int v) { var so = new SerializedObject(c); so.FindProperty(prop).intValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    public static void SetS(Object c, string prop, string v) { var so = new SerializedObject(c); so.FindProperty(prop).stringValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
}

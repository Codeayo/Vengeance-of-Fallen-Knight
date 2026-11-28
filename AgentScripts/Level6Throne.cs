using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using K = LevelKit;
using C5 = Level5Castle;

/// <summary>Chapter VI - The Usurper's Throne: the final confrontation with Lord Malvric.</summary>
public static class Level6Throne
{
    public static string Build()
    {
        var scene = K.Begin("Assets/Scenes/ThroneRoom.unity", "Chapter VI - The Usurper's Throne");
        K.RetireLegacy("Bush", "Ground", "Fence", "Hazards", "Background");
        Color gold = new Color(1f, 0.88f, 0.75f);
        var st = C5.CastleStyle(new Color(1f, 0.85f, 0.85f));

        K.Ground(-2, 40, 0f, st);
        K.Ground(40, 56, 1.5f, st);
        K.Ground(56, 92, 3f, st);
        K.Ledge(66, 72, 6.4f, st);
        K.Ground(92, 110, 3f, st);
        K.Ground(110, 176, 3f, st);   // the throne room floor
        K.Ground(176, 190, 4.5f, st); // the dais
        K.FinishTerrain(st);
        K.Ledge(120, 127, 7f, st); K.Ledge(159, 166, 7f, st); // arena perches

        K.Backdrop(K.SpriteFile("Assets/Art/Castle-Hall/Tiles/Background.png"), 0.85f, 40f, 12f, "Background", 0, new Color(0.65f, 0.45f, 0.5f));
        C5.HallDecor(4f, 108f, 14f, new Color(0.85f, 0.75f, 0.8f));
        C5.HallDecor(112f, 188f, 12f, gold);
        K.Sprite(K.Decor, "Throne Drape", C5.C(2), new Vector3(183f, K.FloorY(183f) + 7f, 0), 2.2f, "backmid", 6, new Color(0.9f, 0.3f, 0.35f));
        K.Prop(C5.C(0), 179f, 1.6f, "Backtiles", 5);
        K.Prop(C5.C(0), 188f, 1.6f, "Backtiles", 5, true);

        K.Player(3f);
        K.Hud();
        K.Camera(8.5f, 30f, new Color(0.06f, 0.02f, 0.03f));

        K.Chapter("CHAPTER VI", "The Usurper's Throne", "Here it ends.",
            "The usurper waits upon your throne, Aldric.", "He wears your king's crown and your name for a traitor. End it.");
        K.Note(16f, "Your Own Portrait",
            "Your face, slashed through with a blade. Beneath it someone has written:\n\n\"The traitor captain.\"\n\n" +
            "Malvric has told the realm you betrayed the king. Only you know the truth.", C5.P(1), 1.1f);
        K.Shrine("Shrine of the Last Vigil", 98f);

        Color guard = new Color(0.85f, 0.55f, 0.55f), palace = new Color(0.9f, 0.8f, 1f);
        K.Enemy(K.Kind.EliteSkeleton, 34f, "Throne Guard", guard);
        K.Enemy(K.Kind.EliteNinja, 62f, "Malvric's Shadow", palace);
        K.Enemy(K.Kind.EliteSkeleton, 84f, "Throne Guard", guard, 0, 0, 0, false);

        // Summon templates for phase 3 (configured, inactive copies)
        var t1 = K.Enemy(K.Kind.EliteSkeleton, 125f, "Risen Royal Guard", guard); t1.SetActive(false);
        var t2 = K.Enemy(K.Kind.EliteSkeleton, 165f, "Risen Royal Guard", guard); t2.SetActive(false);

        var malvric = BuildMalvric(150f);
        var pts = new[] { K.Child(K.Content, "Summon West", new Vector3(118f, K.FloorY(118f) + 3f, 0)), K.Child(K.Content, "Summon East", new Vector3(172f, K.FloorY(172f) + 3f, 0)) };
        var ai = malvric.GetComponent<BossKnightAI>();
        var so = new SerializedObject(ai);
        var sp = so.FindProperty("summonPrefabs"); sp.arraySize = 2; sp.GetArrayElementAtIndex(0).objectReferenceValue = t1; sp.GetArrayElementAtIndex(1).objectReferenceValue = t2;
        var pp = so.FindProperty("summonPoints"); pp.arraySize = 2; pp.GetArrayElementAtIndex(0).objectReferenceValue = pts[0]; pp.GetArrayElementAtIndex(1).objectReferenceValue = pts[1];
        so.ApplyModifiedPropertiesWithoutUndo();
        K.BossArena(114f, ai, "Lord Malvric, the Usurper", "\"Sir Aldric. I buried you myself.\"",
            "The Usurper Is Dead", "Vengeance is done. The crown is free.", "LORD MALVRIC", "THE USURPER KING", "Sir Aldric. I buried you myself.");

        // Epilogue after Malvric falls.
        var epi = K.Child(K.Content, "Epilogue").gameObject.AddComponent<EpilogueCinematic>();
        var throneMark = K.Child(K.Content, "Throne", new Vector3(183f, K.FloorY(183f), 0));
        K.Set(epi, "finalBoss", malvric);
        K.Set(epi, "throne", throneMark);

        K.Pickup("crimson_flask", 50f);
        K.Pickup("verdant_tonic", 100f, 1.2f);
        K.Pickup("prism_relic", 69f, 1.4f, 1, "relic_throne");
        K.Coins(4, 30, 1.3f, 6);

        // The throne room: torches between the pillars, braziers flanking the dais, red carpet runner.
        for (float x = 8f; x < 176f; x += 16f) K.Torch(x, 3.6f, 1.05f);
        K.Torch(177f, 1.4f, 1.5f); K.Torch(189f, 1.4f, 1.5f);
        foreach (float x in new[] { 30f, 78f, 150f }) K.Prop(C5.P(10), x, 1.1f, "Backtiles", 7);
        foreach (float x in new[] { 22f, 96f }) K.Prop(C5.P(5), x, 1.1f, "Backtiles", 6);
        K.Tiled(K.Decor, "Royal Carpet", C5.C(15), new Vector3(150f, K.FloorY(150f) + 0.08f, 0), new Vector2(60f, 0.5f), "Ground", 6, new Color(0.9f, 0.25f, 0.3f));

        K.Atmosphere(new Color(1f, 0.75f, 0.72f), 0.68f, new Color(0.7f, 0.4f, 0.45f), 0.08f,
            new K.VolumeLook("ThronePost", 0.45f, 1f, 16f, -6f, new Color(1f, 0.92f, 0.9f)),
            "Embers", new Color(1f, 0.45f, 0.3f, 0.7f), new Color(1f, 0.8f, 0.5f, 0.7f));

        // Victory: the crown shard on the throne, then the walk to the credits.
        K.Pickup("frozen_crown", 182f, 1.3f, 1, "crown_shard");
        K.Exit(186f, "Credits", new[] { malvric }, new Color(0.9f, 0.15f, 0.2f), "Malvric still lives");
        K.Save(scene);
        return K.Log.ToString();
    }

    static GameObject BuildMalvric(float x)
    {
        var go = new GameObject("Lord Malvric");
        go.transform.SetParent(K.Enemies, false);
        go.layer = LayerMask.NameToLayer("Enemy");
        go.transform.localScale = new Vector3(1.15f, 1.2f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Players/SoulslikeKnight/SoulslikeKnight_SpriteSheets/SoulslikeKnight_Idle.png").OfType<Sprite>().OrderBy(s => s.name.Length).ThenBy(s => s.name).First();
        sr.sortingLayerName = "player";
        sr.flipX = true;
        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Players/SoulslikeKnight/SoulslikeKnight_SpriteSheets/Player.controller");
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f; rb.freezeRotation = true; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(2.0f, 3.25f); box.offset = new Vector2(0.92f, 0f);
        box.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Art/Generated/PlayerNoFriction.physicsMaterial2D");
        go.AddComponent<FlipColliderSync>();
        var ai = go.AddComponent<BossKnightAI>();
        var core = go.AddComponent<EnemyCore>();
        core.Configure("Lord Malvric", 600, 40, 60, true, K.L("ember_of_vengeance", 1f, 2, 2));
        go.transform.position = new Vector3(x, K.FloorY(x) + 1.95f * 1.2f / 1.2f + 0.1f, 0);
        UIKit.Light("Malvric Aura", go.transform, Light2D.LightType.Point, go.transform.position + Vector3.up, new Color(1f, 0.2f, 0.25f), 1f, 5f, 0.5f);
        return go;
    }
}

using UnityEngine;
using K = LevelKit;

/// <summary>Chapter V - The Fallen Castle: through the postern gate and the halls Aldric once guarded.</summary>
public static class Level5Castle
{
    public const string Tex = "Assets/Art/Castle-Hall/Tiles/Tiles.png";
    public const string PropsTex = "Assets/Art/Castle-Hall/Tiles/Props.png";
    public static Sprite C(int i) => K.SpriteAt(Tex, "Tiles_" + i);
    public static Sprite P(int i) => K.SpriteAt(PropsTex, "Props_" + i);

    public static K.Style CastleStyle(Color tint) => new K.Style
    {
        top = C(15), surfaceInset = 0.12f, topTint = tint,
        body = C(12), bodyColor = new Color(0.08f, 0.06f, 0.08f),
        platform = C(21), platformInset = 0.1f,
    };

    /// <summary>Hall dressing: tall windows, banners and pillars along the back wall, chandeliers overhead.</summary>
    public static void HallDecor(float x0, float x1, float spacing, Color tint)
    {
        int i = 0;
        for (float x = x0; x < x1; x += spacing, i++)
        {
            float gy = K.FloorY(x);
            K.Sprite(K.Decor, "Column", C(10), new Vector3(x, gy + 3.2f, 0), 1.6f, "backmid", 3, tint);
            K.Sprite(K.Decor, "Window", C(1), new Vector3(x + spacing * 0.5f, gy + 5.4f, 0), 1.4f, "backmid", 2, tint);
            if (i % 2 == 0) K.Sprite(K.Decor, "Banner", C(13), new Vector3(x + spacing * 0.25f, gy + 5.6f, 0), 1.2f, "backmid", 4, tint);
            if (i % 3 == 1) K.Sprite(K.Decor, "Chandelier", P(2 + i % 3), new Vector3(x + spacing * 0.5f, gy + 8.6f, 0), 1.6f, "Grill", 2, Color.white);
            if (i % 3 == 1) K.PointLight(x + spacing * 0.5f, 7.5f, new Color(1f, 0.75f, 0.45f), 1.2f, 8f);
        }
    }

    public static string Build()
    {
        var scene = K.Begin("Assets/Castle.unity", "Chapter V - The Fallen Castle");
        K.RetireLegacy("Bush", "Ground", "Fence", "Hazards", "Background");
        Color stone = new Color(0.78f, 0.72f, 0.8f);
        var st = CastleStyle(Color.white);

        K.Ground(-2, 30, 0f, st);
        K.Ground(30, 44, 2f, st);
        K.Ground(44, 70, 2f, st);
        K.Ground(75, 96, 2f, st);
        K.Ledge(98, 103, 3.6f, st);
        K.Ground(106, 132, 4f, st);
        K.Ledge(112, 118, 7.4f, st); K.Ledge(121, 127, 10.6f, st); K.Ledge(130, 140, 13.6f, st); // vault climb
        K.Ground(132, 160, 6f, st);
        K.Ground(160, 176, 8f, st);
        K.Ground(181, 210, 8f, st);
        K.Ground(210, 228, 10f, st);
        K.Ground(228, 290, 10f, st);
        K.FinishTerrain(st);

        K.Backdrop(K.SpriteFile("Assets/Art/Castle-Hall/Tiles/Background.png"), 0.85f, 40f, 12f, "Background", 0, new Color(0.55f, 0.48f, 0.62f));
        HallDecor(4f, 286f, 16f, stone);

        K.Player(3f);
        K.Hud();
        K.Camera(8.5f, 34f, new Color(0.05f, 0.04f, 0.07f));

        K.Chapter("CHAPTER V", "The Fallen Castle", "The halls Aldric once guarded. Familiar stone, and everything in it ruined.");
        var note = P(9);
        K.Note(14f, "Steward's Ledger",
            "By order of King Malvric, the old guard's names are struck from the rolls.\n\n" +
            "The vault key hangs in the high gallery above the barracks. The old king's sword lies within. None may touch it.", note, 1.1f);
        K.Note(150f, "Prisoner's Scrawl",
            "Aldric would never have let this happen. They say Malvric struck him down in the night with paid knives " +
            "because he could not beat him in the yard.\n\nIf you read this, Captain, finish it.", note, 1.1f);
        K.Note(286f, "To the Throne",
            "The great doors are open. Beyond them the throne room, and the man who wears your king's crown.", note, 1.1f);

        K.Shrine("Shrine of the Postern", 22f);
        K.Shrine("Shrine of the Barracks", 140f);
        K.Shrine("Shrine of the Great Hall", 232f);

        Color guard = new Color(0.85f, 0.6f, 0.6f), black = new Color(0.55f, 0.45f, 0.5f), palace = new Color(0.85f, 0.8f, 1f);
        K.Enemy(K.Kind.Skeleton, 38f, "Dungeon Risen", Color.white);
        K.Enemy(K.Kind.Ninja, 58f, "Palace Knife", palace);
        K.Enemy(K.Kind.EliteSkeleton, 86f, "Royal Guard", guard);
        K.Enemy(K.Kind.Axe, 120f, "Dungeon Ghoul", Color.white);
        K.Enemy(K.Kind.EliteNinja, 150f, "Palace Knife", palace);
        K.Enemy(K.Kind.EliteSkeleton, 170f, "Royal Guard", guard, 0, 0, 0, false);
        K.Enemy(K.Kind.Ninja, 192f, "Palace Knife", palace);
        K.Enemy(K.Kind.EliteSkeleton, 204f, "Royal Guard", guard);
        K.Enemy(K.Kind.EliteNinja, 222f, "Palace Knife", palace);
        var captain = K.Enemy(K.Kind.GiantSkeleton, 270f, "Sir Dravek, Captain of the Black Guard", black, 420, 26, 420, true,
            K.L("royal_guard_plate", 1f), K.L("verdant_shard", 1f, 2, 3), K.L("soul_shard", 1f, 2, 3), K.L("crimson_flask", 1f, 2, 2));
        var g1 = K.Enemy(K.Kind.EliteSkeleton, 258f, "Black Guard", black);
        K.BossArena(242f, captain.GetComponent<PatrolSkeletonAI>(), "Sir Dravek, Captain of the Black Guard",
            "\"Captain? No. I am captain now.\"", "Dravek Falls", "The way to the throne lies open",
            "SIR DRAVEK", "CAPTAIN OF THE BLACK GUARD", "Captain? No. I am captain now.");

        // The vault: royal key from Dravek opens it on the way back, or via the climb if found earlier
        K.Gate(134f, "royal_key", "castle_vault", K.SpriteAt("Assets/Art/Graveyard/Tiles.png", "Tiles_234"));
        K.Pickup("royal_key", 124f, 1.3f, 1, "castle_royal_key");
        K.Pickup("kingsbane", 137f, 1.4f, 1, "castle_kingsbane");
        K.Pickup("prism_relic", 139f, 1.4f, 1, "relic_castle");
        K.Pickup("crimson_flask", 66f);
        K.Pickup("violet_draught", 101f, 1.2f);
        K.Pickup("heart_vessel", 116f, 1.4f, 1, "castle_heart");
        K.Pickup("ember_of_vengeance", 186f);
        K.Coins(4, 26, 1.3f, 6); K.Coins(108, 128, 1.3f, 6); K.Coins(184, 208, 1.3f, 7);

        foreach (float x in new[] { 10f, 80f, 156f, 214f, 284f }) K.Prop(P(5), x, 1.1f, "Backtiles", 6);     // cabinets
        foreach (float x in new[] { 34f, 126f, 198f }) K.Prop(P(10), x, 1.1f, "Backtiles", 6);               // benches
        foreach (float x in new[] { 52f, 94f, 176f, 250f }) K.Prop(P(7), x, 0.8f, "Backtiles", 7);           // skulls
        foreach (float x in new[] { 28f, 146f, 236f }) K.Prop(C(0), x, 1.3f, "Backtiles", 3);                // statues

        // Lived-in ruin: wall torches, books, bottles, sacks, fallen banners and chains.
        for (float x = 12f; x < 285f; x += 21f) K.Torch(x, 3.6f, 1f);
        foreach (float x in new[] { 18f, 62f, 104f, 165f, 208f, 262f }) K.Prop(P(13 + (int)x % 4), x, 1f, "Backtiles", 8);    // books
        foreach (float x in new[] { 46f, 116f, 188f, 244f }) K.Prop(P(17 + (int)x % 4), x, 0.9f, "Backtiles", 8);             // bottles
        foreach (float x in new[] { 70f, 152f, 228f }) K.Prop(P(11), x, 1f, "Backtiles", 7);                                    // sacks
        foreach (float x in new[] { 40f, 134f, 220f }) K.Prop(C(13), x, 1f, "Backtiles", 2, false, 0.6f, new Color(0.6f, 0.45f, 0.55f)); // fallen banners
        foreach (float x in new[] { 98f, 180f, 275f }) K.Sprite(K.Decor, "Chain", P(2), new Vector3(x, K.FloorY(x) + 6.5f, 0), 1.2f, "Backtiles", 3, new Color(0.7f, 0.7f, 0.75f));

        K.Atmosphere(new Color(0.8f, 0.72f, 0.95f), 0.7f, new Color(0.6f, 0.5f, 0.75f), 0.08f,
            new K.VolumeLook("CastlePost", 0.42f, 0.8f, 14f, -10f, new Color(0.95f, 0.9f, 1f)),
            "Dust", new Color(1f, 0.85f, 0.6f, 0.5f), new Color(0.9f, 0.8f, 1f, 0.5f));

        K.Exit(287f, "ThroneRoom", new[] { captain, g1 }, new Color(0.8f, 0.2f, 0.3f), "The Black Guard bars the throne room doors");
        K.Save(scene);
        return K.Log.ToString();
    }
}

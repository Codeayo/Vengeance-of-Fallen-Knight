using UnityEngine;
using K = LevelKit;

/// <summary>Chapter IV - The Frozen Taiga: the mountain pass to the castle, held by Malvric's frost watch.</summary>
public static class Level4Taiga
{
    const string Tex = "Assets/Art/Final Taiga Forest/Tiles.png";
    static Sprite T(int i) => K.SpriteAt(Tex, "Tiles_" + i);
    static readonly Color Frost = new Color(0.82f, 0.9f, 1f);

    public static string Build()
    {
        var scene = K.Begin("Assets/Scenes/Taiga.unity", "Chapter IV - The Frozen Taiga");
        K.RetireLegacy("Bush", "Ground", "Fence", "Hazards", "Background");

        var st = new K.Style
        {
            top = T(0), surfaceInset = 0.35f, topTint = Frost,
            body = null, bodyColor = new Color(0.1f, 0.12f, 0.12f),
            platform = T(0), platformInset = 0.35f, platformHeight = 1.2f,
        };

        K.Ground(-2, 22, 0f, st);
        K.Ground(22, 36, 2f, st);
        K.Ground(36, 52, 4f, st);
        K.Ground(57, 76, 4f, st);
        K.Ledge(79, 84, 5.6f, st);
        K.Ground(87, 112, 3f, st);
        K.Ledge(95, 101, 7f, st); K.Ledge(103, 108, 10.2f, st);       // frost-key climb
        K.Ground(112, 130, 6f, st);
        K.Ground(130, 152, 8f, st);
        K.Ground(157, 166, 8f, st); K.Ledge(168, 172, 9.4f, st);
        K.Ground(175, 205, 10f, st);
        K.Ground(205, 222, 12f, st);
        K.Ground(222, 240, 10f, st);
        K.Ground(245, 300, 11f, st);
        K.FinishTerrain(st);
        K.Ledge(108, 116, 10.2f, st); // vault ledge behind the frozen gate

        string bgDir = "Assets/Art/Final Taiga Forest/";
        K.Backdrop(K.SpriteFile(bgDir + "Background_0.png"), 0.94f, 46f, 14f, "Background", 0, new Color(0.75f, 0.82f, 0.95f));
        K.Backdrop(K.SpriteFile(bgDir + "Background_1.png"), 0.8f, 40f, 13f, "Background", 1, new Color(0.7f, 0.78f, 0.92f));
        K.Backdrop(K.SpriteFile(bgDir + "Background_3.png"), 0.62f, 34f, 11f, "backmid", 2, new Color(0.7f, 0.78f, 0.9f));
        K.Backdrop(K.SpriteFile(bgDir + "tree.png"), 0.4f, 30f, 10f, "Backnear", 3, new Color(0.65f, 0.72f, 0.85f));

        K.Player(3f);
        K.Hud();
        K.Camera(8.5f, 36f);

        K.Banner(3.8f, "The Frozen Taiga", "The last road to the castle");
        var note = K.SpriteAt("Assets/Art/Castle-Hall/Tiles/Props.png", "Props_1");
        K.Note(12f, "Smith's Letter",
            "We forged for the old king and we will not forge for a usurper. The frost watch can starve us if they like.\n\n" +
            "Our last blade, Frostbrand, we gave to the Warden to keep it from Malvric. Now the Warden serves him.", note, 1.1f);
        K.Note(118f, "Frozen Watchman",
            "His hand is still on his spear. Scratched into the ice beside him:\n\n" +
            "\"The key to the smiths' vault hangs where only a climber can reach it.\"", note, 1.1f);
        K.Note(296f, "The Postern Gate",
            "Ahead, the black towers of the castle. The hermit's map was true: the postern gate stands unguarded.\n\n" +
            "Malvric is waiting on your throne.", note, 1.1f);

        K.Shrine("Shrine of the Pass", 64f);
        K.Shrine("Shrine of the Watch", 178f);
        K.Shrine("Shrine before the Warden", 247f);

        Color frozen = new Color(0.7f, 0.85f, 1f), frostElite = new Color(0.55f, 0.75f, 1f), assassin = new Color(0.75f, 0.85f, 1f);
        K.Enemy(K.Kind.Ninja, 30f, "Frost Assassin", assassin);
        K.Enemy(K.Kind.Skeleton, 46f, "Frozen Dead", frozen);
        K.Enemy(K.Kind.EliteNinja, 70f, "Frost Hunter", frostElite);
        K.Enemy(K.Kind.Skeleton, 90f, "Frozen Dead", frozen, 0, 0, 0, false);
        K.Enemy(K.Kind.EliteSkeleton, 122f, "Frost Watchman", frostElite);
        K.Enemy(K.Kind.Ninja, 140f, "Frost Assassin", assassin);
        K.Enemy(K.Kind.EliteNinja, 148f, "Frost Hunter", frostElite);
        K.Enemy(K.Kind.Axe, 190f, "Frozen Ghoul", frozen);
        K.Enemy(K.Kind.EliteSkeleton, 214f, "Frost Watchman", frostElite);
        K.Enemy(K.Kind.Ninja, 230f, "Frost Assassin", assassin);
        var warden = K.Enemy(K.Kind.GiantSkeleton, 282f, "The Frost Warden", new Color(0.55f, 0.75f, 1f), 360, 24, 360, true,
            K.L("frostbrand", 1f), K.L("frost_amulet", 1f), K.L("soul_shard", 1f, 2, 3), K.L("crimson_flask", 1f, 2, 2));
        var hunter = K.Enemy(K.Kind.EliteNinja, 270f, "Warden's Hunter", frostElite);
        K.BossArena(254f, warden.GetComponent<PatrolSkeletonAI>(), "The Frost Warden", "Ice cracks as the Warden lifts Frostbrand's sheath",
            "The Warden Falls", "Frostbrand is yours");

        K.Pickup("frost_key", 105.5f, 1.3f, 1, "taiga_frost_key");
        K.Gate(107.5f, "frost_key", "taiga_vault", K.SpriteAt("Assets/Art/Graveyard/Tiles.png", "Tiles_234"));
        K.Pickup("frostguard_mail", 112f, 1.3f, 1, "taiga_frostguard");
        K.Pickup("prism_relic", 114.5f, 1.3f, 1, "relic_taiga");
        K.Pickup("crimson_flask", 59f);
        K.Pickup("verdant_shard", 170f, 1.2f, 1, "taiga_ledge_shard");
        K.Pickup("heart_vessel", 218f, 1.3f, 1, "taiga_heart");
        K.Coins(24, 34, 1.3f, 5); K.Coins(132, 150, 1.3f, 6); K.Coins(177, 200, 1.3f, 7);

        // Decor: great stumps, bushes, frozen trees
        foreach (float x in new[] { 16f, 48f, 100f, 136f, 196f, 236f, 290f }) K.Prop(T(1), x, 1f, "Backtiles", 4, false, 0.1f, Frost);
        float[] stumps = { 6f, 60f, 124f, 185f, 228f, 262f };
        for (int i = 0; i < stumps.Length; i++) K.Prop(T(i % 2 == 0 ? 3 : 5), stumps[i], 0.9f, "Backtiles", 2, i % 2 == 1, 0.3f, Frost);
        foreach (float x in new[] { 40f, 116f, 210f, 278f }) K.PointLight(x, 3f, new Color(0.6f, 0.8f, 1f), 0.7f, 7f);

        K.Atmosphere(new Color(0.75f, 0.85f, 1f), 0.85f, new Color(0.85f, 0.92f, 1f), 0.14f,
            new K.VolumeLook("TaigaPost", 0.35f, 0.9f, 12f, -18f, new Color(0.9f, 0.95f, 1.05f)),
            "Snow", new Color(1f, 1f, 1f, 0.9f), new Color(0.8f, 0.9f, 1f, 0.9f));

        K.Exit(296f, "Castle", new[] { warden, hunter }, new Color(0.4f, 0.6f, 1f), "The Warden's ice seals the road");
        K.Save(scene);
        return K.Log.ToString();
    }
}

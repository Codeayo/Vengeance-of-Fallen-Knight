using System.Linq;
using UnityEditor;
using UnityEngine;
using K = LevelKit;

/// <summary>Chapter II - The Burned Camp (Woodcamp): the assassins' camp where Aldric's murder was planned.</summary>
public static class Level2Woodcamp
{
    const string Tex = "Assets/Art/Wood-Camp/Tiles/Assets.png";
    static Sprite A(int i) => K.SpriteAt(Tex, "Assets_" + i);
    static Sprite T(int i) => K.SpriteAt("Assets/Art/Wood-Camp/Tiles/Trees.png", "Trees_" + i);

    public static string Build()
    {
        var scene = K.Begin("Assets/Scenes/Woodcamp.unity", "Chapter II - The Burned Camp");
        K.RetireLegacy("Ground", "Hazards", "Background", "Prop", "Bush");

        var st = new K.Style
        {
            top = A(19), surfaceInset = 0.32f,
            body = A(17), bodyColor = new Color(0.18f, 0.12f, 0.09f),
            platform = A(21), platformInset = 0.22f,
        };

        // --- Terrain (surface heights). The opening keeps the original camp island heights.
        K.Ground(-2, 13, -0.7f, st); K.Ground(13, 24, 0.3f, st); K.Ground(24, 36, -0.7f, st); K.Ground(36, 47, 1.3f, st);
        K.Ground(47, 59, -0.7f, st);
        K.Ground(62.5f, 71, 0.3f, st); K.Ground(71, 100, -0.7f, st);
        K.Ledge(102, 106.5f, 0.6f, st);                                  // ravine stepping ledge
        K.Ground(109, 124, 0.3f, st); K.Ground(124, 142, 2.3f, st);
        K.Ledge(129, 135, 5.6f, st); K.Ledge(137, 141, 8.4f, st);         // optional climb: treasure
        K.Ground(142, 170, 2.3f, st); K.Ground(170, 182, 4.3f, st);
        K.Ground(187.5f, 216, 3.3f, st);
        K.Ledge(204, 209, 6.6f, st); K.Ledge(211, 215, 9.6f, st); K.Ledge(218, 224, 12.4f, st); // secret route up
        K.Ground(216, 240, 3.3f, st); K.Ground(240, 251, 5.3f, st);
        K.Ground(256.5f, 302, 4.3f, st);
        K.FinishTerrain(st);
        K.Ledge(224, 232, 12.4f, st); // treasure ledge behind the locked gate

        // --- Backdrop
        for (int i = 1; i <= 5; i++)
        {
            var bg = K.SpriteFile($"Assets/Art/Wood-Camp/Background/Background-{i}.png");
            float follow = 0.95f - i * 0.12f;
            K.Backdrop(bg, follow, 44f, 12f, i <= 2 ? "Background" : "backmid", i, new Color(0.72f, 0.66f, 0.6f));
        }

        K.Player(3f);
        K.Hud();
        K.Camera(8.5f, 34f);

        // --- Story & structure
        K.Banner(4f, "The Burned Camp", "Where Malvric's assassins wait for their pay");
        var note = K.SpriteAt("Assets/Art/Castle-Hall/Tiles/Props.png", "Props_1");
        K.Note(11f, "Charred Orders",
            "\"The Knight-Captain is dead and buried. Burn the camp's records and hold the forest road.\n\n" +
            "No one is to speak his name again. - M.\"\n\nThe seal is Lord Malvric's.", note, 1.1f);
        K.Note(150f, "Mercenary's Ledger",
            "Paid in full: one Knight-Captain, Sir Aldric.\nPaid in full: the old king's guard, all eleven.\n\n" +
            "Owed: the Master's share. He keeps the contract on him, says it's his insurance.", note, 1.1f);
        K.Note(298f, "Torn Map",
            "The road east runs through the Mushroom Grove. The hermits there still curse Malvric's name, " +
            "and the rot he brought to their wood.", note, 1.1f);

        K.Shrine("Shrine of the Camp Road", 84f);
        K.Shrine("Shrine of the Stockade", 197f);
        K.Shrine("Shrine before the Master", 262f);

        // --- Enemies: ninja patrols, then mixed squads, then the Master
        Color ninja = Color.white, elite = new Color(0.75f, 0.75f, 1f), ghoul = new Color(0.8f, 0.9f, 0.8f);
        K.Enemy(K.Kind.Ninja, 44f, "Camp Lookout", ninja);
        K.Enemy(K.Kind.Ninja, 92f, "Camp Lookout", ninja);
        K.Enemy(K.Kind.Axe, 118f, "Camp Ghoul", ghoul);
        K.Enemy(K.Kind.Ninja, 150f, "Assassin", ninja);
        K.Enemy(K.Kind.Ninja, 163f, "Assassin", ninja);
        K.Enemy(K.Kind.EliteNinja, 178f, "Blade of the Camp", elite);
        K.Enemy(K.Kind.Axe, 207f, "Camp Ghoul", ghoul);
        K.Enemy(K.Kind.Ninja, 236f, "Assassin", ninja);
        K.Enemy(K.Kind.EliteNinja, 246f, "Blade of the Camp", elite);
        var guardA = K.Enemy(K.Kind.Ninja, 272f, "Master's Guard", ninja);
        var guardB = K.Enemy(K.Kind.Ninja, 294f, "Master's Guard", ninja);
        var master = K.Enemy(K.Kind.NinjaMaster, 285f, "Kage, Malvric's Blade", new Color(1f, 0.55f, 0.5f), 260, 16, 260, true,
            K.L("assassins_fang", 1f), K.L("blood_contract", 1f), K.L("heart_vessel", 1f), K.L("crimson_flask", 1f, 1, 2));
        K.BossArena(268f, master.GetComponent<YellowNinja>(), "Kage, Malvric's Blade", "\"You should have stayed buried, Captain.\"",
            "Kage Is Slain", "The Blood Contract bears Malvric's seal");

        // --- Rewards & secrets
        K.Pickup("crimson_flask", 66f);
        K.Pickup("soul_shard", 139f, 1.2f, 1, "wood_shard_climb"); // optional climb
        K.Pickup("ranger_leathers", 132f, 1.3f, 1, "wood_leathers");
        K.Pickup("verdant_shard", 233f, 1.2f, 1, "wood_secret_shard");
        K.Pickup("prism_relic", 229f, 1.4f, 1, "relic_woodcamp");
        K.Pickup("verdant_tonic", 190f);
        K.Gate(222.5f, "crypt_key", "wood_stockade", FenceBar());
        K.Coins(26, 34, 1.4f, 5); K.Coins(110, 122, 1.4f, 5); K.Coins(172, 181, 1.4f, 4); K.Coins(241, 250, 1.4f, 4);

        // --- Decor: the camp
        K.Prop(A(1), 9f, 1.1f, "Backtiles", 3);          // watchtower
        K.Prop(A(1), 66f, 1.1f, "Backtiles", 3, true);
        K.Prop(A(1), 233f, 1.1f, "Backtiles", 3);
        foreach (float x in new[] { 18f, 77f, 147f, 213f, 279f }) K.Prop(A(2), x, 0.9f, "Backtiles", 6);       // crates
        foreach (float x in new[] { 20f, 79f, 160f, 237f }) K.Prop(A(8), x, 0.9f, "Backtiles", 6);             // barrels
        foreach (float x in new[] { 52f, 116f, 192f, 258f, 300f }) K.Prop(A(22), x, 1f, "Backtiles", 5);       // palisades
        foreach (float x in new[] { 30f, 98f, 167f, 244f }) K.Prop(A(6), x, 0.9f, "Backtiles", 4);             // stumps
        K.Prop(A(13), 288f, 1f, "Backtiles", 6);                                                                  // the Master's chest
        for (int i = 0; i < 9; i++)
        {
            float x = -6f + i * 36f;
            K.Prop(T(i % 8), x, 1f, "Backnear", i, i % 2 == 0, 0.3f, new Color(0.8f, 0.78f, 0.75f));
        }
        Campfire(37f); Campfire(156f); Campfire(279f);

        K.Atmosphere(new Color(1f, 0.82f, 0.7f), 0.82f, new Color(0.75f, 0.65f, 0.6f), 0.08f,
            new K.VolumeLook("WoodcampPost", 0.33f, 0.7f, 8f, -12f, new Color(1f, 0.93f, 0.85f)),
            "Embers", new Color(1f, 0.55f, 0.2f, 0.85f), new Color(1f, 0.8f, 0.4f, 0.85f));

        // Exit
        K.Exit(298.5f, "Grove", new[] { master, guardA, guardB }, new Color(1f, 0.3f, 0.2f), "Kage's guards bar the road east");
        K.Save(scene);
        return K.Log.ToString();
    }

    static Sprite FenceBar() => K.SpriteAt("Assets/Art/Graveyard/Tiles.png", "Tiles_234");

    static void Campfire(float x)
    {
        float gy = K.SurfaceY(x);
        K.Prop(A(9), x, 0.6f, "Backtiles", 7);
        var ps = UIKit.Particles("Campfire", K.Fx, new Vector3(x, gy + 0.3f, 0), UIKit.Gen("SoftCircle"), "Grill", 5);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.1f, 0.9f), new Color(1f, 0.85f, 0.35f, 0.9f));
        var em = ps.emission; em.rateOverTime = 30f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(0.7f, 0.1f, 0.1f);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f); vel.y = new ParticleSystem.MinMaxCurve(1.4f, 2.4f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.1f));
        K.PointLight(x, 1.2f, new Color(1f, 0.55f, 0.25f), 1.3f, 7f);
    }
}

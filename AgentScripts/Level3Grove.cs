using UnityEngine;
using K = LevelKit;

/// <summary>Chapter III - The Rotting Grove: Malvric's sorcery has turned the hermits' wood into a graveyard of its own.</summary>
public static class Level3Grove
{
    const string Tex = "Assets/Art/Mushroom_Grove/Assets/Tiles/Tile_set.png";
    static Sprite G(int i) => K.SpriteAt(Tex, "Tile_set_" + i);

    public static string Build()
    {
        var scene = K.Begin("Assets/Scenes/Grove.unity", "Chapter III - The Rotting Grove");
        K.RetireLegacy("Bush", "Ground", "Fence", "Hazards", "Background");

        var st = new K.Style
        {
            top = G(21), surfaceInset = 0.45f,
            body = G(23), bodyColor = new Color(0.12f, 0.08f, 0.07f),
            platform = G(19), platformInset = 0.3f,
        };

        K.Ground(-2, 24, 0f, st);
        K.Ground(24, 40, 1.5f, st);
        K.Ground(45, 70, 0f, st);
        K.Ledge(49, 55, 3.8f, st); K.Ledge(58, 63, 6.8f, st);              // optional: mushroom canopy
        K.Ground(70, 92, -1.5f, st);
        K.Ground(97, 112, 0f, st); K.Ledge(113, 118, 1.4f, st);
        K.Ground(121, 150, 1f, st);
        K.Ground(150, 166, 3f, st);
        K.Ledge(168, 173, 4.6f, st); K.Ledge(176, 181, 6.2f, st);
        K.Ground(184, 214, 5f, st);
        K.Ground(214, 232, 3f, st);
        K.Ground(237, 250, 3f, st);
        K.Ground(250, 296, 2f, st);
        K.FinishTerrain(st);

        Color grove = new Color(0.78f, 0.92f, 0.82f);
        K.Backdrop(K.SpriteFile("Assets/Art/Mushroom_Grove/Assets/Background/Background_0.png"), 0.95f, 52f, 13f, "Background", 0, grove);
        K.Backdrop(K.SpriteFile("Assets/Art/Mushroom_Grove/Assets/Background/Background_1.png"), 0.75f, 32f, 9f, "backmid", 1, grove);
        K.Backdrop(K.SpriteFile("Assets/Art/Mushroom_Grove/Assets/Background/Background_2.png"), 0.55f, 30f, 8f, "backmid", 2, grove);

        K.Player(3f);
        K.Hud();
        K.Camera(8.5f, 30f, new Color(0.03f, 0.09f, 0.06f));

        K.Chapter("CHAPTER III", "The Rotting Grove", "The hermits' wood, corrupted by Malvric's sorcery. The dead do not stay buried here.");
        var note = K.SpriteAt("Assets/Art/Castle-Hall/Tiles/Props.png", "Props_9");
        K.Note(14f, "Hermit's Warning",
            "Turn back, traveller. Since the usurper's mages came, the dead do not stay in the earth here.\n\n" +
            "If you must pass, keep to the high caps of the great mushrooms. The rot sleeps below.", note, 1f);
        K.Note(126f, "Hermit's Journal",
            "Day forty. Brother Oswin walked out of the bog this morning. We buried him a month ago.\n\n" +
            "The colossus in the clearing was the king's own executioner. Malvric's mages made it something else.", note, 1f);
        K.Note(292f, "Hermit's Map (copy)",
            "The pass through the Frozen Taiga is guarded, but there is a hidden path past the smiths' forge.\n" +
            "From there the castle's postern gate lies open to anyone bold enough.", note, 1f);

        K.Shrine("Shrine of the Hermits", 100f);
        K.Shrine("Shrine of the Canopy", 190f);
        K.Shrine("Shrine of the Clearing", 252f);

        Color rot = new Color(0.72f, 1f, 0.72f), rotElite = new Color(0.55f, 0.85f, 0.55f), patrol = new Color(0.85f, 0.85f, 0.95f);
        K.Enemy(K.Kind.Skeleton, 33f, "Bog Risen", rot);
        K.Enemy(K.Kind.Axe, 62f, "Rot Ghoul", rot);
        K.Enemy(K.Kind.Skeleton, 80f, "Bog Risen", rot, 0, 0, 0, false);
        K.Enemy(K.Kind.Ninja, 88f, "Malvric's Scout", patrol);
        K.Enemy(K.Kind.EliteSkeleton, 138f, "Rotted Sentinel", rotElite);
        K.Enemy(K.Kind.Axe, 157f, "Rot Ghoul", rot);
        K.Enemy(K.Kind.Ninja, 200f, "Malvric's Scout", patrol);
        K.Enemy(K.Kind.Skeleton, 208f, "Bog Risen", rot);
        K.Enemy(K.Kind.EliteSkeleton, 226f, "Rotted Sentinel", rotElite);
        var colossus = K.Enemy(K.Kind.GiantSkeleton, 276f, "The Rotting Colossus", new Color(0.55f, 0.85f, 0.45f), 300, 22, 300, true,
            K.L("sporebane", 1f), K.L("hermits_map", 1f), K.L("verdant_tonic", 1f, 2, 2), K.L("verdant_shard", 1f, 2, 3));
        var add1 = K.Enemy(K.Kind.Skeleton, 266f, "Bog Risen", rot, 0, 0, 0, false);
        K.BossArena(258f, colossus.GetComponent<PatrolSkeletonAI>(), "The Rotting Colossus", "The ground heaves as the Colossus wakes",
            "The Colossus Crumbles", "The rot loosens its grip on the grove", "THE ROTTING COLOSSUS", "THE KING'S EXECUTIONER, UNMADE");

        K.Pickup("crimson_flask", 52f, 1.2f);
        K.Pickup("soul_shard", 61f, 1.2f, 1, "grove_canopy_shard");
        K.Pickup("heart_vessel", 179f, 1.4f, 1, "grove_heart");
        K.Pickup("violet_draught", 115f, 1.2f);
        K.Pickup("prism_relic", 60.5f, 2.4f, 1, "relic_grove");
        K.Pickup("ember_of_vengeance", 230f);
        K.Coins(4, 20, 1.3f, 6); K.Coins(72, 90, 1.3f, 6); K.Coins(186, 212, 1.3f, 8);

        // Decor: giant mushrooms, crystals, roots and fences
        float[] mush = { 8, 36, 58, 104, 132, 162, 194, 222, 244, 288 };
        for (int i = 0; i < mush.Length; i++)
        {
            K.Prop(G(3 + i % 3), mush[i], 1.6f, "Backtiles", 2, i % 2 == 0);
            K.Prop(G(i % 2 == 0 ? 6 : 9), mush[i] + 4f, 1.1f, "Backtiles", 5);
        }
        foreach (float x in new[] { 18f, 84f, 146f, 210f, 270f }) K.Prop(G(7), x, 1f, "Backtiles", 6);
        foreach (float x in new[] { 28f, 108f, 240f }) K.Prop(G(10), x, 1f, "Backtiles", 6);
        foreach (float x in new[] { 46f, 128f, 216f }) K.Prop(G(1), x, 1.1f, "Backtiles", 5);
        foreach (float x in new[] { 75f, 155f, 260f }) K.Prop(G(8), x, 1f, "Backtiles", 6);
        foreach (float x in new[] { 50f, 120f, 180f, 250f, 280f })
            K.PointLight(x, 2.5f, new Color(0.6f, 0.45f, 1f), 0.8f, 6f); // crystal glow

        // Denser corrupted growth: hanging vines, glowing crystals, rot heaps and rock arches.
        foreach (float x in new[] { 12f, 41f, 67f, 95f, 118f, 147f, 173f, 201f, 227f, 263f, 285f })
            K.Sprite(K.Decor, "Hanging Vine", G(14), new Vector3(x, K.FloorY(x) + 7.5f, 0), 1.3f, "Backtiles", 1, new Color(0.7f, 0.85f, 0.7f));
        foreach (float x in new[] { 22f, 64f, 112f, 158f, 205f, 247f, 279f })
        {
            K.Prop(G(3 + ((int)x % 3)), x, 0.9f, "Backtiles", 6, x % 2 == 0);
            K.PointLight(x, 1.2f, new Color(0.65f, 0.4f, 1f), 0.7f, 4f).gameObject.AddComponent<LightFlicker>();
        }
        foreach (float x in new[] { 31f, 87f, 136f, 218f, 270f }) K.Prop(G(8), x, 0.9f, "Backtiles", 7);   // rot heaps
        foreach (float x in new[] { 54f, 186f }) K.Prop(G(11), x, 1.2f, "Backtiles", 2);                    // stone arches
        foreach (float x in new[] { 6f, 99f, 254f }) K.Prop(G(2), x, 1.1f, "Backtiles", 5);                 // roots

        K.Atmosphere(new Color(0.65f, 0.85f, 0.75f), 0.72f, new Color(0.55f, 0.8f, 0.6f), 0.12f,
            new K.VolumeLook("GrovePost", 0.4f, 0.85f, 10f, -5f, new Color(0.9f, 1f, 0.92f)),
            "Spores", new Color(0.6f, 1f, 0.5f, 0.7f), new Color(0.8f, 0.6f, 1f, 0.7f));

        K.Exit(293f, "Taiga", new[] { colossus, add1 }, new Color(0.4f, 1f, 0.3f), "The Colossus's rot seals the path north");
        K.Save(scene);
        return K.Log.ToString();
    }
}

using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using K = LevelKit;

/// <summary>Chapter I additions on top of BuildLevel: shared HUD, rewards, lore, items and the crypt secret.</summary>
public static class Level1Extras
{
    public static string Build()
    {
        string baseLog = BuildLevel.Build();
        var scene = EditorSceneManager.GetActiveScene();
        K.Attach("Chapter I - Story and Items");
        var oldHud = GameObject.Find("[L1] HUD Canvas"); if (oldHud != null) Object.DestroyImmediate(oldHud);
        K.Hud();
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p.GetComponent<PlayerInteractor>() == null) p.AddComponent<PlayerInteractor>();
        foreach (var cp in Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
            if (cp.GetComponent<ShrineInteraction>() == null) cp.gameObject.AddComponent<ShrineInteraction>();

        K.Reward(GameObject.Find("Enemies/Restless Skeleton"), "Restless Skeleton", 25, false, K.L("crimson_flask", 1f), K.L("soul_shard", 0.5f));
        K.Reward(GameObject.Find("Enemies/Axe Skeleton"), "Axe Skeleton", 22, false, K.L("soul_shard", 1f));
        K.Reward(GameObject.Find("Enemies/Crypt Guard (West)"), "Crypt Guard", 30, false);
        K.Reward(GameObject.Find("Enemies/Crypt Guard (East)"), "Crypt Guard", 30, false);
        K.Reward(GameObject.Find("Enemies/The Gravewarden"), "The Gravewarden", 160, true,
            K.L("gravewarden_cleaver", 1f), K.L("crypt_key", 1f), K.L("broken_signet", 1f), K.L("crimson_flask", 1f));
        var gw = GameObject.Find("Enemies/The Gravewarden").GetComponent<PatrolSkeletonAI>();
        gw.enrageAtHalf = true; UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(gw);

        var stone = K.SpriteAt("Assets/Coins/platformer items - free assets/static_items.png", "static_items_2");
        K.Note(9f, "Your Own Grave",
            "HERE LIES SIR ALDRIC\nKnight-Captain of the Order\n\"Fallen in the King's service\"\n\n" +
            "The earth is torn open. The grave is empty. You remember the knives in the dark, and the voice that ordered them.", null, 1f, "Read the epitaph");
        K.Note(57f, "Weathered Gravestone",
            "The eleven of the old king's guard, buried in one night, in one grave.\n\nSomeone has scratched a name beneath: MALVRIC.", null, 1f, "Read");
        K.Note(205f, "Gravedigger's Note",
            "The new lord pays well for silence. The Gravewarden keeps the crypt keys and the captain's ring. " +
            "Said the captain might not stay buried. Laughed. I didn't.", null, 1f);
        K.Note(428f, "Road Sign",
            "East: the woodcutters' camp, burned. Malvric's hired blades sleep there now.", null, 1f);

        // Cinematics: the resurrection on a New Game, then the chapter card.
        var grave = GameObject.FindGameObjectWithTag("Player").transform.position;
        var opening = K.Child(K.Content, "Opening Cinematic", grave).gameObject.AddComponent<OpeningCinematic>();
        var burst = UIKit.Particles("Grave Dirt", opening.transform, grave + Vector3.down * 1.5f, UIKit.Gen("SoftCircle"), "Grill", 12);
        var bm = burst.main; bm.loop = false; bm.playOnAwake = false; bm.prewarm = false; bm.duration = 2.4f;
        bm.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f); bm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
        bm.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f); bm.gravityModifier = 1.2f;
        bm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.27f, 0.2f), new Color(0.55f, 0.45f, 0.35f));
        var be = burst.emission; be.rateOverTime = 45f;
        var bs = burst.shape; bs.shapeType = ParticleSystemShapeType.Cone; bs.angle = 35f; bs.rotation = new Vector3(-90f, 0, 0); bs.radius = 1f;
        var gl = UIKit.Light("Grave Light", opening.transform, UnityEngine.Rendering.Universal.Light2D.LightType.Point, grave + Vector3.up * 1f, new Color(0.6f, 0.85f, 1f), 0f, 6f, 0.5f);
        K.Set(opening, "dirtBurst", burst);
        K.Set(opening, "graveLight", gl);
        // Aldric's grave: a weathered headstone over a mound of disturbed earth.
        var headstone = K.Prop(stone, grave.x - 1.6f, 3.2f, "Backtiles", 8, false, 0.2f, new Color(0.62f, 0.64f, 0.72f));
        var mound = K.Prop(K.SpriteAt("Assets/Art/Wood-Camp/Tiles/Assets.png", "Assets_9"), grave.x + 0.6f, 1.1f, "Backtiles", 9, false, 0.25f, new Color(0.45f, 0.38f, 0.34f));
        K.Chapter("CHAPTER I", "The Forsaken Graveyard", "Where the knight was buried, and where he will not stay.");
        var enc = Object.FindFirstObjectByType<BossEncounter>();
        if (enc != null) enc.ConfigureIntro("THE GRAVEWARDEN", "KEEPER OF THE CRYPTS", "");

        // More of the haunted graveyard: candle lights at graves, distant ravens' perches, mist.
        foreach (float x in new[] { 9f, 57f, 140f, 205f, 263f, 330f, 380f, 420f }) K.Torch(x + 1.5f, 0.6f, 0.6f);
        foreach (float x in new[] { 36f, 98f, 172f, 240f, 312f, 365f, 401f })
            K.Prop(stone, x, 1.6f, "Backtiles", 9, x % 2 == 0, 0.15f, new Color(0.55f, 0.58f, 0.66f));

        K.Pickup("crimson_flask", 64f);
        K.Pickup("boneplate_cuirass", 186f, 1.4f, 1, "grave_boneplate");
        K.Pickup("ring_of_the_fallen", 27f, 3.0f, 1, "grave_ring");   // beneath the ruined arch, a jump up
        K.Pickup("soul_shard", 255f, 1.2f, 1, "grave_shard_1");
        K.Pickup("verdant_shard", 320f, 1.2f, 1, "grave_shard_2");
        K.Pickup("heart_vessel", 300f, 1.4f, 1, "grave_heart");
        K.Pickup("prism_relic", 408f, 1.4f, 1, "relic_graveyard");
        K.Save(scene);
        return baseLog + K.Log.ToString();
    }
}

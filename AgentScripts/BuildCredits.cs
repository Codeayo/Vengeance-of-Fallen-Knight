using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>Rebuilds the Credits scene: epilogue, cast, and every asset creator used in the game.</summary>
public static class BuildCredits
{
    const string Gold = "#E8C27A", Ash = "#A9A3AA";

    static string H(string s) => $"\n\n\n<size=130%><color={Gold}><b>{s}</b></color></size>\n<color={Gold}>———  •  ———</color>\n";
    static string Role(string role, string who, string note = "") =>
        $"\n<size=78%><color={Ash}>{role}</color></size>\n<b>{who}</b>" + (note == "" ? "" : $"\n<size=70%><color={Ash}>{note}</color></size>") + "\n";

    public static string Text() =>
        $"<size=170%><color={Gold}><b>VENGEANCE</b></color></size>\n<size=110%>OF THE FALLEN KNIGHT</size>\n\n\n" +
        "<i>Lord Malvric lies dead at the foot of the throne he stole.\n" +
        "The eleven of the old guard are avenged, and the truth of the night\n" +
        "Sir Aldric fell is carried to every corner of the realm.\n\n" +
        "The Shard of the Crown is returned to the old king's line.\n" +
        "The fallen knight lays down his sword before the empty throne\n" +
        "and, at last, is allowed to rest.</i>" +

        H("A GAME BY") +
        Role("Created, designed and directed by", "Codeayo") +

        H("DRAMATIS PERSONAE") +
        Role("The Fallen Knight", "Sir Aldric, Knight-Captain of the Order") +
        Role("The Narrator", "The Watcher") +
        Role("The Usurper King", "Lord Malvric") +
        Role("Malvric's Blade", "Kage") +
        Role("Captain of the Black Guard", "Sir Dravek") +
        Role("Keeper of the Crypts", "The Gravewarden") +
        Role("The King's Executioner, Unmade", "The Rotting Colossus") +
        Role("Guardian of Frostbrand", "The Frost Warden") +

        H("ART & ASSET CREDITS") +
        Role("Main Character", "SpellSoft Pixel", "spellsoftpixel.itch.io/2d-soulslike-knight") +
        Role("Heart UI", "In The Void", "in-the-void.itch.io/pixel-hearts-pack") +
        Role("Main Menu & GUI", "Tiopalada", "tiopalada.itch.io/tiny-rpg-mana-soul-gui") +
        Role("Collectible Items", "Dani Maccari", "dani-maccari.itch.io/platformer-metroidvania-pixel-items-free-assets") +

        H("ENEMIES") +
        Role("Skeleton Enemy Pack", "Jesse M", "jesse-m.itch.io/skeleton-pack") +
        Role("Ninja Enemy Pack", "KBPixelArt", "kbpixelart.itch.io/ninja-enemy-pack") +
        Role("Free Skeleton Pixel Art Sprite Sheets", "CraftPix.net", "craftpix.net/freebies/free-skeleton-pixel-art-sprite-sheets") +

        H("ENVIRONMENT ART") +
        Role("Moon Graveyard  •  Hero's Journey: Wood Camp  •  Mushroom Grove\nFinal Taiga Forest  •  Castle Hall  •  Dungeon Prison", "Anokolisa", "anokolisa.itch.io") +
        Role("Fantasy Swamp Forest", "The Flavare", "theflavare.itch.io") +

        H("EFFECTS AND TYPE") +
        Role("Explosions Pack", "Luis Zuno (Ansimuz)", "ansimuz.itch.io") +
        Role("Typeface", "Liberation Sans", "Red Hat  •  SIL Open Font License") +

        H("AUDIO") +
        Role("Bonfires Dungeon Pack", "Arnocyreus", "arnocyreus.itch.io/bonfiresdungeon") +
        $"\n<size=78%><color={Ash}>Nightfall  •  Mistridden Woods  •  Voices From The Grove  •  Ruins In The Deep\nNecron's Tower  •  Temple Of Illas  •  Cursemark  •  Tyrant's Lament  •  Bonfires</color></size>\n" +

        H("SOUND") +
        Role("Sound effects and ambience", "Synthesized for this game") +

        H("TECHNOLOGY") +
        Role("Engine", "Unity 6") +
        Role("Rendering and tools", "Universal Render Pipeline 2D  •  Cinemachine  •  TextMesh Pro  •  Input System") +
        Role("Development assistance", "Claude Code by Anthropic") +

        "\n\n\n\n" +
        $"<color={Ash}><size=80%>All third-party assets are used under the licenses provided by their creators.\nThank you to every artist and musician who shared their work.</size></color>";

    public static string Build()
    {
        UIKit.EnsureGenerated();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Credits.unity", OpenSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects().Where(g => g.name == "Canvas" || g.name.StartsWith("[Credits]")).ToArray())
            Object.DestroyImmediate(go);

        var cam = Camera.main;
        cam.orthographic = true; cam.orthographicSize = 8f;
        cam.backgroundColor = new Color(0.02f, 0.015f, 0.03f); cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 0, -10);

        // Atmosphere: drifting embers and mist behind the text, lit by a dim warm light.
        var fx = new GameObject("[Credits] Atmosphere").transform;
        UIKit.Light("Ambient", fx, Light2D.LightType.Global, Vector3.zero, new Color(0.7f, 0.65f, 0.8f), 0.8f);
        var embers = UIKit.Particles("Embers", fx, new Vector3(0, -9f, 0), UIKit.Gen("SoftCircle"), "Grill", 5);
        UIKit.Embers(embers, new Vector3(30f, 1f, 1f), 14f, new Color(1f, 0.55f, 0.2f, 0.8f), new Color(1f, 0.82f, 0.45f, 0.8f), 0.16f);
        var vel = embers.velocityOverLifetime; vel.y = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        var mist = UIKit.Particles("Mist", fx, new Vector3(0, -6f, 0), UIKit.Gen("Mist"), "Backtiles", 2);
        UIKit.Fog(mist, new Vector3(34f, 2f, 1f), 2f, new Color(0.55f, 0.5f, 0.7f, 0.12f), new Vector2(6f, 11f), 0.3f);

        var canvas = UIKit.MakeCanvas("[Credits] Canvas", 5);
        var ct = canvas.transform;
        var vign = UIKit.Rect("Top Shade", ct, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 260));
        UIKit.Img(vign, UIKit.Gen("VFadeUp"), new Color(0.02f, 0.015f, 0.03f, 1f)).transform.localScale = new Vector3(1, -1, 1);
        var bottom = UIKit.Rect("Bottom Shade", ct, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 260));
        UIKit.Img(bottom, UIKit.Gen("VFadeUp"), new Color(0.02f, 0.015f, 0.03f, 1f));

        var viewport = UIKit.Stretch("Viewport", ct);
        viewport.SetAsFirstSibling();
        var content = UIKit.Rect("Credits Text", viewport, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1500, 100));
        var text = UIKit.Text(content, Text(), 38, UIKit.Bone, TextAlignmentOptions.Top, UIKit.BodyMat);
        text.lineSpacing = 6;
        text.ForceMeshUpdate();
        content.sizeDelta = new Vector2(1500, text.preferredHeight + 80);

        var closing = UIKit.Rect("Closing Card", ct, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 300));
        var cg = closing.gameObject.AddComponent<CanvasGroup>();
        var thanks = UIKit.Text(UIKit.Rect("Thanks", closing, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(0, 120)),
            "THANK YOU FOR PLAYING", 84, Color.white, TextAlignmentOptions.Bottom, UIKit.TitleMat, FontStyles.Bold, 12f);
        thanks.enableVertexGradient = true;
        thanks.colorGradient = new VertexGradient(new Color(1f, 0.93f, 0.72f), new Color(1f, 0.93f, 0.72f), new Color(0.86f, 0.5f, 0.18f), new Color(0.86f, 0.5f, 0.18f));
        UIKit.Text(UIKit.Rect("Sub", closing, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(0, 60)),
            "Sir Aldric rests at last.", 34, UIKit.Bone, TextAlignmentOptions.Top, UIKit.BodyMat, FontStyles.Italic);

        UIKit.Text(UIKit.Rect("Hint", ct, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 30), new Vector2(800, 40)),
            "Hold <color=#E8C27A>Space</color> to speed up   •   <color=#E8C27A>Esc</color> to return to the menu", 22, UIKit.Ash, TextAlignmentOptions.BottomRight, UIKit.BodyMat);

        var roll = canvas.gameObject.AddComponent<CreditScene>();
        var so = new SerializedObject(roll);
        so.FindProperty("content").objectReferenceValue = content;
        so.FindProperty("closingCard").objectReferenceValue = cg;
        so.FindProperty("scrollSpeed").floatValue = 70f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"credits built, text height {text.preferredHeight:F0}px (~{(text.preferredHeight + 1080) / 70f:F0}s roll)";
    }
}

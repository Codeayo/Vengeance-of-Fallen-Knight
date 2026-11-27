using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class BuildMenu
{
    const string MenuScene = "Assets/Scenes/Main Menu.unity";
    const string GraveScene = "Assets/Scenes/Graveyard.unity";
    static readonly Vector3 CamPos = new Vector3(30f, 4.2f, -10f);
    const float CamSize = 8f;

    public static string Build()
    {
        UIKit.EnsureGenerated();
        UIKit.SetBorder("20250420manaSoul9SlicesA-Sheet.png", new Vector4(22, 22, 22, 22));
        UIKit.SetBorder("20250420manaSoulHeaderA-Sheet.png", new Vector4(30, 8, 30, 8));

        var scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Main Camera" || root.name == "MusicManager" || root.name == "GameManager")
                continue;
            Object.DestroyImmediate(root);
        }

        // ---------- Camera ----------
        var cam = Object.FindFirstObjectByType<Camera>();
        cam.transform.position = CamPos;
        cam.orthographic = true;
        cam.orthographicSize = CamSize;
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.06f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        var camData = cam.GetUniversalAdditionalCameraData();
        camData.renderPostProcessing = true;

        // ---------- Backdrop ----------
        var backdrop = new GameObject("Backdrop");
        backdrop.transform.position = new Vector3(CamPos.x, CamPos.y, 0f);
        var fit = backdrop.AddComponent<FitCameraHeight>();
        var fso = new SerializedObject(fit);
        fso.FindProperty("referenceSize").vector2Value = new Vector2(CamSize * 2f * 2.4f, CamSize * 2f);
        fso.FindProperty("overscan").floatValue = 1.0f;
        fso.ApplyModifiedPropertiesWithoutUndo();

        var sky = Layer("Sky", backdrop.transform, 0.15f, 0.25f);
        var skySr = Sprite(sky, "Sky", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Graveyard/Background_0.png"), new Vector3(CamPos.x + 1f, CamPos.y + 1.2f, 0), 0.82f, "Background", 0);
        skySr.flipX = true;

        var mid = Layer("Mausoleums", backdrop.transform, 0.35f, 0.35f);
        var midSr = Sprite(mid, "Mausoleums", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Graveyard/Background_1.png"), new Vector3(CamPos.x + 1f, 5.2f, 0), 0.7f, "backmid", 0);
        midSr.color = new Color(0.78f, 0.80f, 0.92f);

        var ground = Layer("Graveyard", backdrop.transform, 0.6f, 0.5f);
        CopyGraveyardSlice(ground.transform, -8f, 52f);

        // Fallen knight overlooking the ruins.
        var knight = new GameObject("Fallen Knight");
        knight.transform.SetParent(ground.transform, false);
        knight.transform.position = new Vector3(37.2f, -0.7f + 1.58f, 0);
        var ksr = knight.AddComponent<SpriteRenderer>();
        ksr.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Players/SoulslikeKnight/SoulslikeKnight_SpriteSheets/SoulslikeKnight_Idle.png").OfType<Sprite>().OrderBy(s => s.name.Length).ThenBy(s => s.name).First();
        ksr.sortingLayerName = "player";
        ksr.sortingOrder = 5;
        ksr.flipX = false;
        var kAnim = knight.AddComponent<Animator>();
        kAnim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Players/SoulslikeKnight/SoulslikeKnight_SpriteSheets/Player.controller");

        // Foreground silhouettes.
        var fg = Layer("Foreground", backdrop.transform, 1f, 0.7f);
        var brush = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Graveyard/brush.png").OfType<Sprite>().ToArray();
        for (int i = 0; i < 6; i++)
        {
            var b = Sprite(fg, "Brush " + i, brush[i % brush.Length], new Vector3(CamPos.x - 17f + i * 8.5f, -3.9f + (i % 2) * 0.25f, 0), 1.2f, "ui", -5);
            b.color = new Color(0.02f, 0.02f, 0.04f, 1f);
            b.flipX = i % 2 == 0;
        }

        // ---------- Atmosphere ----------
        var fx = new GameObject("Atmosphere");
        fx.transform.SetParent(backdrop.transform, false);
        var fogBack = UIKit.Particles("Fog Back", fx.transform, new Vector3(CamPos.x, 0.4f, 0), UIKit.Gen("Mist"), "Backtiles", 5);
        UIKit.Fog(fogBack, new Vector3(44f, 1.4f, 1f), 2.2f, new Color(0.55f, 0.62f, 0.8f, 0.13f), new Vector2(5f, 9f), 0.3f);
        var fogFront = UIKit.Particles("Fog Front", fx.transform, new Vector3(CamPos.x, -1.5f, 0), UIKit.Gen("Mist"), "Grill", 20);
        UIKit.Fog(fogFront, new Vector3(44f, 0.5f, 1f), 1.4f, new Color(0.62f, 0.68f, 0.85f, 0.09f), new Vector2(4f, 7f), 0.5f);
        var embers = UIKit.Particles("Embers", fx.transform, new Vector3(CamPos.x + 4f, -1f, 0), UIKit.Gen("SoftCircle"), "player", 10);
        UIKit.Embers(embers, new Vector3(26f, 1f, 1f), 9f, new Color(1f, 0.55f, 0.2f, 0.9f), new Color(1f, 0.82f, 0.45f, 0.9f), 0.14f);
        var souls = UIKit.Particles("Wisps", fx.transform, new Vector3(CamPos.x + 6f, 2f, 0), UIKit.Gen("SoftCircle"), "player", 11);
        UIKit.Embers(souls, new Vector3(18f, 6f, 1f), 2.5f, new Color(0.45f, 0.85f, 1f, 0.7f), new Color(0.7f, 0.95f, 1f, 0.7f), 0.22f);

        var lights = new GameObject("Lights");
        lights.transform.SetParent(backdrop.transform, false);
        UIKit.Light("Moonlight (Global)", lights.transform, Light2D.LightType.Global, Vector3.zero, new Color(0.62f, 0.70f, 0.95f), 0.62f);
        UIKit.Light("Moon Glow", lights.transform, Light2D.LightType.Point, new Vector3(CamPos.x + 9f, CamPos.y + 6f, 0), new Color(0.65f, 0.85f, 1f), 0.8f, 10f, 1f);
        UIKit.Light("Knight Rim", lights.transform, Light2D.LightType.Point, new Vector3(38f, 1.8f, 0), new Color(1f, 0.66f, 0.35f), 1.4f, 5f, 0.4f);
        UIKit.Light("Statue Glow", lights.transform, Light2D.LightType.Point, new Vector3(28f, 3.5f, 0), new Color(0.5f, 0.75f, 1f), 0.5f, 5f, 0.2f);

        UIKit.GlobalVolume(UIKit.Profile("MenuPost", 0.42f, 0.9f, 14f, -6f, new Color(0.95f, 0.93f, 1f)));

        // ---------- UI ----------
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        // The module creates its own default UI actions when none are assigned.

        var canvas = UIKit.MakeCanvas("Menu Canvas", 10);
        var ct = canvas.transform;

        var shade = UIKit.Rect("Left Shade", ct, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(1250, 0));
        UIKit.Img(shade, UIKit.Gen("HFadeRight"), new Color(0.01f, 0.01f, 0.03f, 0.9f));
        var bottom = UIKit.Rect("Bottom Shade", ct, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 240));
        UIKit.Img(bottom, UIKit.Gen("VFadeUp"), new Color(0, 0, 0, 0.7f));

        // Title
        var title = UIKit.Rect("Title", ct, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -96), new Vector2(1200, 300));
        var titleGroup = title.gameObject.AddComponent<CanvasGroup>();
        var t1 = UIKit.Text(UIKit.Rect("Vengeance", title, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(1200, 140)),
            "VENGEANCE", 132, Color.white, TextAlignmentOptions.TopLeft, UIKit.TitleMat, FontStyles.Bold, 16f);
        t1.enableVertexGradient = true;
        t1.colorGradient = new VertexGradient(new Color(1f, 0.93f, 0.7f), new Color(1f, 0.9f, 0.65f), new Color(0.86f, 0.5f, 0.18f), new Color(0.8f, 0.45f, 0.15f));
        UIKit.Text(UIKit.Rect("Subtitle", title, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -138), new Vector2(1200, 60)),
            "OF THE FALLEN KNIGHT", 46, UIKit.Bone, TextAlignmentOptions.TopLeft, UIKit.BodyMat, FontStyles.Bold, 34f);
        var line = UIKit.Rect("Divider", title, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(0, -214), new Vector2(760, 5));
        UIKit.Img(line, UIKit.Gen("HBand"), new Color(1f, 0.72f, 0.35f, 0.95f));
        var gem = UIKit.Rect("Gem", title, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(380, -214), new Vector2(30, 30));
        UIKit.Img(gem, UIKit.Gen("Diamond"), Color.white);
        UIKit.Text(UIKit.Rect("Tagline", title, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -238), new Vector2(1100, 50)),
            "The kingdom has fallen. Only vengeance remains.", 27, new Color(0.72f, 0.70f, 0.74f), TextAlignmentOptions.TopLeft, UIKit.BodyMat, FontStyles.Italic, 2f);

        // Main panel
        var main = UIKit.Rect("Main Panel", ct, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, -420), new Vector2(700, 520));
        var mainGroup = main.gameObject.AddComponent<CanvasGroup>();
        string[] labels = { "Continue", "New Game", "Level Select", "Settings", "About", "Exit" };
        var buttons = new Button[labels.Length];
        for (int i = 0; i < labels.Length; i++)
            buttons[i] = UIKit.MenuButton(labels[i].Replace(" ", "") + "Button", main, labels[i], new Vector2(0, -34 - i * 78), new Vector2(660, 66), 42);
        var detailRt = UIKit.Rect("Detail", buttons[0].transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-10, -2), new Vector2(330, 0));
        var detail = UIKit.Text(detailRt, "I. The Forsaken Graveyard", 22, new Color(0.85f, 0.68f, 0.42f, 0.9f), TextAlignmentOptions.MidlineRight, UIKit.BodyMat, FontStyles.Italic);
        UIKit.SetVerticalNav(buttons);

        // Level select
        var lvl = UIKit.Panel("Level Select Panel", ct, new Vector2(900, 700), "Chapters", out var lvlGroup);
        var levelButtons = new Button[GameSave.Levels.Length];
        for (int i = 0; i < levelButtons.Length; i++)
            levelButtons[i] = UIKit.MenuButton("Level " + (i + 1), lvl, GameSave.Levels[i].title, new Vector2(120, -120 - i * 72), new Vector2(680, 60), 32);
        var lvlBack = UIKit.MenuButton("Back", lvl, "Back", new Vector2(120, -120 - levelButtons.Length * 72 - 14), new Vector2(680, 60), 32);
        UIKit.SetVerticalNav(levelButtons.Cast<Selectable>().Append(lvlBack).ToArray());

        // Settings
        var set = UIKit.Panel("Settings Panel", ct, new Vector2(1060, 700), "Settings", out var setGroup);
        string[] rows = { "Music", "Effects", "Fullscreen", "Resolution", "Quality" };
        var rowLabels = new TMP_Text[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            float y = -140 - i * 84;
            rowLabels[i] = UIKit.Text(UIKit.Rect(rows[i] + " Label", set, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(100, y), new Vector2(330, 60)),
                rows[i], 32, UIKit.Bone, TextAlignmentOptions.MidlineLeft, UIKit.BodyMat, FontStyles.Bold | FontStyles.UpperCase, 5f);
        }

        var music = UIKit.MakeSlider("Music Slider", set, new Vector2(440, -140), new Vector2(360, 50), rowLabels[0]);
        var sfx = UIKit.MakeSlider("Effects Slider", set, new Vector2(440, -224), new Vector2(360, 50), rowLabels[1]);
        var musicVal = ValueText(set, "Music Value", -140);
        var sfxVal = ValueText(set, "Effects Value", -224);

        // Fullscreen toggle
        var tg = UIKit.Rect("Fullscreen Toggle", set, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(440, -308), new Vector2(360, 54));
        var tgHit = UIKit.Img(tg, UIKit.Gen("WhitePixel"), new Color(0, 0, 0, 0), true);
        var box = UIKit.Rect("Box", tg, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(46, 46));
        UIKit.Img(box, UIKit.Gen("WhitePixel"), new Color(0.6f, 0.45f, 0.22f, 0.9f));
        var boxInner = UIKit.Rect("Inner", box, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6, -6));
        UIKit.Img(boxInner, UIKit.Gen("WhitePixel"), new Color(0.05f, 0.04f, 0.08f, 1f));
        var check = UIKit.Rect("Check", box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 34));
        var checkImg = UIKit.Img(check, UIKit.Gen("Diamond"), Color.white);
        var toggle = tg.gameObject.AddComponent<Toggle>();
        toggle.graphic = checkImg;
        toggle.targetGraphic = tgHit;
        toggle.transition = Selectable.Transition.None;
        var tmb = tg.gameObject.AddComponent<MenuButton>();
        var tso = new SerializedObject(tmb);
        tso.FindProperty("label").objectReferenceValue = rowLabels[2];
        tso.FindProperty("selectedScale").floatValue = 1f;
        tso.FindProperty("labelSlide").floatValue = 0f;
        tso.ApplyModifiedPropertiesWithoutUndo();
        var fsVal = ValueText(set, "Fullscreen Value", -308);

        var res = UIKit.MakeSlider("Resolution Slider", set, new Vector2(440, -392), new Vector2(360, 50), rowLabels[3], false);
        var resVal = UIKit.Text(UIKit.Rect("Resolution Value", res.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero),
            "< 1920 x 1080 >", 28, UIKit.Gold, TextAlignmentOptions.Center, UIKit.BodyMat, FontStyles.Bold);
        var qual = UIKit.MakeSlider("Quality Slider", set, new Vector2(440, -476), new Vector2(360, 50), rowLabels[4], false);
        var qualVal = UIKit.Text(UIKit.Rect("Quality Value", qual.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero),
            "< High >", 28, UIKit.Gold, TextAlignmentOptions.Center, UIKit.BodyMat, FontStyles.Bold);
        UIKit.Text(UIKit.Rect("Hint", set, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 118), new Vector2(900, 40)),
            "Use Left / Right to adjust.  Display changes apply in the built game.", 22, UIKit.Ash, TextAlignmentOptions.Center, UIKit.BodyMat, FontStyles.Italic);
        var setBack = UIKit.MenuButton("Back", set, "Back", new Vector2(440, -620), new Vector2(240, 60), 32);
        UIKit.SetVerticalNav(new Selectable[] { music, sfx, toggle, res, qual, setBack });

        // About
        var about = UIKit.Panel("About Panel", ct, new Vector2(1240, 780), "About", out var aboutGroup);
        var aboutText = UIKit.Text(UIKit.Rect("Body", about, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -92), new Vector2(-200, 560)),
            "<color=#E8C27A><b>VENGEANCE OF THE FALLEN KNIGHT</b></color>\n" +
            "<size=24><i>Once the sworn shield of the realm, the knight fell defending a crown betrayed from within. " +
            "Risen from an unmarked grave, he walks the ruins of his kingdom to bring its traitors to judgement.</i></size>\n\n" +
            "<color=#E8C27A><b>CONTROLS</b></color>\n" +
            "<size=24>Move  <b>A / D</b>  or  <b>Arrow Keys</b>        Jump  <b>Space</b>  (press again to double jump)\n" +
            "Attack  <b>J</b> / <b>Left Mouse</b>     Roll  <b>Shift</b>     Block  <b>Right Mouse</b> / <b>K</b>     Heal  <b>H</b>\n" +
            "Interact  <b>E</b>     Inventory  <b>I</b> / <b>Tab</b>     Pause  <b>Esc</b>     Gamepad fully supported</size>\n\n" +
            "<color=#E8C27A><b>CREDITS</b></color>\n" +
            "<size=22>Environment art by Anokolisa   •   Tiny RPG Mana Soul GUI by Gabriel \"tiopalada\" Lima (CC0)\n" +
            "Soulslike Knight hero sprites   •   Music from the \"Bonfires\" dark fantasy music pack</size>",
            28, UIKit.Bone, TextAlignmentOptions.Top, UIKit.BodyMat);
        aboutText.lineSpacing = 8;
        var aboutVersion = UIKit.Text(UIKit.Rect("Version", about, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 112), new Vector2(800, 36)),
            "Version " + PlayerSettings.bundleVersion, 20, UIKit.Ash, TextAlignmentOptions.Center, UIKit.BodyMat);
        var aboutBack = UIKit.MenuButton("Back", about, "Back", new Vector2(530, -700), new Vector2(240, 60), 32);
        aboutBack.navigation = new Navigation { mode = Navigation.Mode.None };

        // Confirm
        var confirm = UIKit.Panel("Confirm Panel", ct, new Vector2(860, 400), "New Journey", out var confirmGroup);
        UIKit.Text(UIKit.Rect("Message", confirm, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(-140, 120)),
            "Begin a new journey?\n<size=24><color=#A9A3AA>Your current progress will be lost. Unlocked chapters remain.</color></size>", 32, UIKit.Bone, TextAlignmentOptions.Top, UIKit.BodyMat);
        var yes = UIKit.MenuButton("Yes", confirm, "Begin Anew", new Vector2(130, -290), new Vector2(300, 60), 32);
        var no = UIKit.MenuButton("No", confirm, "Return", new Vector2(500, -290), new Vector2(240, 60), 32);
        yes.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = no, selectOnLeft = no };
        no.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = yes, selectOnRight = yes };

        // Footer
        UIKit.Text(UIKit.Rect("Controls Hint", ct, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(150, 46), new Vector2(1100, 40)),
            "<color=#E8C27A>W / S</color>  Navigate      <color=#E8C27A>Enter</color>  Select      <color=#E8C27A>Esc</color>  Back      <color=#A9A3AA>Mouse and gamepad supported</color>",
            22, UIKit.Ash, TextAlignmentOptions.BottomLeft, UIKit.BodyMat);
        UIKit.Text(UIKit.Rect("Version", ct, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 46), new Vector2(400, 40)),
            "v" + PlayerSettings.bundleVersion, 22, new Color(0.6f, 0.58f, 0.62f, 0.8f), TextAlignmentOptions.BottomRight, UIKit.BodyMat);

        // ---------- Controller ----------
        var ctrlGo = new GameObject("Main Menu Controller");
        var ctrl = ctrlGo.AddComponent<MainMenuController>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("mainPanel").objectReferenceValue = mainGroup;
        so.FindProperty("levelSelectPanel").objectReferenceValue = lvlGroup;
        so.FindProperty("settingsPanel").objectReferenceValue = setGroup;
        so.FindProperty("aboutPanel").objectReferenceValue = aboutGroup;
        so.FindProperty("confirmPanel").objectReferenceValue = confirmGroup;
        so.FindProperty("titleGroup").objectReferenceValue = titleGroup;
        so.FindProperty("continueButton").objectReferenceValue = buttons[0];
        so.FindProperty("newGameButton").objectReferenceValue = buttons[1];
        so.FindProperty("levelSelectButton").objectReferenceValue = buttons[2];
        so.FindProperty("settingsButton").objectReferenceValue = buttons[3];
        so.FindProperty("aboutButton").objectReferenceValue = buttons[4];
        so.FindProperty("exitButton").objectReferenceValue = buttons[5];
        so.FindProperty("continueDetail").objectReferenceValue = detail;
        var lb = so.FindProperty("levelButtons");
        lb.arraySize = levelButtons.Length;
        for (int i = 0; i < levelButtons.Length; i++)
            lb.GetArrayElementAtIndex(i).objectReferenceValue = levelButtons[i];
        so.FindProperty("levelBackButton").objectReferenceValue = lvlBack;
        so.FindProperty("musicSlider").objectReferenceValue = music;
        so.FindProperty("sfxSlider").objectReferenceValue = sfx;
        so.FindProperty("fullscreenToggle").objectReferenceValue = toggle;
        so.FindProperty("resolutionSlider").objectReferenceValue = res;
        so.FindProperty("qualitySlider").objectReferenceValue = qual;
        so.FindProperty("musicValue").objectReferenceValue = musicVal;
        so.FindProperty("sfxValue").objectReferenceValue = sfxVal;
        so.FindProperty("fullscreenValue").objectReferenceValue = fsVal;
        so.FindProperty("resolutionValue").objectReferenceValue = resVal;
        so.FindProperty("qualityValue").objectReferenceValue = qualVal;
        so.FindProperty("settingsBackButton").objectReferenceValue = setBack;
        so.FindProperty("aboutBackButton").objectReferenceValue = aboutBack;
        so.FindProperty("confirmYesButton").objectReferenceValue = yes;
        so.FindProperty("confirmNoButton").objectReferenceValue = no;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "menu built";
    }

    static TMP_Text ValueText(Transform parent, string name, float y)
    {
        return UIKit.Text(UIKit.Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(830, y), new Vector2(140, 60)),
            "50%", 28, UIKit.Gold, TextAlignmentOptions.MidlineLeft, UIKit.BodyMat, FontStyles.Bold);
    }

    static GameObject Layer(string name, Transform parent, float depth, float mouse)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var p = go.AddComponent<MenuParallaxLayer>();
        var so = new SerializedObject(p);
        so.FindProperty("depth").floatValue = depth;
        so.FindProperty("mouseAmount").floatValue = mouse;
        so.FindProperty("driftAmount").floatValue = 0.18f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    static SpriteRenderer Sprite(GameObject parent, string name, Sprite sprite, Vector3 worldPos, float scale, string layer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return sr;
    }

    /// <summary>Copies a slice of the Graveyard's tilemaps and props so the menu backdrop matches level 1.</summary>
    static void CopyGraveyardSlice(Transform parent, float minX, float maxX)
    {
        var menuScene = SceneManager.GetActiveScene();
        var grave = EditorSceneManager.OpenScene(GraveScene, OpenSceneMode.Additive);
        try
        {
            var grid = grave.GetRootGameObjects().First(g => g.name == "Grid");
            var copy = Object.Instantiate(grid);
            copy.name = "Graveyard Slice";
            SceneManager.MoveGameObjectToScene(copy, menuScene);
            copy.transform.SetParent(parent, true);

            foreach (var tm in copy.GetComponentsInChildren<Tilemap>(true))
            {
                tm.CompressBounds();
                var b = tm.cellBounds;
                for (int x = b.xMin; x < b.xMax; x++)
                    for (int y = b.yMin; y < b.yMax; y++)
                    {
                        var c = new Vector3Int(x, y, 0);
                        float wx = tm.GetCellCenterWorld(c).x;
                        if ((wx < minX || wx > maxX) && tm.HasTile(c))
                            tm.SetTile(c, null);
                    }
            }

            foreach (var sr in copy.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.transform.position.x < minX || sr.transform.position.x > maxX)
                    Object.DestroyImmediate(sr.gameObject);

            foreach (var c in copy.GetComponentsInChildren<SpikeDamage>(true)) Object.DestroyImmediate(c);
            foreach (var c in copy.GetComponentsInChildren<CompositeCollider2D>(true)) Object.DestroyImmediate(c);
            foreach (var c in copy.GetComponentsInChildren<Rigidbody2D>(true)) Object.DestroyImmediate(c);
            foreach (var c in copy.GetComponentsInChildren<Collider2D>(true)) Object.DestroyImmediate(c);

            // Things generated later by the level builder should never be copied into the menu.
            foreach (Transform t in copy.GetComponentsInChildren<Transform>(true).Where(t => t != null && t.name.StartsWith("[L1]")).ToArray())
                if (t != null) Object.DestroyImmediate(t.gameObject);
        }
        finally
        {
            EditorSceneManager.CloseScene(grave, true);
        }
    }
}

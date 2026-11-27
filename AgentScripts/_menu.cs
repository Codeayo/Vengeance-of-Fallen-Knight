using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
// Shared editor-time helpers for building the menu and HUD (not part of the game build).

public static class UIKit
{
    public const string GenDir = "Assets/Art/Generated";
    public const string Gui = "Assets/Main menu/tinyRPG_manaSoulGUI_v_1_0/";

    public static Color Gold = new Color(1f, 0.80f, 0.42f, 1f);
    public static Color Bone = new Color(0.86f, 0.82f, 0.76f, 1f);
    public static Color Ash = new Color(0.62f, 0.60f, 0.62f, 1f);

    // ---------------- ASSETS ----------------
    public static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    public delegate Color PixelFn(float u, float v);

    public static Sprite GenSprite(string name, int w, int h, PixelFn fn, Vector4 border = default, float ppu = 100f)
    {
        EnsureDir(GenDir);
        string path = $"{GenDir}/{name}.png";
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, fn((x + 0.5f) / w, (y + 0.5f) / h));
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Bilinear;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.spriteBorder = border;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static void EnsureGenerated()
    {
        GenSprite("SoftCircle", 128, 128, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            float a = Mathf.Clamp01(1f - d);
            return new Color(1, 1, 1, a * a * (3 - 2 * a));
        });
        GenSprite("HBand", 256, 32, (u, v) =>
        {
            float a = Mathf.Sin(Mathf.PI * u);
            float b = Mathf.Sin(Mathf.PI * v);
            return new Color(1, 1, 1, Mathf.Pow(a, 1.5f) * Mathf.Pow(b, 0.6f));
        });
        GenSprite("HFadeRight", 256, 4, (u, v) => new Color(1, 1, 1, Mathf.SmoothStep(1f, 0f, u)));
        GenSprite("VFadeUp", 4, 256, (u, v) => new Color(1, 1, 1, Mathf.SmoothStep(1f, 0f, v)));
        GenSprite("Diamond", 64, 64, (u, v) =>
        {
            float d = Mathf.Abs(u - 0.5f) + Mathf.Abs(v - 0.5f);
            float a = Mathf.Clamp01((0.5f - d) * 40f);
            float inner = Mathf.Clamp01((0.36f - d) * 40f);
            return Color.Lerp(new Color(0.35f, 0.22f, 0.08f, a), new Color(1f, 0.86f, 0.5f, a), inner);
        });
        GenSprite("WhitePixel", 4, 4, (u, v) => Color.white, new Vector4(1, 1, 1, 1));
        GenSprite("Mist", 256, 128, (u, v) =>
        {
            float n = Mathf.PerlinNoise(u * 4f + 3.1f, v * 3f + 1.7f) * 0.6f + Mathf.PerlinNoise(u * 9f, v * 7f) * 0.4f;
            float edge = Mathf.Sin(Mathf.PI * u) * Mathf.Sin(Mathf.PI * v);
            return new Color(1, 1, 1, Mathf.Clamp01(n * edge * 1.6f - 0.15f));
        });
        AssetDatabase.Refresh();
    }

    public static Sprite Gen(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{GenDir}/{name}.png");

    public static Sprite GuiSprite(string file, int index = 0)
    {
        return AssetDatabase.LoadAllAssetsAtPath(Gui + file).OfType<Sprite>()
            .OrderBy(s => s.name.Length).ThenBy(s => s.name).Skip(index).FirstOrDefault();
    }

    /// <summary>Sets 9-slice borders on the first sprite of a GUI sheet.</summary>
    public static void SetBorder(string file, Vector4 border)
    {
        string path = Gui + file;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath(path));
        provider.InitSpriteEditorDataProvider();
        var rects = provider.GetSpriteRects();
        foreach (var r in rects)
            r.border = border;
        provider.SetSpriteRects(rects);
        provider.Apply();
        ((TextureImporter)AssetImporter.GetAtPath(path)).SaveAndReimport();
    }

    public static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

    public static Material FontMaterial(string name, System.Action<Material> setup)
    {
        EnsureDir(GenDir + "/Fonts");
        string path = $"{GenDir}/Fonts/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Font.material);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = Font.material.shader;
            mat.CopyPropertiesFromMaterial(Font.material);
        }

        mat.shaderKeywords = Font.material.shaderKeywords;
        setup(mat);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static Material TitleMat => FontMaterial("Title", m =>
    {
        m.SetColor("_OutlineColor", new Color(0.12f, 0.06f, 0.02f, 1f));
        m.SetFloat("_OutlineWidth", 0.16f);
        m.SetFloat("_FaceDilate", 0.08f);
        m.EnableKeyword("UNDERLAY_ON");
        m.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.85f));
        m.SetFloat("_UnderlayOffsetX", 0.25f);
        m.SetFloat("_UnderlayOffsetY", -0.45f);
        m.SetFloat("_UnderlayDilate", 0.35f);
        m.SetFloat("_UnderlaySoftness", 0.6f);
        m.EnableKeyword("GLOW_ON");
        m.SetColor("_GlowColor", new Color(1f, 0.55f, 0.15f, 0.35f));
        m.SetFloat("_GlowOuter", 0.45f);
        m.SetFloat("_GlowPower", 0.7f);
    });

    public static Material BodyMat => FontMaterial("Body", m =>
    {
        m.SetColor("_OutlineColor", new Color(0.03f, 0.02f, 0.03f, 1f));
        m.SetFloat("_OutlineWidth", 0.12f);
        m.EnableKeyword("UNDERLAY_ON");
        m.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.7f));
        m.SetFloat("_UnderlayOffsetY", -0.5f);
        m.SetFloat("_UnderlayDilate", 0.2f);
        m.SetFloat("_UnderlaySoftness", 0.4f);
    });

    // ---------------- UI ----------------
    public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(string name, Transform parent)
    {
        var rt = Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        return rt;
    }

    public static Image Img(RectTransform rt, Sprite sprite, Color color, bool raycast = false, Image.Type type = Image.Type.Simple, float ppuMul = 1f)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        img.type = sprite != null && sprite.border != Vector4.zero && type == Image.Type.Simple ? Image.Type.Sliced : type;
        img.pixelsPerUnitMultiplier = ppuMul;
        return img;
    }

    public static TextMeshProUGUI Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align, Material mat = null, FontStyles style = FontStyles.Normal, float spacing = 0f)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = Font;
        if (mat != null)
            t.fontSharedMaterial = mat;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.characterSpacing = spacing;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    public static Canvas MakeCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        canvas.pixelPerfect = false;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    /// <summary>Text-style menu button with arrow cursor, glow band and MenuButton feedback.</summary>
    public static Button MenuButton(string name, Transform parent, string label, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        var rt = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), pos, size);
        var hit = Img(rt, Gen("WhitePixel"), new Color(0, 0, 0, 0), true);

        var glowRt = Rect("Glow", rt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(-30, 0), new Vector2(60, 6));
        glowRt.anchorMin = new Vector2(0, 0); glowRt.anchorMax = new Vector2(1, 1); glowRt.pivot = new Vector2(0.5f, 0.5f);
        glowRt.offsetMin = new Vector2(-40, -6); glowRt.offsetMax = new Vector2(40, 6);
        var glow = Img(glowRt, Gen("HBand"), new Color(0.78f, 0.40f, 0.18f, 0f));

        var arrowRt = Rect("Cursor", rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(-6, 0), new Vector2(42, 28));
        var arrow = Img(arrowRt, GuiSprite("20250425rightArrow-Sheet.png"), Color.white);
        arrow.preserveAspect = true;
        arrowRt.gameObject.SetActive(false);

        var textRt = Rect("Label", rt, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        textRt.offsetMin = new Vector2(18, 0); textRt.offsetMax = new Vector2(0, 0);
        var text = Text(textRt, label, fontSize, Bone, align | TextAlignmentOptions.Midline, BodyMat, FontStyles.Bold | FontStyles.UpperCase, 6f);
        text.textWrappingMode = TextWrappingModes.NoWrap;

        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = hit;
        button.transition = Selectable.Transition.None;

        var mb = rt.gameObject.AddComponent<global::MenuButton>();
        var so = new SerializedObject(mb);
        so.FindProperty("label").objectReferenceValue = text;
        var markers = so.FindProperty("selectionMarkers");
        markers.arraySize = 1;
        markers.GetArrayElementAtIndex(0).objectReferenceValue = arrowRt.gameObject;
        so.FindProperty("glow").objectReferenceValue = glow;
        so.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    /// <summary>Framed panel using the GUI kit's ornate 9-slice.</summary>
    public static RectTransform Panel(string name, Transform parent, Vector2 size, string header, out CanvasGroup group)
    {
        var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), size);
        group = rt.gameObject.AddComponent<CanvasGroup>();

        var shadow = Rect("Shadow", rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        shadow.offsetMin = new Vector2(-60, -70); shadow.offsetMax = new Vector2(60, 50);
        Img(shadow, Gen("SoftCircle"), new Color(0, 0, 0, 0.75f));

        var backing = Stretch("Backing", rt);
        backing.offsetMin = new Vector2(18, 18); backing.offsetMax = new Vector2(-18, -18);
        Img(backing, Gen("WhitePixel"), new Color(0.03f, 0.025f, 0.06f, 0.92f));
        var frame = Stretch("Frame", rt);
        Img(frame, GuiSprite("20250420manaSoul9SlicesA-Sheet.png"), new Color(0.9f, 0.88f, 0.95f, 0.9f), false, Image.Type.Sliced, 2.1f);

        if (!string.IsNullOrEmpty(header))
        {
            var h = Rect("Header", rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(520, 88));
            Img(h, GuiSprite("20250420manaSoulHeaderA-Sheet.png"), Color.white, false, Image.Type.Sliced, 2.1f);
            var ht = Rect("Title", h, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0, 4), Vector2.zero);
            Text(ht, header, 34, new Color(0.25f, 0.12f, 0.05f), TextAlignmentOptions.Center, null, FontStyles.Bold | FontStyles.UpperCase, 10f);
        }

        return rt;
    }

    public static Slider MakeSlider(string name, Transform parent, Vector2 pos, Vector2 size, TMP_Text rowLabel, bool showTrack = true)
    {
        var rt = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), pos, size);
        var hit = Img(rt, Gen("WhitePixel"), new Color(0, 0, 0, 0), true);

        var bg = Rect("Track", rt, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 14));
        Img(bg, Gen("WhitePixel"), new Color(0.55f, 0.42f, 0.22f, showTrack ? 0.7f : 0f));
        var bgInner = Rect("Inner", bg, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4, -4));
        Img(bgInner, Gen("WhitePixel"), new Color(0.05f, 0.04f, 0.08f, showTrack ? 1f : 0f));

        var fillArea = Rect("Fill Area", rt, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 10));
        var fill = Rect("Fill", fillArea, Vector2.zero, new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        Img(fill, Gen("WhitePixel"), showTrack ? new Color(0.92f, 0.66f, 0.3f, 1f) : new Color(0, 0, 0, 0));

        var handleArea = Rect("Handle Slide Area", rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20, 0));
        var handle = Rect("Handle", handleArea, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 0));
        var hImg = Img(handle, Gen("Diamond"), showTrack ? Color.white : new Color(1, 1, 1, 0));
        hImg.preserveAspect = true;

        var slider = rt.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = hit;
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0; slider.maxValue = 1;

        var mb = rt.gameObject.AddComponent<global::MenuButton>();
        var so = new SerializedObject(mb);
        so.FindProperty("label").objectReferenceValue = rowLabel;
        so.FindProperty("selectedScale").floatValue = 1f;
        so.FindProperty("labelSlide").floatValue = 0f;
        so.FindProperty("silent").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
        return slider;
    }

    public static void SetVerticalNav(Selectable[] items, bool wrap = true)
    {
        for (int i = 0; i < items.Length; i++)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            Selectable up = i > 0 ? items[i - 1] : wrap ? items[items.Length - 1] : null;
            Selectable down = i < items.Length - 1 ? items[i + 1] : wrap ? items[0] : null;
            nav.selectOnUp = up;
            nav.selectOnDown = down;
            if (!(items[i] is Slider))
            {
                nav.selectOnLeft = null;
                nav.selectOnRight = null;
            }
            items[i].navigation = nav;
        }
    }

    // ---------------- WORLD ----------------
    public static void ApplyLightToAllLayers(Light2D light)
    {
        var so = new SerializedObject(light);
        var prop = so.FindProperty("m_ApplyToSortingLayers");
        prop.arraySize = SortingLayer.layers.Length;
        for (int i = 0; i < SortingLayer.layers.Length; i++)
            prop.GetArrayElementAtIndex(i).intValue = SortingLayer.layers[i].id;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static Light2D Light(string name, Transform parent, Light2D.LightType type, Vector3 pos, Color color, float intensity, float outer = 6f, float inner = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light2D>();
        l.lightType = type;
        l.color = color;
        l.intensity = intensity;
        if (type == Light2D.LightType.Point)
        {
            l.pointLightOuterRadius = outer;
            l.pointLightInnerRadius = inner;
            l.falloffIntensity = 0.7f;
        }

        ApplyLightToAllLayers(l);
        return l;
    }

    public static VolumeProfile Profile(string name, float vignette, float bloom, float contrast, float saturation, Color tint)
    {
        EnsureDir(GenDir);
        string path = $"{GenDir}/{name}.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        foreach (var c in profile.components.ToArray())
        {
            profile.Remove(c.GetType());
            Object.DestroyImmediate(c, true);
        }

        var v = profile.Add<Vignette>(true);
        v.intensity.Override(vignette);
        v.smoothness.Override(0.45f);
        v.color.Override(new Color(0.02f, 0.01f, 0.04f));
        var b = profile.Add<Bloom>(true);
        b.threshold.Override(0.85f);
        b.intensity.Override(bloom);
        b.scatter.Override(0.65f);
        var ca = profile.Add<ColorAdjustments>(true);
        ca.contrast.Override(contrast);
        ca.saturation.Override(saturation);
        ca.colorFilter.Override(tint);
        var fg = profile.Add<FilmGrain>(true);
        fg.type.Override(FilmGrainLookup.Thin1);
        fg.intensity.Override(0.18f);

        foreach (var c in profile.components)
        {
            if (!AssetDatabase.IsSubAsset(c))
                AssetDatabase.AddObjectToAsset(c, profile);
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    public static Volume GlobalVolume(VolumeProfile profile)
    {
        var go = new GameObject("Post Processing");
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
        return vol;
    }

    public static ParticleSystem Particles(string name, Transform parent, Vector3 pos, Sprite sprite, string sortingLayer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var r = go.GetComponent<ParticleSystemRenderer>();
        var mat = AssetDatabase.LoadAssetAtPath<Material>($"{GenDir}/ParticleSoft.mat");
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(mat, $"{GenDir}/ParticleSoft.mat");
        }

        r.sharedMaterial = mat;
        r.sortingLayerName = sortingLayer;
        r.sortingOrder = order;
        var tsa = ps.textureSheetAnimation;
        tsa.enabled = true;
        tsa.mode = ParticleSystemAnimationMode.Sprites;
        tsa.SetSprite(0, sprite);
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        return ps;
    }

    public static void Fog(ParticleSystem ps, Vector3 boxSize, float rate, Color color, Vector2 sizeRange, float speed)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(14f, 22f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startColor = color;
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        main.maxParticles = 200;
        var em = ps.emission;
        em.rateOverTime = rate;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = boxSize;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        vel.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.05f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
        col.color = g;
    }

    public static void Embers(ParticleSystem ps, Vector3 boxSize, float rate, Color a, Color b, float size)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.maxParticles = 300;
        var em = ps.emission;
        em.rateOverTime = rate;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = boxSize;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0.05f, 0.35f);
        vel.y = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.35f;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0, 1) });
        col.color = g;
    }
}

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
        es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

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
            "Attack  <b>J</b>        Roll  <b>Left Shift</b>        Block  <b>Right Mouse</b>        Heal  <b>H</b>        Pause  <b>Esc</b></size>\n\n" +
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
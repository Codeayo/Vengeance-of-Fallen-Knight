using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
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

/// <summary>Builds Assets/Prefab/UI/GameHUD.prefab: the shared in-level HUD, menus and inventory.</summary>
public static class BuildHud
{
    public const string PrefabPath = "Assets/Prefab/UI/GameHUD.prefab";

    public static string Build()
    {
        UIKit.EnsureGenerated();
        var go = BuildCanvas();
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        return "saved " + PrefabPath;
    }

    static void Assign(Object target, params (string prop, Object value)[] pairs)
    {
        var so = new SerializedObject(target);
        foreach (var (prop, value) in pairs)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError("Missing property " + prop + " on " + target.GetType().Name); continue; }
            p.objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static GameObject BuildCanvas()
    {

        var heart = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Royal Heart").FirstOrDefault() ?? "")).OfType<Sprite>().FirstOrDefault();
        var flask = ItemDatabase.Get("crimson_flask").icon;
        var coinSprite = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Coin.prefab").GetComponentInChildren<SpriteRenderer>().sprite))
            .OfType<Sprite>().FirstOrDefault(s => s.name == "animated_items_4") ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Coin.prefab").GetComponentInChildren<SpriteRenderer>().sprite;

        var canvas = UIKit.MakeCanvas("Game HUD", 20);
        var ct = canvas.transform;
        var hud = canvas.gameObject.AddComponent<GameHUD>();

        // --- Health cluster (top-left)
        var cluster = UIKit.Rect("Status", ct, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -30), new Vector2(620, 150));
        var plate = UIKit.Rect("Plate", cluster, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(-20, 14), new Vector2(660, 170));
        UIKit.Img(plate, UIKit.Gen("HFadeRight"), new Color(0, 0, 0, 0.55f));
        var frame = UIKit.Rect("Health Frame", cluster, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(84, -18), new Vector2(460, 44));
        UIKit.Img(frame, UIKit.Gen("WhitePixel"), new Color(0.55f, 0.42f, 0.22f, 0.95f));
        var inner = UIKit.Rect("Inner", frame, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6, -6));
        UIKit.Img(inner, UIKit.Gen("WhitePixel"), new Color(0.06f, 0.03f, 0.05f, 1f));
        var trail = UIKit.Rect("Trail", inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4, -4));
        var trailImg = UIKit.Img(trail, UIKit.Gen("WhitePixel"), new Color(1f, 0.9f, 0.75f, 0.85f), false, Image.Type.Filled);
        trailImg.fillMethod = Image.FillMethod.Horizontal;
        var fill = UIKit.Rect("Fill", inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4, -4));
        var fillImg = UIKit.Img(fill, UIKit.Gen("WhitePixel"), new Color(0.75f, 0.12f, 0.12f), false, Image.Type.Filled);
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        var shine = UIKit.Rect("Shine", inner, new Vector2(0, 0.55f), Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4, -2));
        UIKit.Img(shine, UIKit.Gen("WhitePixel"), new Color(1, 1, 1, 0.08f));
        var hpText = UIKit.Text(UIKit.Rect("Value", frame, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0, 1), Vector2.zero),
            "100 / 100", 24, Color.white, TextAlignmentOptions.Center, UIKit.BodyMat, FontStyles.Bold);
        var heartRt = UIKit.Rect("Heart", cluster, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(46, -40), new Vector2(84, 84));
        var hImg = UIKit.Img(heartRt, heart, Color.white);
        hImg.preserveAspect = true;
        UIKit.Text(UIKit.Rect("Name", cluster, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(88, -16), new Vector2(460, 30)),
            "THE FALLEN KNIGHT", 20, UIKit.Gold, TextAlignmentOptions.BottomLeft, UIKit.BodyMat, FontStyles.Bold, 6f);

        var coinRt = UIKit.Rect("Coin Icon", cluster, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(110, -98), new Vector2(44, 44));
        UIKit.Img(coinRt, coinSprite, Color.white).preserveAspect = true;
        var coinText = UIKit.Text(UIKit.Rect("Coins", cluster, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(140, -98), new Vector2(140, 40)),
            "0", 30, new Color(1f, 0.88f, 0.55f), TextAlignmentOptions.MidlineLeft, UIKit.BodyMat, FontStyles.Bold);
        var flaskRt = UIKit.Rect("Flask Icon", cluster, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(262, -98), new Vector2(44, 44));
        UIKit.Img(flaskRt, flask, Color.white).preserveAspect = true;
        var flaskText = UIKit.Text(UIKit.Rect("Flasks", cluster, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(290, -98), new Vector2(200, 40)),
            "x1", 30, new Color(0.75f, 0.95f, 0.85f), TextAlignmentOptions.MidlineLeft, UIKit.BodyMat, FontStyles.Bold);
        UIKit.Text(UIKit.Rect("Flask Key", cluster, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(345, -98), new Vector2(200, 40)),
            "<color=#E8C27A>[H]</color> Heal", 20, UIKit.Ash, TextAlignmentOptions.MidlineLeft, UIKit.BodyMat);

        // --- Area title
        var area = UIKit.Rect("Area Title", ct, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1400, 200));
        var areaGroup = area.gameObject.AddComponent<CanvasGroup>();
        var areaTitle = UIKit.Text(UIKit.Rect("Title", area, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 90)),
            "The Forsaken Graveyard", 68, Color.white, TextAlignmentOptions.Top, UIKit.TitleMat, FontStyles.Bold | FontStyles.SmallCaps, 6f);
        areaTitle.enableVertexGradient = true;
        areaTitle.colorGradient = new VertexGradient(new Color(1f, 0.93f, 0.72f), new Color(1f, 0.93f, 0.72f), new Color(0.88f, 0.58f, 0.25f), new Color(0.88f, 0.58f, 0.25f));
        var lineL = UIKit.Rect("Line", area, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -104), new Vector2(700, 4));
        UIKit.Img(lineL, UIKit.Gen("HBand"), new Color(1f, 0.75f, 0.4f, 0.9f));
        var areaSub = UIKit.Text(UIKit.Rect("Subtitle", area, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -118), new Vector2(0, 50)),
            "Where the fallen knight was buried", 28, UIKit.Bone, TextAlignmentOptions.Top, UIKit.BodyMat, FontStyles.Italic, 2f);

        // --- Toast
        var toast = UIKit.Rect("Toast", ct, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 190), new Vector2(1100, 60));
        var toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        UIKit.Img(UIKit.Stretch("Band", toast), UIKit.Gen("HBand"), new Color(0, 0, 0, 0.75f));
        var toastText = UIKit.Text(UIKit.Rect("Text", toast, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero),
            "Shrine kindled", 30, UIKit.Bone, TextAlignmentOptions.Center, UIKit.BodyMat, FontStyles.Italic, 2f);

        // --- Controls hint
        var hint = UIKit.Rect("Controls", ct, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1500, 50));
        var hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
        UIKit.Img(UIKit.Stretch("Band", hint), UIKit.Gen("HBand"), new Color(0, 0, 0, 0.6f));
        UIKit.Text(UIKit.Rect("Text", hint, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero),
            "<color=#E8C27A>A/D</color> Move    <color=#E8C27A>Space</color> Jump (x2)    <color=#E8C27A>J</color> Attack    <color=#E8C27A>Shift</color> Roll    <color=#E8C27A>Right Mouse</color> Block    <color=#E8C27A>H</color> Heal    <color=#E8C27A>Esc</color> Pause",
            24, UIKit.Bone, TextAlignmentOptions.Center, UIKit.BodyMat);

        // --- Boss bar
        var boss = UIKit.Rect("Boss Bar", ct, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(1000, 70));
        var bossGroup = boss.gameObject.AddComponent<CanvasGroup>();
        var bossName = UIKit.Text(UIKit.Rect("Name", boss, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0), new Vector2(0, -26), new Vector2(0, 40)),
            "The Gravewarden", 30, new Color(1f, 0.78f, 0.7f), TextAlignmentOptions.Bottom, UIKit.BodyMat, FontStyles.Bold | FontStyles.SmallCaps, 6f);
        var bFrame = UIKit.Rect("Frame", boss, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(0, 26));
        UIKit.Img(bFrame, UIKit.Gen("WhitePixel"), new Color(0.5f, 0.38f, 0.2f, 0.95f));
        var bInner = UIKit.Rect("Inner", bFrame, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6, -6));
        UIKit.Img(bInner, UIKit.Gen("WhitePixel"), new Color(0.05f, 0.02f, 0.03f, 1f));
        var bTrail = UIKit.Img(UIKit.Stretch("Trail", bInner), UIKit.Gen("WhitePixel"), new Color(1f, 0.85f, 0.65f, 0.8f), false, Image.Type.Filled);
        bTrail.fillMethod = Image.FillMethod.Horizontal;
        var bFill = UIKit.Img(UIKit.Stretch("Fill", bInner), UIKit.Gen("WhitePixel"), new Color(0.6f, 0.08f, 0.1f), false, Image.Type.Filled);
        bFill.fillMethod = Image.FillMethod.Horizontal;

        // --- Respawn fade
        var fade = UIKit.Stretch("Screen Fade", ct);
        var fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
        UIKit.Img(fade, UIKit.Gen("WhitePixel"), new Color(0.02f, 0.01f, 0.03f, 1f));

        // --- Game over
        var over = UIKit.Stretch("Game Over", ct);
        var overGroup = over.gameObject.AddComponent<CanvasGroup>();
        UIKit.Img(over, UIKit.Gen("WhitePixel"), new Color(0.05f, 0.0f, 0.01f, 0.78f), true);
        var overTitle = UIKit.Text(UIKit.Rect("Title", over, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1600, 160)),
            "YOU HAVE FALLEN", 110, Color.white, TextAlignmentOptions.Center, UIKit.TitleMat, FontStyles.Bold, 18f);
        overTitle.enableVertexGradient = true;
        overTitle.colorGradient = new VertexGradient(new Color(1f, 0.55f, 0.45f), new Color(1f, 0.55f, 0.45f), new Color(0.6f, 0.08f, 0.08f), new Color(0.6f, 0.08f, 0.08f));
        UIKit.Text(UIKit.Rect("Subtitle", over, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1200, 50)),
            "Your vengeance is not yet complete.", 32, UIKit.Bone, TextAlignmentOptions.Center, UIKit.BodyMat, FontStyles.Italic);
        var btnHolder = UIKit.Rect("Buttons", over, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(520, 200));
        var rise = UIKit.MenuButton("Rise Again", btnHolder, "Rise Again", new Vector2(110, -40), new Vector2(400, 64), 40);
        var toMenu = UIKit.MenuButton("Main Menu", btnHolder, "Main Menu", new Vector2(110, -120), new Vector2(400, 64), 40);
        UIKit.SetVerticalNav(new Selectable[] { rise, toMenu });

        // --- Pause
        var pauseRoot = UIKit.Stretch("Pause", ct);
        var pauseGroup = pauseRoot.gameObject.AddComponent<CanvasGroup>();
        UIKit.Img(pauseRoot, UIKit.Gen("WhitePixel"), new Color(0.01f, 0.01f, 0.03f, 0.72f), true);
        var pp = UIKit.Panel("Panel", pauseRoot, new Vector2(820, 640), "Paused", out var _);
        var resume = UIKit.MenuButton("Resume", pp, "Resume", new Vector2(130, -130), new Vector2(560, 60), 36);
        (Slider, TMP_Text) Row(string name, float y)
        {
            var label = UIKit.Text(UIKit.Rect(name + " Label", pp, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(150, y), new Vector2(240, 50)),
                name, 26, UIKit.Bone, TextAlignmentOptions.MidlineLeft, UIKit.BodyMat, FontStyles.Bold | FontStyles.UpperCase, 4f);
            var slider = UIKit.MakeSlider(name + " Slider", pp, new Vector2(370, y), new Vector2(260, 44), label);
            var val = UIKit.Text(UIKit.Rect(name + " Value", pp, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(650, y), new Vector2(120, 50)),
                "50%", 24, UIKit.Gold, TextAlignmentOptions.MidlineLeft, UIKit.BodyMat, FontStyles.Bold);
            return (slider, val);
        }
        var (masterS, masterV) = Row("Master", -205);
        var (mSlider, mVal) = Row("Music", -263);
        var (sSlider, sVal) = Row("Effects", -321);
        var (aSlider, aVal) = Row("Ambience", -379);
        var pMenu = UIKit.MenuButton("Main Menu", pp, "Main Menu", new Vector2(130, -465), new Vector2(560, 60), 36);
        var pQuit = UIKit.MenuButton("Quit", pp, "Quit Game", new Vector2(130, -535), new Vector2(560, 60), 36);
        UIKit.SetVerticalNav(new Selectable[] { resume, masterS, mSlider, sSlider, aSlider, pMenu, pQuit });
        var pause = canvas.gameObject.AddComponent<PauseMenu>();
        var pso = new SerializedObject(pause);
        pso.FindProperty("panel").objectReferenceValue = pauseGroup;
        pso.FindProperty("resumeButton").objectReferenceValue = resume;
        pso.FindProperty("mainMenuButton").objectReferenceValue = pMenu;
        pso.FindProperty("quitButton").objectReferenceValue = pQuit;
        pso.FindProperty("musicSlider").objectReferenceValue = mSlider;
        pso.FindProperty("sfxSlider").objectReferenceValue = sSlider;
        pso.FindProperty("masterSlider").objectReferenceValue = masterS;
        pso.FindProperty("ambientSlider").objectReferenceValue = aSlider;
        pso.FindProperty("masterValue").objectReferenceValue = masterV;
        pso.FindProperty("ambientValue").objectReferenceValue = aVal;
        pso.FindProperty("musicValue").objectReferenceValue = mVal;
        pso.FindProperty("sfxValue").objectReferenceValue = sVal;
        pso.ApplyModifiedPropertiesWithoutUndo();

        // Health colour: healthy crimson -> dark red, pulses brighter when low.
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(new Color(0.95f, 0.2f, 0.12f), 0f), new GradientColorKey(new Color(0.78f, 0.12f, 0.12f), 0.35f), new GradientColorKey(new Color(0.72f, 0.1f, 0.12f), 1f) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });

        var so = new SerializedObject(hud);
        so.FindProperty("healthFill").objectReferenceValue = fillImg;
        so.FindProperty("healthTrail").objectReferenceValue = trailImg;
        so.FindProperty("healthText").objectReferenceValue = hpText;
        so.FindProperty("healthFrame").objectReferenceValue = frame;
        so.FindProperty("coinText").objectReferenceValue = coinText;
        so.FindProperty("flaskText").objectReferenceValue = flaskText;
        so.FindProperty("areaTitleGroup").objectReferenceValue = areaGroup;
        so.FindProperty("areaTitle").objectReferenceValue = areaTitle;
        so.FindProperty("areaSubtitle").objectReferenceValue = areaSub;
        so.FindProperty("toastGroup").objectReferenceValue = toastGroup;
        so.FindProperty("toastText").objectReferenceValue = toastText;
        so.FindProperty("controlsHint").objectReferenceValue = hintGroup;
        so.FindProperty("screenFade").objectReferenceValue = fadeGroup;
        so.FindProperty("gameOverGroup").objectReferenceValue = overGroup;
        so.FindProperty("riseAgainButton").objectReferenceValue = rise;
        so.FindProperty("gameOverMenuButton").objectReferenceValue = toMenu;
        so.FindProperty("bossGroup").objectReferenceValue = bossGroup;
        so.FindProperty("bossFill").objectReferenceValue = bFill;
        so.FindProperty("bossTrail").objectReferenceValue = bTrail;
        so.FindProperty("bossName").objectReferenceValue = bossName;
        so.ApplyModifiedPropertiesWithoutUndo();
        hud.GetType().GetField("healthColors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hud, grad);
        EditorUtility.SetDirty(hud);

        // Hidden-at-start groups
        foreach (var g in new[] { areaGroup, toastGroup, hintGroup, bossGroup, fadeGroup, overGroup, pauseGroup })
        {
            g.alpha = 0f;
            g.interactable = false;
            g.blocksRaycasts = false;
        }
        pauseRoot.gameObject.SetActive(false);
        Extras(canvas, hud);
        return canvas.gameObject;
    }

    static void Extras(Canvas canvas, GameHUD hud)
    {
        var ct = canvas.transform;
        var cluster = ct.Find("Status");

        // --- Level + experience
        var lvl = UIKit.Text(UIKit.Rect("Level", cluster, new Vector2(0, 1), new Vector2(0, 1), new Vector2(1, 0), new Vector2(544, -16), new Vector2(140, 30)),
            "LV 1", 22, new Color(1f, 0.88f, 0.55f), TextAlignmentOptions.BottomRight, UIKit.BodyMat, FontStyles.Bold, 2f);
        var xpBack = UIKit.Rect("XP Bar", cluster, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(84, -66), new Vector2(460, 8));
        UIKit.Img(xpBack, UIKit.Gen("WhitePixel"), new Color(0.05f, 0.04f, 0.08f, 0.95f));
        var xpFill = UIKit.Img(UIKit.Stretch("Fill", xpBack), UIKit.Gen("WhitePixel"), new Color(0.95f, 0.75f, 0.35f), false, Image.Type.Filled);
        xpFill.fillMethod = Image.FillMethod.Horizontal;
        xpFill.fillAmount = 0f;

        // --- Pickup feed (bottom right)
        var feed = UIKit.Rect("Pickup Feed", ct, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 250), new Vector2(520, 340));
        var vlg = feed.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.LowerRight;
        vlg.spacing = 6;
        vlg.childControlHeight = false; vlg.childControlWidth = false; vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = false;
        var row = UIKit.Rect("Feed Row", feed, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(500, 48));
        row.gameObject.AddComponent<CanvasGroup>();
        UIKit.Img(UIKit.Stretch("Band", row), UIKit.Gen("HBand"), new Color(0, 0, 0, 0.7f));
        var ri = UIKit.Img(UIKit.Rect("Icon", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(40, 40)), null, Color.white);
        ri.preserveAspect = true;
        UIKit.Text(UIKit.Rect("Label", row, new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-72, 0), new Vector2(-80, 0)),
            "Crimson Flask", 24, UIKit.Bone, TextAlignmentOptions.MidlineRight, UIKit.BodyMat, FontStyles.Bold);
        row.gameObject.SetActive(false);

        // --- Interaction prompt
        var prompt = UIKit.Rect("Prompt", ct, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 270), new Vector2(700, 54));
        var promptGroup = prompt.gameObject.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0f; promptGroup.blocksRaycasts = false;
        UIKit.Img(UIKit.Stretch("Band", prompt), UIKit.Gen("HBand"), new Color(0, 0, 0, 0.75f));
        var promptText = UIKit.Text(UIKit.Rect("Text", prompt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero),
            "[E] Read", 28, UIKit.Bone, TextAlignmentOptions.Center, UIKit.BodyMat, FontStyles.Bold);

        Assign(hud, ("xpFill", xpFill), ("levelText", lvl), ("feedRoot", feed), ("feedTemplate", row.gameObject), ("promptGroup", promptGroup), ("promptText", promptText));

        BuildCinematic(canvas);
        BuildReadPanel(canvas);
        BuildShrineMenu(canvas);
        BuildInventory(canvas);
        // Fade, Game Over and Pause stay on top of everything.
        ct.Find("Screen Fade").SetAsLastSibling();
        ct.Find("Game Over").SetAsLastSibling();
        ct.Find("Pause").SetAsLastSibling();
        ct.Find("Cinematic").SetAsLastSibling();
    }

    static void BuildCinematic(Canvas canvas)
    {
        var root = UIKit.Stretch("Cinematic", canvas.transform);
        var black = UIKit.Stretch("Black", root);
        var blackGroup = black.gameObject.AddComponent<CanvasGroup>();
        UIKit.Img(black, UIKit.Gen("WhitePixel"), new Color(0.01f, 0.01f, 0.02f, 1f));
        var top = UIKit.Rect("Letterbox Top", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 0));
        UIKit.Img(top, UIKit.Gen("WhitePixel"), Color.black);
        var bottom = UIKit.Rect("Letterbox Bottom", root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 0));
        UIKit.Img(bottom, UIKit.Gen("WhitePixel"), Color.black);

        var title = UIKit.Rect("Title Card", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1700, 300));
        var titleGroup = title.gameObject.AddComponent<CanvasGroup>();
        var main = UIKit.Text(UIKit.Rect("Main", title, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0), new Vector2(0, -10), new Vector2(0, 150)),
            "VENGEANCE OF THE FALLEN KNIGHT", 92, Color.white, TextAlignmentOptions.Bottom, UIKit.TitleMat, FontStyles.Bold, 14f);
        main.enableVertexGradient = true;
        main.colorGradient = new VertexGradient(new Color(1f, 0.93f, 0.72f), new Color(1f, 0.93f, 0.72f), new Color(0.86f, 0.5f, 0.18f), new Color(0.86f, 0.5f, 0.18f));
        main.textWrappingMode = TextWrappingModes.NoWrap;
        main.enableAutoSizing = true;
        main.fontSizeMax = 92;
        main.fontSizeMin = 44;
        var line = UIKit.Rect("Rule", title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -26), new Vector2(820, 5));
        UIKit.Img(line, UIKit.Gen("HBand"), new Color(1f, 0.72f, 0.35f, 0.9f));
        var sub = UIKit.Text(UIKit.Rect("Sub", title, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(0, 110)),
            "", 34, UIKit.Bone, TextAlignmentOptions.Top, UIKit.BodyMat, FontStyles.Italic, 3f);

        var subt = UIKit.Rect("Subtitles", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(1500, 190));
        var subtGroup = subt.gameObject.AddComponent<CanvasGroup>();
        UIKit.Img(UIKit.Stretch("Band", subt), UIKit.Gen("HBand"), new Color(0, 0, 0, 0.7f));
        var speaker = UIKit.Text(UIKit.Rect("Speaker", subt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(0, 44)),
            "The Watcher", 30, new Color(0.65f, 0.9f, 1f), TextAlignmentOptions.Top, UIKit.BodyMat, FontStyles.Bold | FontStyles.SmallCaps, 6f);
        var text = UIKit.Text(UIKit.Rect("Line", subt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -62), new Vector2(-160, 120)),
            "", 32, UIKit.Bone, TextAlignmentOptions.Top, UIKit.BodyMat, FontStyles.Italic);

        var skip = UIKit.Rect("Skip Hint", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 30), new Vector2(600, 40));
        var skipGroup = skip.gameObject.AddComponent<CanvasGroup>();
        UIKit.Text(skip, "Hold <color=#E8C27A>Esc</color> / <color=#E8C27A>Enter</color> / <color=#E8C27A>Start</color> to skip", 22, UIKit.Ash, TextAlignmentOptions.BottomRight, UIKit.BodyMat);

        foreach (var g in new[] { blackGroup, titleGroup, subtGroup, skipGroup }) { g.alpha = 0f; g.blocksRaycasts = false; g.interactable = false; }
        var c = canvas.gameObject.AddComponent<Cinematic>();
        Assign(c, ("topBar", top), ("bottomBar", bottom), ("titleGroup", titleGroup), ("titleMain", main), ("titleSub", sub),
            ("subtitleGroup", subtGroup), ("speakerText", speaker), ("lineText", text), ("black", blackGroup), ("skipHint", skipGroup));
    }

    static CanvasGroup Overlay(Transform parent, string name)
    {
        var root = UIKit.Stretch(name, parent);
        var g = root.gameObject.AddComponent<CanvasGroup>();
        UIKit.Img(root, UIKit.Gen("WhitePixel"), new Color(0.01f, 0.01f, 0.03f, 0.75f), true);
        g.alpha = 0f; g.interactable = false; g.blocksRaycasts = false;
        return g;
    }

    static void BuildReadPanel(Canvas canvas)
    {
        var g = Overlay(canvas.transform, "Read Panel");
        var p = UIKit.Panel("Panel", g.transform, new Vector2(1100, 620), "Inscription", out _);
        var title = p.Find("Header/Title").GetComponent<TMP_Text>();
        var body = UIKit.Text(UIKit.Rect("Body", p, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(-160, 420)),
            "", 30, UIKit.Bone, TextAlignmentOptions.Top, UIKit.BodyMat, FontStyles.Italic);
        body.lineSpacing = 10;
        UIKit.Text(UIKit.Rect("Hint", p, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(800, 36)),
            "<color=#E8C27A>[E]</color> or <color=#E8C27A>Esc</color> to close", 22, UIKit.Ash, TextAlignmentOptions.Center, UIKit.BodyMat);
        var rp = canvas.gameObject.AddComponent<ReadPanel>();
        Assign(rp, ("group", g), ("titleText", title), ("bodyText", body));
    }

    static void BuildShrineMenu(Canvas canvas)
    {
        var g = Overlay(canvas.transform, "Shrine Menu");
        var p = UIKit.Panel("Panel", g.transform, new Vector2(1000, 600), "Shrine", out _);
        var status = UIKit.Text(UIKit.Rect("Status", p, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(-140, 80)),
            "", 26, UIKit.Bone, TextAlignmentOptions.Top, UIKit.BodyMat, FontStyles.Italic);
        var temper = UIKit.MenuButton("Temper", p, "Temper Blade", new Vector2(110, -230), new Vector2(760, 60), 34);
        var temperDetail = UIKit.Text(UIKit.Rect("Temper Detail", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(130, -262), new Vector2(760, 34)),
            "", 22, UIKit.Ash, TextAlignmentOptions.TopLeft, UIKit.BodyMat);
        var reinforce = UIKit.MenuButton("Reinforce", p, "Reinforce Armor", new Vector2(110, -340), new Vector2(760, 60), 34);
        var reinforceDetail = UIKit.Text(UIKit.Rect("Reinforce Detail", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(130, -372), new Vector2(760, 34)),
            "", 22, UIKit.Ash, TextAlignmentOptions.TopLeft, UIKit.BodyMat);
        var leave = UIKit.MenuButton("Leave", p, "Leave", new Vector2(110, -460), new Vector2(760, 60), 34);
        UIKit.SetVerticalNav(new Selectable[] { temper, reinforce, leave });
        var sm = canvas.gameObject.AddComponent<ShrineMenu>();
        Assign(sm, ("group", g), ("temperButton", temper), ("reinforceButton", reinforce), ("leaveButton", leave),
            ("temperDetail", temperDetail), ("reinforceDetail", reinforceDetail), ("statusText", status));
    }

    static void BuildInventory(Canvas canvas)
    {
        var g = Overlay(canvas.transform, "Inventory");
        var p = UIKit.Panel("Panel", g.transform, new Vector2(1640, 880), "Inventory", out _);
        p.anchoredPosition = new Vector2(0, -10);

        // Character column
        UIKit.Text(UIKit.Rect("Character Title", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -100), new Vector2(380, 40)),
            "SIR ALDRIC", 30, UIKit.Gold, TextAlignmentOptions.TopLeft, UIKit.BodyMat, FontStyles.Bold, 6f);
        var stats = UIKit.Text(UIKit.Rect("Stats", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -150), new Vector2(380, 300)),
            "", 26, UIKit.Ash, TextAlignmentOptions.TopLeft, UIKit.BodyMat);
        stats.lineSpacing = 6;
        UIKit.Text(UIKit.Rect("Equipped Title", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -470), new Vector2(380, 40)),
            "EQUIPPED", 24, UIKit.Gold, TextAlignmentOptions.TopLeft, UIKit.BodyMat, FontStyles.Bold, 6f);
        Image Equip(string n, float y)
        {
            var frame = UIKit.Rect(n + " Frame", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, y), new Vector2(64, 64));
            UIKit.Img(frame, UIKit.Gen("WhitePixel"), new Color(0.1f, 0.08f, 0.14f, 0.95f));
            var i = UIKit.Img(UIKit.Rect("Icon", frame, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8, -8)), null, Color.white);
            i.preserveAspect = true;
            return i;
        }
        var wIcon = Equip("Weapon", -512);
        var aIcon = Equip("Armor", -586);
        var cIcon = Equip("Charm", -660);
        var eqText = UIKit.Text(UIKit.Rect("Equipment", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -528), new Vector2(320, 230)),
            "", 24, UIKit.Bone, TextAlignmentOptions.TopLeft, UIKit.BodyMat);
        eqText.lineSpacing = 52;

        // Grid
        var gridRt = UIKit.Rect("Grid", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(500, -110), new Vector2(672, 560));
        var gl = gridRt.gameObject.AddComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(100, 100);
        gl.spacing = new Vector2(12, 12);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = 6;
        var slotRt = UIKit.Rect("Slot", gridRt, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
        var frameImg = UIKit.Img(slotRt, UIKit.Gen("WhitePixel"), new Color(0.1f, 0.08f, 0.14f, 0.95f), true);
        var edge = UIKit.Img(UIKit.Rect("Rarity", slotRt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 5)), UIKit.Gen("WhitePixel"), Color.gray);
        var icon = UIKit.Img(UIKit.Rect("Icon", slotRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(-26, -26)), null, Color.white);
        icon.preserveAspect = true;
        var count = UIKit.Text(UIKit.Rect("Count", slotRt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-6, 6), new Vector2(60, 30)),
            "", 22, Color.white, TextAlignmentOptions.BottomRight, UIKit.BodyMat, FontStyles.Bold);
        var mark = UIKit.Rect("Equipped", slotRt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(4, -4), new Vector2(22, 22));
        UIKit.Img(mark, UIKit.Gen("Diamond"), Color.white);
        var sel = slotRt.gameObject.AddComponent<Button>();
        sel.transition = Selectable.Transition.None;
        sel.targetGraphic = frameImg;
        var slot = slotRt.gameObject.AddComponent<InventorySlot>();
        slot.frame = frameImg; slot.icon = icon; slot.count = count; slot.equippedMark = mark.gameObject; slot.rarityEdge = edge;

        // Details
        var dIcon = UIKit.Img(UIKit.Rect("Detail Icon", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(1220, -110), new Vector2(130, 130)), null, Color.white);
        dIcon.preserveAspect = true;
        var dName = UIKit.Text(UIKit.Rect("Detail Name", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(1220, -255), new Vector2(360, 50)),
            "", 32, UIKit.Bone, TextAlignmentOptions.TopLeft, UIKit.BodyMat, FontStyles.Bold);
        var dMeta = UIKit.Text(UIKit.Rect("Detail Meta", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(1220, -300), new Vector2(360, 34)),
            "", 22, UIKit.Ash, TextAlignmentOptions.TopLeft, UIKit.BodyMat, FontStyles.Italic);
        var dDesc = UIKit.Text(UIKit.Rect("Detail Description", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(1220, -345), new Vector2(360, 200)),
            "", 23, UIKit.Bone, TextAlignmentOptions.TopLeft, UIKit.BodyMat);
        var dEff = UIKit.Text(UIKit.Rect("Detail Effect", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(1220, -555), new Vector2(360, 80)),
            "", 22, new Color(0.75f, 0.95f, 0.8f), TextAlignmentOptions.TopLeft, UIKit.BodyMat, FontStyles.Bold);
        var use = UIKit.MenuButton("Use", p, "Use", new Vector2(1240, -660), new Vector2(300, 56), 30);
        var useLabel = use.transform.Find("Label").GetComponent<TMP_Text>();
        var discard = UIKit.MenuButton("Discard", p, "Discard", new Vector2(1240, -726), new Vector2(300, 56), 30);
        var msg = UIKit.Text(UIKit.Rect("Message", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(500, -690), new Vector2(680, 40)),
            "", 24, new Color(1f, 0.88f, 0.55f), TextAlignmentOptions.TopLeft, UIKit.BodyMat, FontStyles.Italic);
        UIKit.Text(UIKit.Rect("Hint", p, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1400, 36)),
            "<color=#E8C27A>[I]</color> Close     <color=#E8C27A>Enter</color> Use / Equip     <color=#E8C27A>Arrows</color> Browse     Mouse and gamepad supported",
            22, UIKit.Ash, TextAlignmentOptions.Center, UIKit.BodyMat);

        var inv = canvas.gameObject.AddComponent<InventoryUI>();
        Assign(inv, ("group", g), ("grid", gridRt), ("slotTemplate", slot), ("detailIcon", dIcon), ("detailName", dName), ("detailMeta", dMeta),
            ("detailDescription", dDesc), ("detailEffect", dEff), ("useButton", use), ("useLabel", useLabel), ("discardButton", discard), ("messageText", msg),
            ("statsText", stats), ("weaponIcon", wIcon), ("armorIcon", aIcon), ("charmIcon", cIcon), ("equipmentText", eqText));
    }
}
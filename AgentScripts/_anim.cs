using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Animations;
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

/// <summary>Rebuilds the knight's and enemies' Animator Controllers with complete, responsive state machines.</summary>
public static class BuildAnimators
{
    const string OutDir = "Assets/Animations/Generated";
    static StringBuilder log;

    static Sprite[] Frames(string png) =>
        AssetDatabase.LoadAllAssetsAtPath(png).OfType<Sprite>()
            .OrderBy(s => { var p = s.name.Split('_'); return int.TryParse(p.Last(), out int n) ? n : 0; }).ToArray();

    /// <summary>Creates (or replaces) a sprite clip from frames.</summary>
    static AnimationClip Clip(string name, Sprite[] frames, float fps, bool loop, string target = "")
    {
        UIKit.EnsureDir(OutDir);
        string path = $"{OutDir}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.ClearCurves();
        clip.frameRate = fps;
        var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = target, propertyName = "m_Sprite" };
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static void SetLoop(AnimationClip c, bool loop)
    {
        var s = AnimationUtility.GetAnimationClipSettings(c);
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(c, s);
        EditorUtility.SetDirty(c);
    }

    static AnimationClip Existing(string path) => AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

    // ---------------- controller helpers ----------------
    static AnimatorController Reset(string path, params (string name, AnimatorControllerParameterType type)[] ps)
    {
        var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (c == null) c = AnimatorController.CreateAnimatorControllerAtPath(path);
        foreach (var p in c.parameters.ToArray()) c.RemoveParameter(p);
        foreach (var (n, t) in ps) c.AddParameter(n, t);
        var sm = c.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
        foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
        foreach (var t in sm.entryTransitions.ToArray()) sm.RemoveEntryTransition(t);
        return c;
    }

    static AnimatorState State(AnimatorController c, string name, Motion m, float speed = 1f)
    {
        var s = c.layers[0].stateMachine.AddState(name);
        s.motion = m;
        s.speed = speed;
        s.writeDefaultValues = false;
        return s;
    }

    static AnimatorStateTransition T(AnimatorState from, AnimatorState to, float exitTime = -1f)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = exitTime >= 0f;
        if (exitTime >= 0f) t.exitTime = exitTime;
        t.duration = 0f;
        t.hasFixedDuration = true;
        return t;
    }

    static AnimatorStateTransition Any(AnimatorController c, AnimatorState to, bool canSelf = false)
    {
        var t = c.layers[0].stateMachine.AddAnyStateTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        t.hasFixedDuration = true;
        t.canTransitionToSelf = canSelf;
        return t;
    }

    const AnimatorConditionMode If = AnimatorConditionMode.If, IfNot = AnimatorConditionMode.IfNot,
        Gt = AnimatorConditionMode.Greater, Lt = AnimatorConditionMode.Less, Eq = AnimatorConditionMode.Equals;
    const AnimatorControllerParameterType B = AnimatorControllerParameterType.Bool, Tr = AnimatorControllerParameterType.Trigger,
        F = AnimatorControllerParameterType.Float, I = AnimatorControllerParameterType.Int;

    // ---------------- knight ----------------
    static void Knight()
    {
        string dir = "Assets/Players/SoulslikeKnight/SoulslikeKnight_SpriteSheets/";
        var idle = Existing(dir + "Idle.anim"); SetLoop(idle, true);
        var run = Existing(dir + "Run.anim"); SetLoop(run, true);
        var a1 = Existing(dir + "Attack1.anim"); SetLoop(a1, false);
        var a2 = Existing(dir + "Attack2.anim"); SetLoop(a2, false);
        var a3 = Existing(dir + "Attack3.anim"); SetLoop(a3, false);
        var block = Existing(dir + "Block.anim"); SetLoop(block, true);
        var heal = Existing(dir + "Heal.anim"); SetLoop(heal, false);
        var hurt = Existing(dir + "Hurt.anim"); SetLoop(hurt, false);
        var roll = Existing(dir + "Roll.anim"); SetLoop(roll, false);
        var jumpFrames = Frames(dir + "SoulslikeKnight_Jump.png");
        var rise = Clip("Knight_JumpRise", jumpFrames.Take(2).ToArray(), 8, false);
        var fall = Clip("Knight_Fall", new[] { jumpFrames.Last() }, 8, true);
        var deathFrames = Frames(dir + "SoulslikeKnight_Death.png");
        var death = Clip("Knight_Death", deathFrames, 9, false);
        var resurrect = Clip("Knight_Resurrect", deathFrames.Reverse().ToArray(), 6, false);

        var c = Reset(dir + "Player.controller",
            ("IsRunning", B), ("IsGrounded", B), ("YVel", F), ("IsBlocking", B), ("IsHealing", B), ("IsDead", B),
            ("Attack", Tr), ("CombpStep", I), ("Roll", Tr), ("Jump", Tr), ("Hurt", Tr), ("Death", Tr), ("Rise", Tr));
        var sm = c.layers[0].stateMachine;
        var sIdle = State(c, "Idle", idle); sm.defaultState = sIdle;
        var sRun = State(c, "Run", run);
        var sJump = State(c, "Jump", rise);
        var sFall = State(c, "Fall", fall);
        var sA1 = State(c, "Attack1", a1, 1.2f);
        var sA2 = State(c, "Attack2", a2, 1.2f);
        var sA3 = State(c, "Attack3", a3, 1.1f);
        var sBlock = State(c, "Block", block);
        var sHeal = State(c, "Heal", heal);
        var sHurt = State(c, "Hurt", hurt, 1.3f);
        var sRoll = State(c, "Roll", roll, 1.15f);
        var sDeath = State(c, "Death", death);
        var sRise = State(c, "Resurrect", resurrect);

        // Highest priority first.
        Any(c, sDeath).AddCondition(If, 0, "IsDead");
        var r = Any(c, sRise); r.AddCondition(If, 0, "Rise");
        var h = Any(c, sHurt); h.AddCondition(If, 0, "Hurt"); h.AddCondition(IfNot, 0, "IsDead");
        var ro = Any(c, sRoll); ro.AddCondition(If, 0, "Roll"); ro.AddCondition(IfNot, 0, "IsDead");
        foreach (var (st, step) in new[] { (sA1, 0), (sA2, 1), (sA3, 2) })
        {
            var t = Any(c, st, true); t.AddCondition(If, 0, "Attack"); t.AddCondition(Eq, step, "CombpStep"); t.AddCondition(IfNot, 0, "IsDead");
        }
        var j = Any(c, sJump); j.AddCondition(If, 0, "Jump"); j.AddCondition(IfNot, 0, "IsDead");
        var bl = Any(c, sBlock); bl.AddCondition(If, 0, "IsBlocking"); bl.AddCondition(If, 0, "IsGrounded"); bl.AddCondition(IfNot, 0, "IsDead");
        var he = Any(c, sHeal); he.AddCondition(If, 0, "IsHealing"); he.AddCondition(IfNot, 0, "IsDead");

        T(sIdle, sRun).AddCondition(If, 0, "IsRunning");
        T(sRun, sIdle).AddCondition(IfNot, 0, "IsRunning");
        foreach (var s in new[] { sIdle, sRun }) { var f = T(s, sFall); f.AddCondition(IfNot, 0, "IsGrounded"); f.AddCondition(Lt, -1.5f, "YVel"); }
        T(sJump, sFall).AddCondition(Lt, 0f, "YVel");
        foreach (var s in new[] { sJump, sFall })
        {
            var l1 = T(s, sRun); l1.AddCondition(If, 0, "IsGrounded"); l1.AddCondition(If, 0, "IsRunning");
            var l2 = T(s, sIdle); l2.AddCondition(If, 0, "IsGrounded"); l2.AddCondition(IfNot, 0, "IsRunning");
        }
        foreach (var s in new[] { sA1, sA2, sA3, sHurt, sRoll, sRise, sHeal }) T(s, sIdle, 0.95f);
        T(sBlock, sIdle).AddCondition(IfNot, 0, "IsBlocking");
        EditorUtility.SetDirty(c);
        log.AppendLine("knight: 13 states (idle, run, jump, fall, 3-hit combo, block, heal, hurt, roll, death, resurrect)");
    }

    // ---------------- shield skeleton ----------------
    static void Skeleton()
    {
        string dir = "Assets/Enemy/SKeleton/Skeleton_Warrior/";
        var idle = Clip("Skel_Idle", Frames(dir + "Idle.png"), 8, true);
        var walk = Clip("Skel_Walk", Frames(dir + "Walk.png"), 9, true);
        var run = Clip("Skel_Run", Frames(dir + "Run.png"), 12, true);
        var a1 = Clip("Skel_Attack1", Frames(dir + "Attack_1.png"), 11, false);
        var a2 = Clip("Skel_Attack2", Frames(dir + "Attack_2.png"), 12, false);
        var a3 = Clip("Skel_Attack3", Frames(dir + "Attack_3.png"), 11, false);
        var runAtk = Clip("Skel_RunAttack", Frames(dir + "Run+attack.png"), 12, false);
        var hurt = Clip("Skel_Hurt", Frames(dir + "Hurt.png"), 8, false);
        var dead = Clip("Skel_Death", Frames(dir + "Dead.png"), 7, false);
        var protect = Clip("Skel_Protect", Frames(dir + "Protect.png"), 4, true);

        var c = Reset("Assets/Animations/PatrolSkeleton.controller",
            ("Speed", F), ("Attack", Tr), ("AttackIndex", I), ("Hurt", Tr), ("Dead", B), ("Protect", B),
            ("PlayerDetected", B), ("PlayerInRange", B));
        var sm = c.layers[0].stateMachine;
        var sIdle = State(c, "Idle", idle); sm.defaultState = sIdle;
        var sWalk = State(c, "Walk", walk);
        var sRun = State(c, "Run", run);
        var sDeath = State(c, "Death", dead);
        var sHurt = State(c, "Hurt", hurt);
        var sProt = State(c, "Protect", protect);
        Any(c, sDeath).AddCondition(If, 0, "Dead");
        var h = Any(c, sHurt); h.AddCondition(If, 0, "Hurt"); h.AddCondition(IfNot, 0, "Dead");
        int i = 0;
        foreach (var m in new Motion[] { a1, a2, a3, runAtk })
        {
            var s = State(c, "Attack" + (i + 1), m);
            var t = Any(c, s); t.AddCondition(If, 0, "Attack"); t.AddCondition(Eq, i, "AttackIndex"); t.AddCondition(IfNot, 0, "Dead");
            T(s, sIdle, 0.96f);
            i++;
        }
        var p = Any(c, sProt); p.AddCondition(If, 0, "Protect"); p.AddCondition(IfNot, 0, "Dead");
        T(sProt, sIdle).AddCondition(IfNot, 0, "Protect");
        T(sHurt, sIdle, 0.95f);
        T(sIdle, sWalk).AddCondition(Gt, 0.1f, "Speed");
        var wr = T(sWalk, sRun); wr.AddCondition(Gt, 1.9f, "Speed");
        T(sWalk, sIdle).AddCondition(Lt, 0.1f, "Speed");
        T(sRun, sWalk).AddCondition(Lt, 1.9f, "Speed");
        T(sIdle, sRun).AddCondition(Gt, 1.9f, "Speed");
        EditorUtility.SetDirty(c);
        log.AppendLine("skeleton: idle, walk, run, 3 attacks + run-attack, hurt, protect, death");
    }

    // ---------------- axe skeleton ----------------
    static void Axe()
    {
        string dir = "Assets/Enemy/Skeleton 1/Sprite Sheets/";
        var idle = Clip("Axe_Idle", Frames(dir + "Skeleton Idle.png"), 10, true, "enemy_spirtes");
        var walk = Clip("Axe_Walk", Frames(dir + "Skeleton Walk.png"), 13, true, "enemy_spirtes");
        var atk = Clip("Axe_Attack", Frames(dir + "Skeleton Attack.png"), 16, true, "enemy_spirtes");
        var hit = Clip("Axe_Hit", Frames(dir + "Skeleton Hit.png"), 14, false, "enemy_spirtes");
        var dead = Clip("Axe_Death", Frames(dir + "Skeleton Dead.png"), 13, false, "enemy_spirtes");
        var react = Clip("Axe_React", Frames(dir + "Skeleton React.png"), 10, false, "enemy_spirtes");

        var c = Reset("Assets/Animations/EnemyAnimation/Enemy.controller",
            ("canWalk", B), ("Attack", B), ("Hurt", Tr), ("Death", Tr), ("Alert", Tr));
        var sm = c.layers[0].stateMachine;
        var sIdle = State(c, "Idle", idle); sm.defaultState = sIdle;
        var sWalk = State(c, "Walk", walk);
        var sAtk = State(c, "Attack", atk);
        var sHit = State(c, "Hit", hit);
        var sDead = State(c, "Death", dead);
        var sReact = State(c, "Alert", react);
        Any(c, sDead).AddCondition(If, 0, "Death");
        Any(c, sHit).AddCondition(If, 0, "Hurt");
        Any(c, sReact).AddCondition(If, 0, "Alert");
        T(sIdle, sWalk).AddCondition(If, 0, "canWalk");
        T(sWalk, sIdle).AddCondition(IfNot, 0, "canWalk");
        T(sIdle, sAtk).AddCondition(If, 0, "Attack");
        T(sWalk, sAtk).AddCondition(If, 0, "Attack");
        T(sAtk, sIdle).AddCondition(IfNot, 0, "Attack");
        T(sHit, sIdle, 0.95f);
        T(sReact, sIdle, 0.95f);
        EditorUtility.SetDirty(c);
        log.AppendLine("axe skeleton: idle, walk, attack, hit, alert, death (from the unused sprite sheets)");
    }

    // ---------------- ninja ----------------
    static void Ninja()
    {
        string dir = "Assets/Enemy/YellowNinja/";
        var idle = Clip("Ninja_Idle", Frames(dir + "yellowNinja - idle.png"), 10, true);
        var walk = Existing(dir + "YellowNinjaWalk.anim"); SetLoop(walk, true);
        var atk = Existing(dir + "YellowNinjaAttack.anim"); SetLoop(atk, true);
        var hit = Existing(dir + "YellowNinjaHit.anim"); SetLoop(hit, false);
        var death = Existing(dir + "YellowNinjaDeath.anim"); SetLoop(death, false);

        var c = Reset(dir + "Enemy_1.controller", ("Attack", B), ("Hurt", Tr), ("Death", Tr), ("Speed", F));
        var sm = c.layers[0].stateMachine;
        var sIdle = State(c, "Idle", idle); sm.defaultState = sIdle;
        var sWalk = State(c, "Walk", walk);
        var sAtk = State(c, "Attack", atk, 1.25f);
        var sHit = State(c, "Hit", hit);
        var sDeath = State(c, "Death", death);
        Any(c, sDeath).AddCondition(If, 0, "Death");
        Any(c, sHit).AddCondition(If, 0, "Hurt");
        T(sIdle, sWalk).AddCondition(Gt, 0.1f, "Speed");
        T(sWalk, sIdle).AddCondition(Lt, 0.1f, "Speed");
        T(sIdle, sAtk).AddCondition(If, 0, "Attack");
        T(sWalk, sAtk).AddCondition(If, 0, "Attack");
        T(sAtk, sIdle).AddCondition(IfNot, 0, "Attack");
        var hw = T(sHit, sIdle, 0.95f);
        EditorUtility.SetDirty(c);
        log.AppendLine("ninja: idle (new), walk, attack, hit, death");
    }

    public static string Build()
    {
        log = new StringBuilder();
        Knight(); Skeleton(); Axe(); Ninja();
        AssetDatabase.SaveAssets();
        return log.ToString();
    }
}
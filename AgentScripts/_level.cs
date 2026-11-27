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
using Unity.Cinemachine;
using UnityEditor.SceneManagement;
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

/// <summary>Editor-time builder for level 1 (Graveyard). Idempotent: re-running replaces its own objects.</summary>
public static class BuildLevel
{
    const string ScenePath = "Assets/Scenes/Graveyard.unity";
    const string Root = "[L1] Level Design";
    static StringBuilder log;
    static Tilemap ground;
    static int groundMask;

    public static string Build()
    {
        log = new StringBuilder();
        UIKit.EnsureGenerated();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects().Where(g => g.name.StartsWith("[L1]")).ToArray())
            Object.DestroyImmediate(go);

        var root = new GameObject(Root).transform;
        ground = GameObject.Find("Grid/Ground1").GetComponent<Tilemap>();
        groundMask = LayerMask.GetMask("Ground");

        FixGroundPhysics();
        PaintBoundaryWalls();
        Physics2D.SyncTransforms();
        FixPlayer();
        FixSorting();
        FixSpikesAndPits(root);
        FixCamera();
        FixBackgrounds();
        SetupEnemies(root);
        BuildCheckpoints(root);
        BuildBanners(root);
        BuildTraps(root);
        BuildExit(root);
        BuildDecor(root);
        BuildAtmosphere(root);
        CleanupOldUI();
        BuildHud();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return log.ToString();
    }

    // ------------------------------------------------------------------ helpers
    static float GroundY(float x, float from = 60f)
    {
        var hit = Physics2D.Raycast(new Vector2(x, from), Vector2.down, 120f, groundMask);
        return hit ? hit.point.y : float.NaN;
    }

    static GameObject Child(Transform parent, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        return go;
    }

    static T Set<T>(T component, string prop, Object value) where T : Object
    {
        var so = new SerializedObject(component);
        so.FindProperty(prop).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        return component;
    }

    static void SetF(Object c, string prop, float v) { var so = new SerializedObject(c); so.FindProperty(prop).floatValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void SetI(Object c, string prop, int v) { var so = new SerializedObject(c); so.FindProperty(prop).intValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void SetS(Object c, string prop, string v) { var so = new SerializedObject(c); so.FindProperty(prop).stringValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }

    static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Vector3 pos, Vector3 scale, string layer, int order, Color? color = null)
    {
        var go = Child(parent, name, pos);
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        if (color.HasValue) sr.color = color.Value;
        return sr;
    }

    static Sprite TileSprite(string n) => AssetDatabase.LoadAssetAtPath<Tile>($"Assets/Art/Graveyard/Tile map/Tiles_{n}.asset")?.sprite;

    // ------------------------------------------------------------------ physics
    static void FixGroundPhysics()
    {
        var rb = ground.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var cc = ground.GetComponent<CompositeCollider2D>();
        cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
        cc.generationType = CompositeCollider2D.GenerationType.Synchronous;
        log.AppendLine("ground: static body, polygon composite");
    }

    static void PaintBoundaryWalls()
    {
        // Reference tiles from a flat column of the existing ground (top surface, second row, body fill).
        Vector3Int refTop = TopCell(50f);
        TileBase top = ground.GetTile(refTop);
        TileBase second = ground.GetTile(refTop + Vector3Int.down);
        TileBase body = ground.GetTile(refTop + Vector3Int.down * 4);

        void Wall(float x0, float x1, float baseX, int height)
        {
            // Base height comes from a neighbouring floor column so re-running never stacks walls.
            int baseY = TopCell(baseX).y;
            for (float x = x0; x <= x1; x += 1f)
            {
                int cx = ground.WorldToCell(new Vector3(x, 0, 0)).x;
                for (int y = baseY + 1; y <= baseY + 25; y++)
                    ground.SetTile(new Vector3Int(cx, y, 0), null);
                int topY = baseY + height;
                for (int y = baseY + 1; y <= topY; y++)
                    ground.SetTile(new Vector3Int(cx, y, 0), y == topY ? top : y == topY - 1 ? second : body);
            }
        }

        Wall(-4.6f, -2.6f, -1f, 9);     // west boundary, just behind the spawn
        Wall(436.5f, 439.5f, 434.5f, 9); // east boundary, behind the exit gate
        ground.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        ground.GetComponent<CompositeCollider2D>().GenerateGeometry();
        log.AppendLine("walls painted at both level edges");
    }

    static Vector3Int TopCell(float worldX)
    {
        var b = ground.cellBounds;
        int cx = ground.WorldToCell(new Vector3(worldX, 0, 0)).x;
        for (int y = b.yMax; y >= b.yMin; y--)
            if (ground.HasTile(new Vector3Int(cx, y, 0)))
                return new Vector3Int(cx, y, 0);
        return new Vector3Int(cx, b.yMin, 0);
    }

    static void FixPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        p.layer = LayerMask.NameToLayer("Player");
        var rb = p.GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        string matPath = "Assets/Art/Generated/PlayerNoFriction.physicsMaterial2D";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, matPath);
        }

        var box = p.GetComponent<BoxCollider2D>();
        box.sharedMaterial = mat;
        // Fit the body (sprite art is off-centre): 2.0 x 3.25 local, centred on the knight.
        box.size = new Vector2(2.0f, 3.25f);
        box.offset = new Vector2(0.92f, 0f);
        box.edgeRadius = 0f;

        if (p.GetComponent<FlipColliderSync>() == null)
            p.AddComponent<FlipColliderSync>();

        // Spawn: standing on the ground at the west end.
        float gy = GroundY(3f);
        p.transform.position = new Vector3(3f, gy + 1.63f, 0f);

        var player = p.GetComponent<Player>();
        var so = new SerializedObject(player);
        so.FindProperty("menuPanel").objectReferenceValue = null;
        so.FindProperty("healthSlider").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();
        Set(p.GetComponent<Player_Collectables>(), "currentCoin_Text", null);

        // Ground detection now uses the collider's feet (mirrors with the sprite), not a fixed child point.
        var gc = p.transform.Find("Groundcheck");
        if (gc != null)
            gc.gameObject.layer = 0;
        player.groundCheck = null;

        log.AppendLine($"player: layer Player, frictionless body, interpolated, spawn y={p.transform.position.y:F2}");
    }

    static void FixSorting()
    {
        void Layer(string path, string layer, int order)
        {
            var go = GameObject.Find(path);
            if (go == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.sortingLayerName = layer;
                r.sortingOrder = order;
            }
        }

        Layer("Grid/Clumns", "Backtiles", 2);
        Layer("Grid/Prop", "Backtiles", 3);
        Layer("Grid/Grill_tile", "Backtiles", 4);
        Layer("Grid/Bushes", "Backtiles", 1);
        Layer("Grid/Statues", "Backtiles", 0);
        log.AppendLine("sorting: pillars, fences, bushes and statues moved behind the player");
    }

    static void FixSpikesAndPits(Transform root)
    {
        var spikes = GameObject.Find("Grid/Spikes");
        foreach (var c in spikes.GetComponents<SpikeDamage>()) Object.DestroyImmediate(c);
        foreach (var c in spikes.GetComponents<BoxCollider2D>()) Object.DestroyImmediate(c);
        spikes.GetComponent<TilemapRenderer>().sortingLayerName = "Grill";

        var hazards = Child(root, "Hazards", Vector3.zero).transform;

        // Find pits from the ground profile: runs at least 4 units lower than both neighbours.
        var runs = new List<(float x0, float x1, float y)>();
        float runStart = -2f, runY = GroundY(-2f);
        for (float x = -1.5f; x <= 436f; x += 0.5f)
        {
            float y = GroundY(x);
            if (float.IsNaN(y) || Mathf.Abs(y - runY) > 0.5f)
            {
                runs.Add((runStart, x - 0.5f, runY));
                runStart = x;
                runY = y;
            }
        }
        runs.Add((runStart, 436f, runY));

        int pits = 0;
        for (int i = 1; i < runs.Count - 1; i++)
        {
            var r = runs[i];
            float rim = Mathf.Min(runs[i - 1].y, runs[i + 1].y);
            if (r.y < rim - 4f)
            {
                float w = r.x1 - r.x0 + 0.5f;
                var zone = Child(hazards, $"Spike Pit {++pits}", new Vector3((r.x0 + r.x1) * 0.5f, (r.y + rim - 2f) * 0.5f, 0));
                zone.layer = LayerMask.NameToLayer("Ignore Raycast");
                var box = zone.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(w, rim - 2f - r.y + 1f);
                var hz = zone.AddComponent<HazardZone>();
                SetI(hz, "damage", 20);
                log.AppendLine($"pit {pits}: x {r.x0:F1}-{r.x1:F1} floor {r.y:F1} rim {rim:F1}");
            }
        }

        var abyss = Child(hazards, "Abyss (out of bounds)", new Vector3(218f, -18f, 0));
        abyss.layer = LayerMask.NameToLayer("Ignore Raycast");
        var ab = abyss.AddComponent<BoxCollider2D>();
        ab.isTrigger = true;
        ab.size = new Vector2(480f, 6f);
        var abz = abyss.AddComponent<HazardZone>();
        SetI(abz, "damage", 25);
        SetS(abz, "message", "Swallowed by the abyss");
    }

    static void FixCamera()
    {
        var cm = Object.FindFirstObjectByType<CinemachineCamera>();
        var lens = cm.Lens;
        lens.OrthographicSize = 8.5f;
        cm.Lens = lens;

        var confiner = GameObject.Find("Camera/confiner").GetComponent<PolygonCollider2D>();
        confiner.isTrigger = true;
        confiner.offset = Vector2.zero;
        var t = confiner.transform;
        Vector2 L(float x, float y) => t.InverseTransformPoint(new Vector3(x, y, 0));
        confiner.pathCount = 1;
        confiner.SetPath(0, new[] { L(-3.5f, -9.5f), L(440f, -9.5f), L(440f, 33f), L(-3.5f, 33f) });
        var conf2d = cm.GetComponent<CinemachineConfiner2D>();
        if (conf2d != null) conf2d.InvalidateBoundingShapeCache();

        var pos = cm.GetComponent<CinemachinePositionComposer>();
        if (pos != null)
        {
            pos.Damping = new Vector3(0.6f, 0.9f, 0f);
            var comp = pos.Composition;
            comp.ScreenPosition = new Vector2(-0.08f, 0.2f);
            pos.Composition = comp;
            pos.Lookahead.Enabled = true;
            pos.Lookahead.Time = 0.25f;
            pos.Lookahead.Smoothing = 8f;
        }

        var cam = Camera.main;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.06f);
        log.AppendLine("camera: lens 8.5, confiner fitted to level walls, light look-ahead");
    }

    static void FixBackgrounds()
    {
        var bg1 = GameObject.Find("BackGrounds/BG1");
        var bg2 = GameObject.Find("BackGrounds/BG2");
        foreach (Transform c in bg1.transform) { var p = c.localPosition; c.localPosition = new Vector3(p.x, 0f, p.z); }
        foreach (Transform c in bg2.transform) { var p = c.localPosition; c.localPosition = new Vector3(p.x, 0f, p.z); }

        var f1 = bg1.GetComponent<ParallaxFollow>(); if (f1 == null) f1 = bg1.AddComponent<ParallaxFollow>();
        SetF(f1, "followX", 0.88f); SetF(f1, "followY", 0.92f);
        var f2 = bg2.GetComponent<ParallaxFollow>(); if (f2 == null) f2 = bg2.AddComponent<ParallaxFollow>();
        SetF(f2, "followX", 0.7f); SetF(f2, "followY", 0.82f);
        log.AppendLine("backgrounds: aligned panels, camera-relative parallax (no seams as the path climbs)");
    }

    // ------------------------------------------------------------------ enemies
    static GameObject gravewarden, guardA, guardB;

    static void SetupEnemies(Transform root)
    {
        var parentGo = GameObject.Find("Ememy") ?? GameObject.Find("Enemies");
        var parent = parentGo.transform;
        parent.name = "Enemies";
        foreach (string n in new[] { "Crypt Guard (West)", "Crypt Guard (East)", "The Gravewarden" })
        {
            var old = parent.Find(n);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        // First encounter: lone skeleton on the plateau after the second spike pit.
        var skel = (parent.Find("PatrolSkeleton") ?? parent.Find("Restless Skeleton")).gameObject;
        ConfigureSkeleton(skel, "Restless Skeleton", 136f, new Vector2(1.15f, 1.2f), 50, 10, 3.4f, 7f);

        // Second encounter: the axe skeleton on the next plateau.
        var axe = (parent.Find("Enemy") ?? parent.Find("Axe Skeleton")).gameObject;
        axe.name = "Axe Skeleton";
        float gy = GroundY(165f);
        axe.transform.position = new Vector3(165f, gy + 1.4f, 0);
        axe.layer = LayerMask.NameToLayer("Enemy");
        foreach (Transform c in axe.transform) c.gameObject.layer = LayerMask.NameToLayer("Enemy");
        var hitbox = axe.transform.Find("hitbox");
        if (hitbox != null) hitbox.GetComponent<Collider2D>().isTrigger = true; // weapon box should not shove the player
        var det = axe.transform.Find("Detection")?.gameObject ?? new GameObject("Detection");
        det.transform.SetParent(axe.transform, false);
        det.transform.localPosition = Vector3.zero;
        det.layer = LayerMask.NameToLayer("Ignore Raycast");
        var circle = det.GetComponent<CircleCollider2D>(); if (circle == null) circle = det.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 6.5f;
        var eb = axe.GetComponent<Enemy_behaviour>();
        eb.raycaskMask = LayerMask.GetMask("Player");
        eb.rayCastLength = 7f;
        eb.attackDistance = 3.5f;
        eb.moveSpeed = 2.2f;
        eb.maxHealth = 40;
        if (axe.GetComponent<DamageFlash>() == null) axe.AddComponent<DamageFlash>();

        // Final encounter: the Gravewarden and two guards.
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemies/Prefabs/PatrolSkeleton.prefab");
        guardA = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        ConfigureSkeleton(guardA, "Crypt Guard (West)", 362f, new Vector2(1.15f, 1.2f), 50, 10, 3.4f, 8f);
        guardB = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        ConfigureSkeleton(guardB, "Crypt Guard (East)", 381f, new Vector2(1.15f, 1.2f), 50, 10, 3.4f, 8f);
        guardB.GetComponent<SpriteRenderer>().flipX = true;
        gravewarden = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        ConfigureSkeleton(gravewarden, "The Gravewarden", 374f, new Vector2(1.7f, 1.8f), 170, 18, 4.3f, 10f);
        var gsr = gravewarden.GetComponent<SpriteRenderer>();
        gsr.color = new Color(1f, 0.72f, 0.68f);
        gsr.flipX = true;
        var gai = gravewarden.GetComponent<PatrolSkeletonAI>();
        gai.patrolSpeed = 0.8f;
        gai.chaseSpeed = 2.6f;
        gai.attackCooldown = 1.3f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(gai);
        PrefabUtility.RecordPrefabInstancePropertyModifications(gsr);
        var glow = UIKit.Light("Warden Aura", gravewarden.transform, Light2D.LightType.Point, gravewarden.transform.position + Vector3.up * 2f, new Color(1f, 0.25f, 0.2f), 0.9f, 5f, 0.5f);

        var arena = Child(root, "Boss Arena Trigger", new Vector3(359f, GroundY(359f) + 3f, 0));
        arena.layer = LayerMask.NameToLayer("Ignore Raycast");
        var ab = arena.AddComponent<BoxCollider2D>();
        ab.isTrigger = true;
        ab.size = new Vector2(6f, 10f);
        var enc = arena.AddComponent<BossEncounter>();
        Set(enc, "boss", gai);
        log.AppendLine("enemies: lone skeleton x136, axe skeleton x165 (now detects the player), final arena x356-385: Gravewarden + 2 guards");
    }

    static void ConfigureSkeleton(GameObject s, string name, float x, Vector2 scale, int hp, int dmg, float attackRange, float chaseRange)
    {
        s.name = name;
        s.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        s.layer = LayerMask.NameToLayer("Enemy");
        var box = s.GetComponent<BoxCollider2D>();
        // Fit to the skeleton's body (sprite has a wide shield / spear margin).
        box.size = new Vector2(1.1f, 3.8f);
        box.offset = new Vector2(0.4f, -0.05f);
        var sr = s.GetComponent<SpriteRenderer>();
        sr.sortingLayerName = "player";
        sr.sortingOrder = -1;
        float halfH = box.size.y * scale.y * 0.5f;
        float gy = GroundY(x);
        s.transform.position = new Vector3(x, gy + halfH - box.offset.y * scale.y + 0.02f, 0);
        var gc = s.transform.Find("GroundCheck");
        if (gc != null) gc.localPosition = new Vector3(1.0f, -box.size.y * 0.5f + box.offset.y, 0);

        var ai = s.GetComponent<PatrolSkeletonAI>();
        var so = new SerializedObject(ai);
        so.FindProperty("maxHealth").intValue = hp;
        so.ApplyModifiedPropertiesWithoutUndo();
        ai.attackDamage = dmg;
        ai.attackRange = attackRange;
        ai.chaseRange = chaseRange;
        ai.groundLayer = LayerMask.GetMask("Ground");
        ai.groundCheckDistance = 2.5f;
        var rb = s.GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (s.GetComponent<DamageFlash>() == null) s.AddComponent<DamageFlash>();
        SetF(s.GetComponent<DamageFlash>(), "numberHeight", box.size.y * scale.y * 0.6f);
        if (s.GetComponent<FlipColliderSync>() == null) s.AddComponent<FlipColliderSync>();

        // Prefab instances: direct field edits must be recorded as overrides or they revert on save.
        if (PrefabUtility.IsPartOfPrefabInstance(s))
        {
            foreach (Object o in new Object[] { s.transform, box, sr, ai, rb, s })
                PrefabUtility.RecordPrefabInstancePropertyModifications(o);
            if (gc != null) PrefabUtility.RecordPrefabInstancePropertyModifications(gc);
        }
    }

    // ------------------------------------------------------------------ checkpoints, banners, traps, exit
    static void BuildCheckpoints(Transform root)
    {
        var parent = Child(root, "Checkpoints", Vector3.zero).transform;
        Shrine(parent, "Shrine of the Fallen", 101f);
        Shrine(parent, "Shrine of the Mausoleum Steps", 199f);
        Shrine(parent, "Shrine before the Warden", 350.5f);
    }

    static void Shrine(Transform parent, string name, float x)
    {
        float gy = GroundY(x);
        var go = Child(parent, name, new Vector3(x, gy, 0));
        var statue = Sprite(go.transform, "Statue", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Graveyard/Salt.png"), new Vector3(x, gy + 3.0f, 0), new Vector3(3.0f, 3.0f, 1), "Backtiles", 6);
        var bowl = Sprite(go.transform, "Brazier", UIKit.Gen("Diamond"), new Vector3(x + 1.3f, gy + 0.35f, 0), new Vector3(0.6f, 0.35f, 1), "Backtiles", 7, new Color(0.35f, 0.3f, 0.28f));
        var glow = Sprite(go.transform, "Glow", UIKit.Gen("SoftCircle"), new Vector3(x + 1.3f, gy + 0.9f, 0), new Vector3(2.4f, 2.4f, 1), "Backtiles", 8, new Color(1f, 0.55f, 0.2f, 0.55f));
        var flame = UIKit.Particles("Flame", go.transform, new Vector3(x + 1.3f, gy + 0.55f, 0), UIKit.Gen("SoftCircle"), "Grill", 5);
        var main = flame.main;
        main.prewarm = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.15f, 0.9f), new Color(1f, 0.85f, 0.4f, 0.9f));
        main.startSpeed = 0f;
        var em = flame.emission; em.rateOverTime = 28f;
        var sh = flame.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(0.4f, 0.05f, 0.1f);
        var vel = flame.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f); vel.y = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var sz = flame.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.1f));
        var col = flame.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.85f, 0.5f), 0), new GradientColorKey(new Color(1f, 0.3f, 0.1f), 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
        col.color = g;

        var light = UIKit.Light("Flame Light", go.transform, Light2D.LightType.Point, new Vector3(x + 1.3f, gy + 1.2f, 0), new Color(1f, 0.6f, 0.3f), 0.25f, 6f, 0.4f);
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.offset = new Vector2(0.6f, 2f);
        box.size = new Vector2(3f, 4f);
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        var cp = go.AddComponent<Checkpoint>();
        var respawn = Child(go.transform, "Respawn Point", new Vector3(x + 2.6f, gy + 1.7f, 0));
        var so = new SerializedObject(cp);
        so.FindProperty("checkpointName").stringValue = name;
        so.FindProperty("respawnPoint").objectReferenceValue = respawn.transform;
        so.FindProperty("flameLight").objectReferenceValue = light;
        so.FindProperty("flameParticles").objectReferenceValue = flame;
        var gs = so.FindProperty("glowSprites");
        gs.arraySize = 1;
        gs.GetArrayElementAtIndex(0).objectReferenceValue = glow;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildBanners(Transform root)
    {
        var parent = Child(root, "Area Banners", Vector3.zero).transform;
        void Banner(float x, string title, string sub)
        {
            var go = Child(parent, title, new Vector3(x, GroundY(x) + 4f, 0));
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            var b = go.AddComponent<BoxCollider2D>();
            b.isTrigger = true;
            b.size = new Vector2(1f, 12f);
            var ab = go.AddComponent<AreaBanner>();
            SetS(ab, "title", title);
            SetS(ab, "subtitle", sub);
        }

        Banner(3.8f, "The Forsaken Graveyard", "Where the fallen knight was buried");
        Banner(123f, "The Ossuary Path", "The dead do not rest here");
        Banner(187f, "The Mausoleum Steps", "Mind the iron underfoot");
        Banner(354f, "The Gravewarden's Rest", "Its keeper guards the way out");
    }

    static void BuildTraps(Transform root)
    {
        var parent = Child(root, "Spike Traps", Vector3.zero).transform;
        Sprite[] tips = { TileSprite("216"), TileSprite("217"), TileSprite("218"), TileSprite("219") };
        Sprite[] bars = { TileSprite("234"), TileSprite("235"), TileSprite("236"), TileSprite("237") };

        void Trap(float x, float delay)
        {
            float gy = GroundY(x);
            var go = Child(parent, $"Spike Trap x{x:F0}", new Vector3(x, gy, 0));
            var plate = Sprite(go.transform, "Plate", UIKit.Gen("WhitePixel"), new Vector3(x, gy + 0.03f, 0), new Vector3(100f, 2.5f, 1f), "Ground", 5, new Color(0.18f, 0.14f, 0.12f));
            var spikes = Child(go.transform, "Spikes", new Vector3(x, gy - 1.55f, 0));
            for (int i = 0; i < 4; i++)
            {
                Sprite(spikes.transform, "Tip " + i, tips[i], new Vector3(x - 1.5f + i, gy - 0.05f, 0), Vector3.one, "Backtiles", 9);
                Sprite(spikes.transform, "Bar " + i, bars[i], new Vector3(x - 1.5f + i, gy - 1.05f, 0), Vector3.one, "Backtiles", 9);
            }
            spikes.transform.position = new Vector3(x, gy - 1.6f, 0);

            var dmg = Child(go.transform, "Damage Area", new Vector3(x, gy + 0.6f, 0));
            dmg.layer = LayerMask.NameToLayer("Ignore Raycast");
            var box = dmg.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(3.4f, 1.1f);
            var trap = dmg.AddComponent<SpikeTrap>();
            var so = new SerializedObject(trap);
            so.FindProperty("spikes").objectReferenceValue = spikes.transform;
            so.FindProperty("damageArea").objectReferenceValue = box;
            so.FindProperty("raisedHeight").floatValue = 1.55f;
            so.FindProperty("startDelay").floatValue = delay;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Trap(216f, 0f);
        Trap(229f, 1.2f);
        Trap(245.5f, 0.6f);
        Trap(286.5f, 0f);
        Trap(328.5f, 0.9f);
        log.AppendLine("traps: 5 telegraphed spike traps across the Mausoleum Steps");
    }

    static void BuildExit(Transform root)
    {
        float x = 432.5f;
        float gy = GroundY(x);
        var gate = Child(root, "Exit - Cursed Gate", new Vector3(x, gy, 0));
        var statue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Graveyard/Salt.png");
        Sprite(gate.transform, "Statue West", statue, new Vector3(x - 2.9f, gy + 3.3f, 0), new Vector3(3.3f, 3.3f, 1), "Backtiles", 6);
        var east = Sprite(gate.transform, "Statue East", statue, new Vector3(x + 2.9f, gy + 3.3f, 0), new Vector3(3.3f, 3.3f, 1), "Backtiles", 6);
        east.flipX = true;

        var sealedFx = Child(gate.transform, "Sealed", new Vector3(x, gy, 0));
        Sprite(sealedFx.transform, "Curse Glow", UIKit.Gen("SoftCircle"), new Vector3(x, gy + 2.5f, 0), new Vector3(4f, 7f, 1), "Backtiles", 7, new Color(0.7f, 0.1f, 0.15f, 0.55f));
        UIKit.Light("Curse Light", sealedFx.transform, Light2D.LightType.Point, new Vector3(x, gy + 2.5f, 0), new Color(1f, 0.2f, 0.25f), 1.1f, 5f, 0.5f);
        var curse = UIKit.Particles("Curse Mist", sealedFx.transform, new Vector3(x, gy + 0.3f, 0), UIKit.Gen("SoftCircle"), "Grill", 4);
        UIKit.Embers(curse, new Vector3(2.5f, 0.3f, 1f), 14f, new Color(0.8f, 0.1f, 0.2f, 0.8f), new Color(0.4f, 0.05f, 0.1f, 0.8f), 0.35f);

        var openFx = Child(gate.transform, "Open", new Vector3(x, gy, 0));
        Sprite(openFx.transform, "Beam", UIKit.Gen("SoftCircle"), new Vector3(x, gy + 4f, 0), new Vector3(3.2f, 12f, 1), "Backtiles", 7, new Color(1f, 0.82f, 0.45f, 0.65f));
        UIKit.Light("Gate Light", openFx.transform, Light2D.LightType.Point, new Vector3(x, gy + 2.5f, 0), new Color(1f, 0.85f, 0.55f), 1.8f, 7f, 1f);
        var motes = UIKit.Particles("Rising Light", openFx.transform, new Vector3(x, gy + 0.3f, 0), UIKit.Gen("SoftCircle"), "Grill", 4);
        UIKit.Embers(motes, new Vector3(2.5f, 0.3f, 1f), 18f, new Color(1f, 0.9f, 0.6f, 0.9f), new Color(1f, 0.7f, 0.3f, 0.9f), 0.25f);

        gate.layer = LayerMask.NameToLayer("Ignore Raycast");
        var box = gate.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.offset = new Vector2(0, 3f);
        box.size = new Vector2(2.6f, 6f);
        var exit = gate.AddComponent<LevelExit>();
        var so = new SerializedObject(exit);
        so.FindProperty("nextSceneName").stringValue = "Woodcamp";
        var req = so.FindProperty("requiredDefeated");
        req.arraySize = 3;
        req.GetArrayElementAtIndex(0).objectReferenceValue = gravewarden;
        req.GetArrayElementAtIndex(1).objectReferenceValue = guardA;
        req.GetArrayElementAtIndex(2).objectReferenceValue = guardB;
        so.FindProperty("sealedVisual").objectReferenceValue = sealedFx;
        so.FindProperty("openVisual").objectReferenceValue = openFx;
        so.ApplyModifiedPropertiesWithoutUndo();
        openFx.SetActive(false);
        log.AppendLine($"exit: cursed gate at x{x} (sealed until the final encounter is cleared) -> Woodcamp");
    }

    // ------------------------------------------------------------------ decoration & atmosphere
    static void BuildDecor(Transform root)
    {
        var parent = Child(root, "Decor", Vector3.zero).transform;
        var brush = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Graveyard/brush.png").OfType<Sprite>().ToArray();
        var grass1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Graveyard/Grass_background_1.png");
        var grass2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Graveyard/Grass_background_2.png");
        var statue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Graveyard/Salt.png");

        void Bush(float x, int i, float s = 1f) { float gy = GroundY(x); Sprite(parent, "Brush", brush[i % brush.Length], new Vector3(x, gy + 1.4f * s, 0), Vector3.one * s, "Backtiles", 1, new Color(0.75f, 0.78f, 0.88f)).flipX = i % 2 == 1; }
        void Grass(float x, Sprite sp, float s) { float gy = GroundY(x); var r = Sprite(parent, "Overgrowth", sp, new Vector3(x, gy + sp.bounds.extents.y * s - 0.2f, 0), Vector3.one * s, "Backtiles", 0, new Color(0.7f, 0.74f, 0.85f)); }
        void Statue(float x, float s, bool flip) { float gy = GroundY(x); var r = Sprite(parent, "Mourning Statue", statue, new Vector3(x, gy + 1f * s, 0), Vector3.one * s, "Backtiles", 0, new Color(0.8f, 0.82f, 0.9f)); r.flipX = flip; }

        // Late section was bare: graveyard overgrowth, statues and brush to frame the climb.
        float[] bushes = { 58f, 132f, 158f, 205f, 233f, 254f, 278f, 297f, 318f, 338f, 360f, 389f, 401f, 412f, 422f };
        for (int i = 0; i < bushes.Length; i++) Bush(bushes[i], i, i % 3 == 0 ? 1.1f : 0.85f);
        Grass(193f, grass1, 0.9f); Grass(276f, grass2, 0.8f); Grass(316f, grass1, 0.9f); Grass(392f, grass2, 0.8f); Grass(426f, grass1, 0.8f);
        Statue(213.5f, 3.2f, false); Statue(298f, 3.0f, true); Statue(343.5f, 3.2f, false); Statue(410f, 3.4f, true);

        // Foreground silhouettes at the bottom of the screen add depth without hiding the path.
        for (float x = 20f; x < 430f; x += 37f)
        {
            float gy = GroundY(x);
            Sprite(parent, "Foreground Brush", brush[(int)x % brush.Length], new Vector3(x, gy - 2.2f, 0), Vector3.one * 1.3f, "Grill", 30, new Color(0.03f, 0.03f, 0.05f)).flipX = ((int)x / 37) % 2 == 0;
        }
    }

    static void BuildAtmosphere(Transform root)
    {
        var fx = Child(root, "Atmosphere", Vector3.zero).transform;
        UIKit.Light("Moonlight (Global)", fx, Light2D.LightType.Global, Vector3.zero, new Color(0.7f, 0.76f, 0.98f), 0.78f);

        void FogBand(float x0, float x1, float y)
        {
            var back = UIKit.Particles($"Fog {x0:F0}-{x1:F0}", fx, new Vector3((x0 + x1) * 0.5f, y + 0.6f, 0), UIKit.Gen("Mist"), "Backtiles", 10);
            UIKit.Fog(back, new Vector3(x1 - x0, 1.6f, 1f), (x1 - x0) * 0.06f, new Color(0.55f, 0.62f, 0.8f, 0.12f), new Vector2(5f, 9f), 0.25f);
            var front = UIKit.Particles($"Low Mist {x0:F0}-{x1:F0}", fx, new Vector3((x0 + x1) * 0.5f, y - 0.6f, 0), UIKit.Gen("Mist"), "Grill", 25);
            UIKit.Fog(front, new Vector3(x1 - x0, 0.4f, 1f), (x1 - x0) * 0.035f, new Color(0.62f, 0.68f, 0.85f, 0.07f), new Vector2(3.5f, 6f), 0.4f);
        }

        FogBand(-3f, 125f, -0.7f);
        FogBand(125f, 185f, 2.3f);
        FogBand(185f, 300f, 7.5f);
        FogBand(300f, 440f, 13f);

        var wisps = UIKit.Particles("Soul Wisps", fx, new Vector3(218f, 9f, 0), UIKit.Gen("SoftCircle"), "Grill", 6);
        UIKit.Embers(wisps, new Vector3(445f, 22f, 1f), 28f, new Color(0.45f, 0.85f, 1f, 0.75f), new Color(0.75f, 0.95f, 1f, 0.75f), 0.2f);

        UIKit.Light("Window Glow 1", fx, Light2D.LightType.Point, new Vector3(30f, 4f, 0), new Color(1f, 0.6f, 0.3f), 0.5f, 6f, 0.5f);
        UIKit.GlobalVolume(UIKit.Profile("GraveyardPost", 0.36f, 0.75f, 10f, -8f, new Color(0.95f, 0.95f, 1f))).transform.SetParent(fx, false);
    }

    // ------------------------------------------------------------------ UI
    static void CleanupOldUI()
    {
        // The old canvas carried SceneManagement, whose DontDestroyOnLoad made this whole canvas
        // (and its HUD) persist into the next scenes. Its transitions are replaced by SceneFader.
        var oldCanvas = GameObject.Find("Canvas") ?? GameObject.Find("Canvas (legacy, unused)");
        if (oldCanvas == null) return;
        foreach (var sm in Object.FindObjectsByType<SceneManagement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(sm);
        foreach (string n in new[] { "Canvas/Main menu Ui", "Canvas/AboutMenuUI", "Canvas/Health_UI", "Canvas/Coin UI", "Canvas/GameOverUI", "Canvas/Fade_In" })
        {
            var t = oldCanvas.transform.Find(n.Substring("Canvas/".Length));
            if (t != null) t.gameObject.SetActive(false);
        }
        foreach (Transform t in oldCanvas.transform)
            if (t.name.StartsWith("StartTransition") || t.name.StartsWith("EndTransition")) t.gameObject.SetActive(false);
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.name == "Canvas" && c.transform.parent == null)
                c.gameObject.name = "Canvas (legacy, unused)";
        log.AppendLine("old in-level menu/HUD copies disabled; SceneManagement removed from the level canvas");
    }

    static void BuildHud()
    {
        foreach (var old in Object.FindObjectsByType<GameHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(old.gameObject);

        var heart = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Royal Heart").FirstOrDefault() ?? "")).OfType<Sprite>().FirstOrDefault();
        var pc = GameObject.FindGameObjectWithTag("Player").GetComponent<Player_Collectables>();
        var flask = new SerializedObject(pc).FindProperty("healingFlaskSprite").objectReferenceValue as Sprite;
        var coinSprite = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Coin.prefab").GetComponentInChildren<SpriteRenderer>().sprite))
            .OfType<Sprite>().FirstOrDefault(s => s.name == "animated_items_4") ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Coin.prefab").GetComponentInChildren<SpriteRenderer>().sprite;

        var canvas = UIKit.MakeCanvas("[L1] HUD Canvas", 20);
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
        var pp = UIKit.Panel("Panel", pauseRoot, new Vector2(820, 540), "Paused", out var _);
        var resume = UIKit.MenuButton("Resume", pp, "Resume", new Vector2(130, -130), new Vector2(560, 60), 36);
        var mLabel = UIKit.Text(UIKit.Rect("Music Label", pp, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(150, -220), new Vector2(240, 50)),
            "Music", 28, UIKit.Bone, TextAlignmentOptions.MidlineLeft, UIKit.BodyMat, FontStyles.Bold | FontStyles.UpperCase, 4f);
        var mSlider = UIKit.MakeSlider("Music Slider", pp, new Vector2(370, -220), new Vector2(260, 46), mLabel);
        var mVal = UIKit.Text(UIKit.Rect("Music Value", pp, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(650, -220), new Vector2(120, 50)),
            "35%", 26, UIKit.Gold, TextAlignmentOptions.MidlineLeft, UIKit.BodyMat, FontStyles.Bold);
        var sLabel = UIKit.Text(UIKit.Rect("Effects Label", pp, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(150, -290), new Vector2(240, 50)),
            "Effects", 28, UIKit.Bone, TextAlignmentOptions.MidlineLeft, UIKit.BodyMat, FontStyles.Bold | FontStyles.UpperCase, 4f);
        var sSlider = UIKit.MakeSlider("Effects Slider", pp, new Vector2(370, -290), new Vector2(260, 46), sLabel);
        var sVal = UIKit.Text(UIKit.Rect("Effects Value", pp, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(650, -290), new Vector2(120, 50)),
            "55%", 26, UIKit.Gold, TextAlignmentOptions.MidlineLeft, UIKit.BodyMat, FontStyles.Bold);
        var pMenu = UIKit.MenuButton("Main Menu", pp, "Main Menu", new Vector2(130, -380), new Vector2(560, 60), 36);
        var pQuit = UIKit.MenuButton("Quit", pp, "Quit Game", new Vector2(130, -450), new Vector2(560, 60), 36);
        UIKit.SetVerticalNav(new Selectable[] { resume, mSlider, sSlider, pMenu, pQuit });
        var pause = canvas.gameObject.AddComponent<PauseMenu>();
        var pso = new SerializedObject(pause);
        pso.FindProperty("panel").objectReferenceValue = pauseGroup;
        pso.FindProperty("resumeButton").objectReferenceValue = resume;
        pso.FindProperty("mainMenuButton").objectReferenceValue = pMenu;
        pso.FindProperty("quitButton").objectReferenceValue = pQuit;
        pso.FindProperty("musicSlider").objectReferenceValue = mSlider;
        pso.FindProperty("sfxSlider").objectReferenceValue = sSlider;
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
        log.AppendLine("HUD: health/coins/flasks, area titles, toasts, controls hint, boss bar, respawn fade, Game Over, Pause");
    }
}
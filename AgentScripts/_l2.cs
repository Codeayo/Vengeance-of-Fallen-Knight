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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using K = LevelKit;
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

        BuildReadPanel(canvas);
        BuildShrineMenu(canvas);
        BuildInventory(canvas);
        // Fade, Game Over and Pause stay on top of everything.
        ct.Find("Screen Fade").SetAsLastSibling();
        ct.Find("Game Over").SetAsLastSibling();
        ct.Find("Pause").SetAsLastSibling();
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

/// <summary>Shared editor-time level construction toolkit used by every level builder.</summary>
public static class LevelKit
{
    public static StringBuilder Log = new StringBuilder();
    public static Transform Root;
    public static Transform Terrain, Enemies, Content, Decor, Fx;
    public static int GroundLayer => LayerMask.NameToLayer("Ground");
    public static int GroundMask => LayerMask.GetMask("Ground");
    public static float MinX, MaxX;
    static readonly List<(float x0, float x1, float y)> grounds = new List<(float, float, float)>();

    // ------------------------------------------------------------------ styles
    public class Style
    {
        public Sprite top;            // tiled horizontally along the surface
        public float surfaceInset;    // distance from the sprite's top edge down to the walkable surface
        public Color topTint = Color.white;
        public Sprite body;           // tiled below the top strip (null = solid colour)
        public Color bodyColor = new Color(0.05f, 0.04f, 0.05f);
        public Sprite platform;       // floating jump-through ledges
        public float platformInset = 0.15f;
        public float platformHeight;  // 0 = sprite height
        public string sortingLayer = "Ground";
    }

    public static Sprite SpriteAt(string texPath, string name) =>
        AssetDatabase.LoadAllAssetsAtPath(texPath).OfType<Sprite>().FirstOrDefault(s => s.name == name);

    public static Sprite SpriteFile(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

    /// <summary>Tiled SpriteRenderers need FullRect meshes.</summary>
    public static void EnsureFullRect(Sprite s)
    {
        if (s == null) return;
        string p = AssetDatabase.GetAssetPath(s);
        var ti = AssetImporter.GetAtPath(p) as TextureImporter;
        if (ti == null) return;
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        if (settings.spriteMeshType != SpriteMeshType.FullRect || ti.wrapMode != TextureWrapMode.Clamp)
        {
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }
    }

    // ------------------------------------------------------------------ scene
    public static Scene Begin(string scenePath, string rootName)
    {
        Log = new StringBuilder();
        grounds.Clear();
        UIKit.EnsureGenerated();
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects().Where(g => g.name.StartsWith("[L]")).ToArray())
            Object.DestroyImmediate(go);
        Root = new GameObject("[L] " + rootName).transform;
        Terrain = Child(Root, "Terrain");
        Enemies = Child(Root, "Enemies");
        Content = Child(Root, "Gameplay");
        Decor = Child(Root, "Decor");
        Fx = Child(Root, "Atmosphere");
        return scene;
    }

    public static void Save(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static Transform Child(Transform parent, string name, Vector3? pos = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (pos.HasValue) go.transform.position = pos.Value;
        return go.transform;
    }

    /// <summary>Disables the old broken terrain, legacy canvas, scene switchers and stray coins.</summary>
    public static void RetireLegacy(params string[] tilemapNames)
    {
        foreach (var tm in Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (tilemapNames.Contains(tm.name))
                tm.gameObject.SetActive(false);

        foreach (var name in new[] { "Canvas", "LevelLoader", "NextLevel", "DeathBoxx", "GameObject", "SceneSwitcher" })
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name == name))
                go.SetActive(false);

        foreach (var sm in Object.FindObjectsByType<SceneManagement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(sm);

        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name.StartsWith("Coin") || g.name.StartsWith("Enemy_1")).ToArray())
            Object.DestroyImmediate(go);
        foreach (var h in Object.FindObjectsByType<GameHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(h.gameObject);
    }

    // ------------------------------------------------------------------ terrain
    /// <summary>Solid ground from x0 to x1 with its walkable surface at y. Extends down to the bottom.</summary>
    public static GameObject Ground(float x0, float x1, float y, Style st, float depth = 14f)
    {
        var go = new GameObject($"Ground {x0:F0}-{x1:F0} @{y:F1}");
        go.transform.SetParent(Terrain, false);
        go.layer = GroundLayer;
        go.transform.position = new Vector3((x0 + x1) * 0.5f, y, 0);
        float w = x1 - x0;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, depth);
        col.offset = new Vector2(0, -depth * 0.5f);

        float topH = st.top.bounds.size.y;
        var top = Tiled(go.transform, "Surface", st.top, new Vector3(0, -topH * 0.5f + st.surfaceInset, 0), new Vector2(w, topH), st.sortingLayer, 2, st.topTint);
        float bodyTop = -(topH - st.surfaceInset) + 0.05f;
        float bodyH = depth + bodyTop;
        if (bodyH > 0.1f)
        {
            if (st.body != null)
                Tiled(go.transform, "Body", st.body, new Vector3(0, bodyTop - bodyH * 0.5f, 0), new Vector2(w, bodyH), st.sortingLayer, 1, Color.white);
            else
                Tiled(go.transform, "Body", UIKit.Gen("WhitePixel"), new Vector3(0, bodyTop - bodyH * 0.5f, 0), new Vector2(w, bodyH), st.sortingLayer, 1, st.bodyColor);
        }

        grounds.Add((x0, x1, y));
        MinX = Mathf.Min(MinX, x0);
        MaxX = Mathf.Max(MaxX, x1);
        return go;
    }

    /// <summary>Floating ledge the knight can jump up through and stand on.</summary>
    public static GameObject Ledge(float x0, float x1, float y, Style st)
    {
        var sprite = st.platform ?? st.top;
        var go = new GameObject($"Ledge {x0:F0}-{x1:F0} @{y:F1}");
        go.transform.SetParent(Terrain, false);
        go.layer = GroundLayer;
        go.transform.position = new Vector3((x0 + x1) * 0.5f, y, 0);
        float w = x1 - x0;
        float h = st.platformHeight > 0 ? st.platformHeight : sprite.bounds.size.y;
        Tiled(go.transform, "Surface", sprite, new Vector3(0, -h * 0.5f + st.platformInset, 0), new Vector2(w, h), st.sortingLayer, 3, st.topTint);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, 0.4f);
        col.offset = new Vector2(0, -0.2f);
        col.usedByEffector = true;
        var eff = go.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.surfaceArc = 160f;
        return go;
    }

    public static SpriteRenderer Tiled(Transform parent, string name, Sprite sprite, Vector3 localPos, Vector2 size, string layer, int order, Color tint)
    {
        EnsureFullRect(sprite);
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = tint;
        return sr;
    }

    /// <summary>Boundary walls, pit hazards between ground segments and an abyss safety volume.</summary>
    public static void FinishTerrain(Style st, float wallHeight = 14f)
    {
        var sorted = grounds.OrderBy(g => g.x0).ToList();
        var first = sorted.First();
        var last = sorted.Last();
        Wall(first.x0 - 3f, first.x0, first.y, wallHeight, st);
        Wall(last.x1, last.x1 + 3f, last.y, wallHeight, st);

        var hz = Child(Content, "Hazards");
        int pits = 0;
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            float gapStart = sorted[i].x1, gapEnd = sorted[i + 1].x0;
            if (gapEnd - gapStart < 0.6f) continue;
            float rim = Mathf.Min(sorted[i].y, sorted[i + 1].y);
            var zone = Child(hz, $"Pit {++pits}", new Vector3((gapStart + gapEnd) * 0.5f, rim - 6f, 0));
            zone.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            var box = zone.gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(gapEnd - gapStart + 0.6f, 6f);
            var h = zone.gameObject.AddComponent<HazardZone>();
            SetS(h, "message", "You fall into the darkness");
            SetI(h, "damage", 15);
        }

        float minY = sorted.Min(g => g.y);
        var abyss = Child(hz, "Abyss", new Vector3((MinX + MaxX) * 0.5f, minY - 22f, 0));
        abyss.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        var ab = abyss.gameObject.AddComponent<BoxCollider2D>();
        ab.isTrigger = true;
        ab.size = new Vector2(MaxX - MinX + 40f, 8f);
        var az = abyss.gameObject.AddComponent<HazardZone>();
        SetI(az, "damage", 20);
        Log.AppendLine($"terrain: {sorted.Count} ground segments x {MinX:F0}..{MaxX:F0}, {pits} pits with respawn hazards");
    }

    static void Wall(float x0, float x1, float baseY, float height, Style st)
    {
        var go = Ground(x0, x1, baseY + height, st, height + 14f);
        go.name = "Boundary Wall";
        grounds.RemoveAt(grounds.Count - 1);
    }

    public static float SurfaceY(float x)
    {
        Physics2D.SyncTransforms();
        var hit = Physics2D.Raycast(new Vector2(x, 80f), Vector2.down, 200f, GroundMask);
        return hit ? hit.point.y : 0f;
    }

    /// <summary>Surface of solid ground only (ignores floating ledges).</summary>
    public static float FloorY(float x)
    {
        Physics2D.SyncTransforms();
        foreach (var h in Physics2D.RaycastAll(new Vector2(x, 80f), Vector2.down, 200f, GroundMask).OrderByDescending(h => h.point.y))
            if (h.collider.GetComponent<PlatformEffector2D>() == null) return h.point.y;
        return 0f;
    }

    // ------------------------------------------------------------------ camera, background, light
    public static void Camera(float lens = 8.5f, float topY = 30f)
    {
        var cm = Object.FindFirstObjectByType<CinemachineCamera>();
        var lensSettings = cm.Lens;
        lensSettings.OrthographicSize = lens;
        cm.Lens = lensSettings;
        var player = GameObject.FindGameObjectWithTag("Player");
        cm.Follow = player.transform;
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

        var conf2d = cm.GetComponent<CinemachineConfiner2D>();
        if (conf2d == null) conf2d = cm.gameObject.AddComponent<CinemachineConfiner2D>();
        var confGo = GameObject.Find("[L] Camera Bounds") ?? new GameObject("[L] Camera Bounds");
        var poly = confGo.GetComponent<PolygonCollider2D>(); if (poly == null) poly = confGo.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        confGo.layer = LayerMask.NameToLayer("Ignore Raycast");
        float minY = grounds.Min(g => g.y) - 9f;
        poly.pathCount = 1;
        poly.SetPath(0, new[] { new Vector2(MinX - 3.5f, minY), new Vector2(MaxX + 3.5f, minY), new Vector2(MaxX + 3.5f, topY), new Vector2(MinX - 3.5f, topY) });
        conf2d.BoundingShape2D = poly;
        conf2d.InvalidateBoundingShapeCache();

        var cam = UnityEngine.Camera.main;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.07f);
    }

    /// <summary>Camera-following background layer tiled across the level.</summary>
    public static void Backdrop(Sprite sprite, float follow, float height, float y, string layer, int order, Color tint)
    {
        EnsureFullRect(sprite);
        var go = new GameObject("Backdrop " + sprite.name);
        go.transform.SetParent(Decor, false);
        float scale = height / sprite.bounds.size.y;
        float len = (MaxX - MinX) * (1f - follow) + 90f;
        go.transform.position = new Vector3(MinX + len * 0.5f - 45f, y, 0);
        go.transform.localScale = new Vector3(scale, scale, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(len / scale, sprite.bounds.size.y);
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = tint;
        var pf = go.AddComponent<ParallaxFollow>();
        SetF(pf, "followX", follow);
        SetF(pf, "followY", Mathf.Clamp01(follow + 0.05f));
    }

    public static void Atmosphere(Color ambient, float intensity, Color fog, float fogAlpha, VolumeLook look, string particles, Color particleA, Color particleB)
    {
        UIKit.Light("Ambient (Global)", Fx, Light2D.LightType.Global, Vector3.zero, ambient, intensity);
        var sorted = grounds.OrderBy(g => g.x0).ToList();
        // Fog bands follow the terrain height in 60-unit chunks.
        for (float x = MinX; x < MaxX; x += 60f)
        {
            float x1 = Mathf.Min(MaxX, x + 60f);
            var inRange = sorted.Where(g => g.x1 > x && g.x0 < x1).ToList();
            float y = inRange.Count > 0 ? inRange.Average(g => g.y) : 0f;
            var back = UIKit.Particles($"Fog {x:F0}", Fx, new Vector3((x + x1) * 0.5f, y + 0.8f, 0), UIKit.Gen("Mist"), "Backtiles", 10);
            UIKit.Fog(back, new Vector3(x1 - x, 1.8f, 1f), (x1 - x) * 0.06f, new Color(fog.r, fog.g, fog.b, fogAlpha), new Vector2(5f, 9f), 0.25f);
            var front = UIKit.Particles($"Low Mist {x:F0}", Fx, new Vector3((x + x1) * 0.5f, y - 0.7f, 0), UIKit.Gen("Mist"), "Grill", 25);
            UIKit.Fog(front, new Vector3(x1 - x, 0.4f, 1f), (x1 - x) * 0.035f, new Color(fog.r, fog.g, fog.b, fogAlpha * 0.6f), new Vector2(3.5f, 6f), 0.4f);
        }

        float midY = sorted.Average(g => g.y);
        var ps = UIKit.Particles(particles, Fx, new Vector3((MinX + MaxX) * 0.5f, midY + 8f, 0), UIKit.Gen("SoftCircle"), "Grill", 6);
        if (particles == "Snow")
        {
            UIKit.Embers(ps, new Vector3(MaxX - MinX + 40f, 1f, 1f), (MaxX - MinX) * 0.35f, particleA, particleB, 0.16f);
            var vel = ps.velocityOverLifetime;
            vel.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.2f);
            vel.y = new ParticleSystem.MinMaxCurve(-1.6f, -0.8f);
            ps.transform.position += Vector3.up * 10f;
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 14f);
        }
        else
        {
            UIKit.Embers(ps, new Vector3(MaxX - MinX + 20f, 18f, 1f), (MaxX - MinX) * 0.08f, particleA, particleB, 0.2f);
        }

        UIKit.GlobalVolume(UIKit.Profile(look.name, look.vignette, look.bloom, look.contrast, look.saturation, look.tint)).transform.SetParent(Fx, false);
    }

    public struct VolumeLook
    {
        public string name; public float vignette, bloom, contrast, saturation; public Color tint;
        public VolumeLook(string n, float v, float b, float c, float s, Color t) { name = n; vignette = v; bloom = b; contrast = c; saturation = s; tint = t; }
    }

    public static Light2D PointLight(float x, float yAboveGround, Color c, float intensity, float radius)
    {
        return UIKit.Light("Light", Fx, Light2D.LightType.Point, new Vector3(x, SurfaceY(x) + yAboveGround, 0), c, intensity, radius, radius * 0.15f);
    }

    // ------------------------------------------------------------------ player, UI
    public static void Player(float spawnX, string music = null)
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        p.layer = LayerMask.NameToLayer("Player");
        var rb = p.GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Art/Generated/PlayerNoFriction.physicsMaterial2D");
        var box = p.GetComponent<BoxCollider2D>();
        box.sharedMaterial = mat;
        box.size = new Vector2(2.0f, 3.25f);
        box.offset = new Vector2(0.92f, 0f);
        if (p.GetComponent<FlipColliderSync>() == null) p.AddComponent<FlipColliderSync>();
        if (p.GetComponent<PlayerInteractor>() == null) p.AddComponent<PlayerInteractor>();
        var player = p.GetComponent<global::Player>();
        player.groundLayer = GroundMask;
        player.groundCheck = null;
        var so = new SerializedObject(player);
        so.FindProperty("menuPanel").objectReferenceValue = null;
        so.FindProperty("healthSlider").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();
        Set(p.GetComponent<Player_Collectables>(), "currentCoin_Text", null);
        float y = SurfaceY(spawnX);
        p.transform.position = new Vector3(spawnX, y + 1.64f, 0);
        if (PrefabUtility.IsPartOfPrefabInstance(p))
            foreach (Object o in new Object[] { p, p.transform, rb, box, player })
                PrefabUtility.RecordPrefabInstancePropertyModifications(o);
    }

    public static void Hud()
    {
        foreach (var h in Object.FindObjectsByType<GameHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(h.gameObject);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildHud.PrefabPath);
        var hud = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        hud.name = "[L] HUD";
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            // The module creates its default UI actions itself when none are assigned.
            new GameObject("[L] EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }

    // ------------------------------------------------------------------ gameplay objects
    public static Checkpoint Shrine(string name, float x)
    {
        float gy = SurfaceY(x);
        var go = Child(Content, name, new Vector3(x, gy, 0)).gameObject;
        Sprite(go.transform, "Statue", SpriteFile("Assets/Art/Graveyard/Salt.png"), new Vector3(x, gy + 3.0f, 0), 3.0f, "Backtiles", 6, Color.white);
        Sprite(go.transform, "Brazier", UIKit.Gen("Diamond"), new Vector3(x + 1.3f, gy + 0.35f, 0), 0.6f, "Backtiles", 7, new Color(0.35f, 0.3f, 0.28f)).transform.localScale = new Vector3(0.6f, 0.35f, 1);
        var glow = Sprite(go.transform, "Glow", UIKit.Gen("SoftCircle"), new Vector3(x + 1.3f, gy + 0.9f, 0), 2.4f, "Backtiles", 8, new Color(1f, 0.55f, 0.2f, 0.55f));
        var flame = UIKit.Particles("Flame", go.transform, new Vector3(x + 1.3f, gy + 0.55f, 0), UIKit.Gen("SoftCircle"), "Grill", 5);
        var main = flame.main;
        main.prewarm = false; main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.15f, 0.9f), new Color(1f, 0.85f, 0.4f, 0.9f));
        main.startSpeed = 0f;
        var em = flame.emission; em.rateOverTime = 28f;
        var sh = flame.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(0.4f, 0.05f, 0.1f);
        var vel = flame.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f); vel.y = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var sz = flame.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.1f));
        var light = UIKit.Light("Flame Light", go.transform, Light2D.LightType.Point, new Vector3(x + 1.3f, gy + 1.2f, 0), new Color(1f, 0.6f, 0.3f), 0.25f, 6f, 0.4f);
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true; box.offset = new Vector2(0.6f, 2f); box.size = new Vector2(3f, 4f);
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        var cp = go.AddComponent<Checkpoint>();
        var respawn = Child(go.transform, "Respawn Point", new Vector3(x + 2.6f, gy + 1.7f, 0));
        var so = new SerializedObject(cp);
        so.FindProperty("checkpointName").stringValue = name;
        so.FindProperty("respawnPoint").objectReferenceValue = respawn;
        so.FindProperty("flameLight").objectReferenceValue = light;
        so.FindProperty("flameParticles").objectReferenceValue = flame;
        var gs = so.FindProperty("glowSprites"); gs.arraySize = 1; gs.GetArrayElementAtIndex(0).objectReferenceValue = glow;
        so.ApplyModifiedPropertiesWithoutUndo();
        go.AddComponent<ShrineInteraction>();
        return cp;
    }

    public static void Banner(float x, string title, string sub)
    {
        var go = Child(Content, "Banner: " + title, new Vector3(x, SurfaceY(x) + 4f, 0)).gameObject;
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        var b = go.AddComponent<BoxCollider2D>(); b.isTrigger = true; b.size = new Vector2(1f, 14f);
        var ab = go.AddComponent<AreaBanner>();
        SetS(ab, "title", title); SetS(ab, "subtitle", sub);
    }

    public static void Note(float x, string title, string body, Sprite visual, float scale = 1f, string verb = "Read")
    {
        float gy = SurfaceY(x);
        var go = Child(Content, "Note: " + title, new Vector3(x, gy + 1.2f, 0)).gameObject;
        if (visual != null)
        {
            var sr = Sprite(go.transform, "Visual", visual, new Vector3(x, gy + visual.bounds.extents.y * scale - 0.05f, 0), scale, "Backtiles", 12, Color.white);
        }
        var marker = Sprite(go.transform, "Glint", UIKit.Gen("SoftCircle"), new Vector3(x, gy + 1.6f * Mathf.Max(1f, scale), 0), 0.9f, "Grill", 3, new Color(1f, 0.85f, 0.5f, 0.6f));
        var n = go.AddComponent<LoreNote>();
        n.Configure(title, body, verb);
        Set(n, "unreadMarker", marker.gameObject);
    }

    public static void Pickup(string id, float x, float yAboveGround = 1.2f, int amount = 1, string unique = null)
    {
        var p = ItemPickup.Spawn(id, amount, new Vector3(x, SurfaceY(x) + yAboveGround, 0), unique);
        p.transform.SetParent(Content, true);
        var so = new SerializedObject(p);
        so.FindProperty("itemId").stringValue = id;
        so.FindProperty("amount").intValue = amount;
        so.FindProperty("uniqueId").stringValue = unique ?? "";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void Coins(float x0, float x1, float yAbove, int count)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Coin.prefab");
        for (int i = 0; i < count; i++)
        {
            float x = count == 1 ? x0 : Mathf.Lerp(x0, x1, i / (float)(count - 1));
            var c = (GameObject)PrefabUtility.InstantiatePrefab(prefab, Content);
            c.transform.position = new Vector3(x, SurfaceY(x) + yAbove + Mathf.Sin(i * 0.9f) * 0.3f, 0);
            var rb = c.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; PrefabUtility.RecordPrefabInstancePropertyModifications(rb); }
        }
    }

    public static void Gate(float x, string keyId, string id, Sprite bars, float height = 4.5f)
    {
        float gy = SurfaceY(x);
        var go = Child(Content, "Locked Gate (" + keyId + ")", new Vector3(x, gy, 0)).gameObject;
        var barsT = Child(go.transform, "Bars", new Vector3(x, gy, 0));
        int n = Mathf.CeilToInt(height / bars.bounds.size.y);
        for (int i = 0; i < n; i++)
            Sprite(barsT, "Bar", bars, new Vector3(x, gy + bars.bounds.extents.y + i * bars.bounds.size.y, 0), 1f, "Grill", 5, new Color(0.8f, 0.8f, 0.9f));
        var block = go.AddComponent<BoxCollider2D>();
        block.offset = new Vector2(0, height * 0.5f);
        block.size = new Vector2(0.8f, height);
        go.layer = GroundLayer;
        var g = go.AddComponent<LockedGate>();
        g.Configure(keyId, barsT, block, id);
    }

    public static void Trap(float x, float delay, Sprite[] tips, Sprite[] bars)
    {
        float gy = SurfaceY(x);
        var go = Child(Content, $"Spike Trap x{x:F0}", new Vector3(x, gy, 0)).gameObject;
        var spikes = Child(go.transform, "Spikes", new Vector3(x, gy - 1.6f, 0));
        for (int i = 0; i < 4; i++)
        {
            Sprite(spikes, "Tip " + i, tips[i], new Vector3(x - 1.5f + i, gy - 1.65f, 0), 1f, "Backtiles", 9, Color.white);
            Sprite(spikes, "Bar " + i, bars[i], new Vector3(x - 1.5f + i, gy - 2.65f, 0), 1f, "Backtiles", 9, Color.white);
        }
        var dmg = Child(go.transform, "Damage Area", new Vector3(x, gy + 0.6f, 0)).gameObject;
        dmg.layer = LayerMask.NameToLayer("Ignore Raycast");
        var box = dmg.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(3.4f, 1.1f);
        var trap = dmg.AddComponent<SpikeTrap>();
        var so = new SerializedObject(trap);
        so.FindProperty("spikes").objectReferenceValue = spikes;
        so.FindProperty("damageArea").objectReferenceValue = box;
        so.FindProperty("raisedHeight").floatValue = 1.55f;
        so.FindProperty("startDelay").floatValue = delay;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static LevelExit Exit(float x, string next, GameObject[] required, Color sealedColor, string sealedMessage)
    {
        float gy = SurfaceY(x);
        var gate = Child(Content, "Exit -> " + next, new Vector3(x, gy, 0)).gameObject;
        var statue = SpriteFile("Assets/Art/Graveyard/Salt.png");
        Sprite(gate.transform, "Statue West", statue, new Vector3(x - 2.9f, gy + 3.3f, 0), 3.3f, "Backtiles", 6, Color.white);
        Sprite(gate.transform, "Statue East", statue, new Vector3(x + 2.9f, gy + 3.3f, 0), 3.3f, "Backtiles", 6, Color.white).flipX = true;
        var sealedFx = Child(gate.transform, "Sealed", new Vector3(x, gy, 0)).gameObject;
        Sprite(sealedFx.transform, "Curse Glow", UIKit.Gen("SoftCircle"), new Vector3(x, gy + 2.5f, 0), 1f, "Backtiles", 7, new Color(sealedColor.r, sealedColor.g, sealedColor.b, 0.55f)).transform.localScale = new Vector3(4f, 7f, 1);
        UIKit.Light("Curse Light", sealedFx.transform, Light2D.LightType.Point, new Vector3(x, gy + 2.5f, 0), sealedColor, 1.1f, 5f, 0.5f);
        var openFx = Child(gate.transform, "Open", new Vector3(x, gy, 0)).gameObject;
        Sprite(openFx.transform, "Beam", UIKit.Gen("SoftCircle"), new Vector3(x, gy + 4f, 0), 1f, "Backtiles", 7, new Color(1f, 0.82f, 0.45f, 0.65f)).transform.localScale = new Vector3(3.2f, 12f, 1);
        UIKit.Light("Gate Light", openFx.transform, Light2D.LightType.Point, new Vector3(x, gy + 2.5f, 0), new Color(1f, 0.85f, 0.55f), 1.8f, 7f, 1f);
        var motes = UIKit.Particles("Rising Light", openFx.transform, new Vector3(x, gy + 0.3f, 0), UIKit.Gen("SoftCircle"), "Grill", 4);
        UIKit.Embers(motes, new Vector3(2.5f, 0.3f, 1f), 18f, new Color(1f, 0.9f, 0.6f, 0.9f), new Color(1f, 0.7f, 0.3f, 0.9f), 0.25f);
        gate.layer = LayerMask.NameToLayer("Ignore Raycast");
        var box = gate.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.offset = new Vector2(0, 3f); box.size = new Vector2(2.6f, 6f);
        var exit = gate.AddComponent<LevelExit>();
        var so = new SerializedObject(exit);
        so.FindProperty("nextSceneName").stringValue = next;
        so.FindProperty("sealedMessage").stringValue = sealedMessage;
        var req = so.FindProperty("requiredDefeated");
        req.arraySize = required?.Length ?? 0;
        for (int i = 0; i < req.arraySize; i++) req.GetArrayElementAtIndex(i).objectReferenceValue = required[i];
        so.FindProperty("sealedVisual").objectReferenceValue = sealedFx;
        so.FindProperty("openVisual").objectReferenceValue = openFx;
        so.ApplyModifiedPropertiesWithoutUndo();
        openFx.SetActive(false);
        return exit;
    }

    public static void BossArena(float x, MonoBehaviour boss, string name, string arrival, string defeatTitle, string defeatSub)
    {
        var go = Child(Content, "Boss Trigger: " + name, new Vector3(x, SurfaceY(x) + 4f, 0)).gameObject;
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        var b = go.AddComponent<BoxCollider2D>(); b.isTrigger = true; b.size = new Vector2(6f, 14f);
        var enc = go.AddComponent<BossEncounter>();
        var so = new SerializedObject(enc);
        so.FindProperty("boss").objectReferenceValue = boss;
        so.FindProperty("bossName").stringValue = name;
        so.FindProperty("arrivalMessage").stringValue = arrival;
        so.FindProperty("defeatTitle").stringValue = defeatTitle;
        so.FindProperty("defeatSubtitle").stringValue = defeatSub;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ enemies
    public enum Kind { Skeleton, EliteSkeleton, GiantSkeleton, Axe, Ninja, EliteNinja, NinjaMaster }

    static GameObject Prefab(Kind k)
    {
        switch (k)
        {
            case Kind.Axe: return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemies/Prefabs/AxeSkeleton.prefab");
            case Kind.Ninja: case Kind.EliteNinja: case Kind.NinjaMaster: return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemies/Prefabs/YellowNinja.prefab");
            default: return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemies/Prefabs/PatrolSkeleton.prefab");
        }
    }

    public static EnemyCore.Loot L(string id, float chance, int min = 1, int max = 1) => new EnemyCore.Loot { itemId = id, chance = chance, min = min, max = max };

    /// <summary>Spawns and fully configures an enemy of the given archetype on the ground at x.</summary>
    public static GameObject Enemy(Kind kind, float x, string name, Color tint, int hp = 0, int dmg = 0, int xp = 0, bool faceLeft = true, params EnemyCore.Loot[] loot)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(kind), Enemies);
        go.name = name;
        go.layer = LayerMask.NameToLayer("Enemy");
        foreach (Transform t in go.GetComponentsInChildren<Transform>()) if (t.gameObject.layer == 0) t.gameObject.layer = LayerMask.NameToLayer("Enemy");
        var core = go.GetComponent<EnemyCore>(); if (core == null) core = go.AddComponent<EnemyCore>();
        if (go.GetComponent<DamageFlash>() == null) go.AddComponent<DamageFlash>();
        var rb = go.GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        foreach (var sr0 in go.GetComponentsInChildren<SpriteRenderer>()) { sr0.sortingLayerName = "player"; sr0.sortingOrder = -1; sr0.color = tint; }
        bool boss = kind == Kind.GiantSkeleton || kind == Kind.NinjaMaster;

        if (kind == Kind.Skeleton || kind == Kind.EliteSkeleton || kind == Kind.GiantSkeleton)
        {
            Vector2 scale = kind == Kind.Skeleton ? new Vector2(1.15f, 1.2f) : kind == Kind.EliteSkeleton ? new Vector2(1.32f, 1.4f) : new Vector2(1.7f, 1.8f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1);
            var box = go.GetComponent<BoxCollider2D>();
            box.size = new Vector2(1.1f, 3.8f); box.offset = new Vector2(0.4f, -0.05f);
            var gc = go.transform.Find("GroundCheck");
            if (gc != null) gc.localPosition = new Vector3(1.0f, -box.size.y * 0.5f + box.offset.y, 0);
            var ai = go.GetComponent<PatrolSkeletonAI>();
            var so = new SerializedObject(ai);
            so.FindProperty("maxHealth").intValue = hp > 0 ? hp : kind == Kind.Skeleton ? 50 : kind == Kind.EliteSkeleton ? 95 : 220;
            so.ApplyModifiedPropertiesWithoutUndo();
            ai.attackDamage = dmg > 0 ? dmg : kind == Kind.Skeleton ? 10 : kind == Kind.EliteSkeleton ? 14 : 20;
            ai.attackRange = kind == Kind.GiantSkeleton ? 4.3f : kind == Kind.EliteSkeleton ? 3.7f : 3.4f;
            ai.chaseRange = kind == Kind.GiantSkeleton ? 11f : 7.5f;
            ai.chaseSpeed = kind == Kind.Skeleton ? 2.2f : kind == Kind.EliteSkeleton ? 2.0f : 2.5f;
            ai.attackCooldown = kind == Kind.Skeleton ? 1.1f : kind == Kind.EliteSkeleton ? 1.3f : 1.25f;
            ai.attackWindup = kind == Kind.GiantSkeleton ? 0.5f : 0.38f;
            ai.enrageAtHalf = kind == Kind.GiantSkeleton;
            ai.groundLayer = GroundMask;
            ai.groundCheckDistance = 2.5f;
            if (go.GetComponent<FlipColliderSync>() == null) go.AddComponent<FlipColliderSync>();
            float gy = SurfaceY(x);
            go.transform.position = new Vector3(x, gy + box.size.y * scale.y * 0.5f - box.offset.y * scale.y + 0.05f, 0);
            go.GetComponent<SpriteRenderer>().flipX = faceLeft;
            Record(go, box, ai, gc);
        }
        else if (kind == Kind.Axe)
        {
            go.transform.position = new Vector3(x, SurfaceY(x) + 1.5f, 0);
            var eb = go.GetComponent<Enemy_behaviour>();
            eb.maxHealth = hp > 0 ? hp : 40;
            eb.attackDamage = dmg > 0 ? dmg : 9;
            eb.moveSpeed = 2.6f;
            Record(go, eb);
        }
        else
        {
            float s = kind == Kind.Ninja ? 1f : kind == Kind.EliteNinja ? 1.15f : 1.5f;
            go.transform.localScale = new Vector3(s, s, 1);
            var n = go.GetComponent<YellowNinja>();
            n.maxHealth = hp > 0 ? hp : kind == Kind.Ninja ? 32 : kind == Kind.EliteNinja ? 60 : 240;
            n.attackDamage = dmg > 0 ? dmg : kind == Kind.Ninja ? 9 : kind == Kind.EliteNinja ? 13 : 17;
            n.chaseSpeed = kind == Kind.Ninja ? 3.0f : kind == Kind.EliteNinja ? 3.6f : 4.2f;
            n.walkSpeed = 1.6f;
            n.attackCooldown = kind == Kind.Ninja ? 1.0f : kind == Kind.EliteNinja ? 0.8f : 0.65f;
            n.detectionRadius = kind == Kind.NinjaMaster ? 12f : 7f;
            n.attackRadius = 1.3f * s;
            n.attackTriggerDistance = 2.6f * s;
            n.whatIsGround = GroundMask;
            n.whatIsPlayer = LayerMask.GetMask("Player", "Default");
            n.groundCheckDistance = 1.2f;
            var box = go.GetComponent<BoxCollider2D>();
            go.transform.position = new Vector3(x, SurfaceY(x) + box.size.y * s * 0.5f + 0.1f, 0);
            Record(go, n, box);
        }

        int defaultXp = kind switch { Kind.Skeleton => 22, Kind.EliteSkeleton => 45, Kind.GiantSkeleton => 220, Kind.Axe => 18, Kind.Ninja => 20, Kind.EliteNinja => 40, _ => 240 };
        core.Configure(name, xp > 0 ? xp : defaultXp, boss ? 15 : 1, boss ? 25 : 4, boss,
            loot.Length > 0 ? loot : new[] { L("crimson_flask", 0.12f), L("soul_shard", kind == Kind.EliteSkeleton || kind == Kind.EliteNinja ? 0.45f : 0.15f), L("verdant_shard", kind == Kind.EliteSkeleton || kind == Kind.EliteNinja ? 0.3f : 0.1f) });
        EditorUtility.SetDirty(core);
        Record(go, core, rb, go.transform);
        return go;
    }

    static void Record(GameObject go, params Object[] objs)
    {
        if (!PrefabUtility.IsPartOfPrefabInstance(go)) return;
        foreach (var o in objs) if (o != null) PrefabUtility.RecordPrefabInstancePropertyModifications(o);
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>()) PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
    }

    // ------------------------------------------------------------------ decor
    public static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Vector3 pos, float scale, string layer, int order, Color tint)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = tint;
        return sr;
    }

    /// <summary>Places a prop standing on the ground at x (bottom of sprite on the surface).</summary>
    public static SpriteRenderer Prop(Sprite s, float x, float scale = 1f, string layer = "Backtiles", int order = 4, bool flip = false, float sink = 0.1f, Color? tint = null)
    {
        if (s == null) { Debug.LogWarning("Missing prop sprite"); return null; }
        float gy = SurfaceY(x);
        float bottom = (s.bounds.center.y - s.bounds.extents.y) * scale;
        var sr = Sprite(Decor, s.name, s, new Vector3(x, gy - bottom - sink, 0), scale, layer, order, tint ?? Color.white);
        sr.flipX = flip;
        return sr;
    }

    // ------------------------------------------------------------------ serialized helpers
    public static void Set(Object c, string prop, Object v) { var so = new SerializedObject(c); so.FindProperty(prop).objectReferenceValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    public static void SetF(Object c, string prop, float v) { var so = new SerializedObject(c); so.FindProperty(prop).floatValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    public static void SetI(Object c, string prop, int v) { var so = new SerializedObject(c); so.FindProperty(prop).intValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
    public static void SetS(Object c, string prop, string v) { var so = new SerializedObject(c); so.FindProperty(prop).stringValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
}

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
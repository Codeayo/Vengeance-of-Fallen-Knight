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
using UnityEngine.Tilemaps;
using K = LevelKit;
using C5 = Level5Castle;
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

    /// <summary>Creates the content containers in the already-open scene (for levels built elsewhere).</summary>
    public static void Attach(string rootName)
    {
        Log = new StringBuilder();
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name.StartsWith("[L]")).ToArray())
            Object.DestroyImmediate(go);
        Root = new GameObject("[L] " + rootName).transform;
        Terrain = Child(Root, "Terrain"); Enemies = Child(Root, "Enemies"); Content = Child(Root, "Gameplay"); Decor = Child(Root, "Decor"); Fx = Child(Root, "Atmosphere");
    }

    /// <summary>Adds rewards to an enemy that was placed by another builder.</summary>
    public static void Reward(GameObject enemy, string name, int xp, bool boss, params EnemyCore.Loot[] loot)
    {
        var core = enemy.GetComponent<EnemyCore>(); if (core == null) core = enemy.AddComponent<EnemyCore>();
        core.Configure(name, xp, boss ? 15 : 1, boss ? 25 : 4, boss, loot.Length > 0 ? loot : new[] { L("crimson_flask", 0.12f), L("soul_shard", 0.2f), L("verdant_shard", 0.1f) });
        EditorUtility.SetDirty(core);
        if (PrefabUtility.IsPartOfPrefabInstance(enemy)) PrefabUtility.RecordPrefabInstancePropertyModifications(core);
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
                Solid(go.transform, "Body", new Vector3(0, bodyTop - bodyH * 0.5f, 0), new Vector2(w, bodyH), st.sortingLayer, 1, st.bodyColor);
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

    /// <summary>Plain colour block (a scaled sprite, not a tiled one).</summary>
    public static SpriteRenderer Solid(Transform parent, string name, Vector3 localPos, Vector2 size, string layer, int order, Color tint)
    {
        var px = UIKit.Gen("WhitePixel");
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x / px.bounds.size.x, size.y / px.bounds.size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = px;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = tint;
        return sr;
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

        // Old camera confiners in these scenes were solid colliders that trapped enemies and players.
        foreach (var old in Object.FindObjectsByType<PolygonCollider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (old.name != "[L] Camera Bounds")
            {
                old.isTrigger = true;
                old.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
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

        K.Pickup("crimson_flask", 64f);
        K.Pickup("boneplate_cuirass", 186f, 1.4f, 1, "grave_boneplate");
        K.Pickup("ring_of_the_fallen", 29f, 7.0f, 1, "grave_ring");   // atop the ruined arch
        K.Pickup("soul_shard", 255f, 1.2f, 1, "grave_shard_1");
        K.Pickup("verdant_shard", 320f, 1.2f, 1, "grave_shard_2");
        K.Pickup("heart_vessel", 300f, 1.4f, 1, "grave_heart");
        K.Pickup("prism_relic", 408f, 1.4f, 1, "relic_graveyard");
        K.Save(scene);
        return baseLog + K.Log.ToString();
    }
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

        for (int i = 0; i < 3; i++)
            K.Backdrop(K.SpriteFile($"Assets/Art/Mushroom_Grove/Assets/Background/Background_{i}.png"), 0.9f - i * 0.18f, 40f, 9f,
                i == 0 ? "Background" : "backmid", i, new Color(0.6f, 0.72f, 0.66f));

        K.Player(3f);
        K.Hud();
        K.Camera(8.5f, 30f);

        K.Banner(3.8f, "The Rotting Grove", "Malvric's sorcery took root here");
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
            "The Colossus Crumbles", "The rot loosens its grip on the grove");

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

        K.Atmosphere(new Color(0.65f, 0.85f, 0.75f), 0.72f, new Color(0.55f, 0.8f, 0.6f), 0.12f,
            new K.VolumeLook("GrovePost", 0.4f, 0.85f, 10f, -5f, new Color(0.9f, 1f, 0.92f)),
            "Spores", new Color(0.6f, 1f, 0.5f, 0.7f), new Color(0.8f, 0.6f, 1f, 0.7f));

        K.Exit(293f, "Taiga", new[] { colossus, add1 }, new Color(0.4f, 1f, 0.3f), "The Colossus's rot seals the path north");
        K.Save(scene);
        return K.Log.ToString();
    }
}

/// <summary>Chapter IV - The Frozen Taiga: the mountain pass to the castle, held by Malvric's frost watch.</summary>
public static class Level4Taiga
{
    const string Tex = "Assets/Art/Final Taiga Forest/Tiles.png";
    static Sprite T(int i) => K.SpriteAt(Tex, "Tiles_" + i);
    static readonly Color Frost = new Color(0.82f, 0.9f, 1f);

    public static string Build()
    {
        var scene = K.Begin("Assets/Scenes/Taiga.unity", "Chapter IV - The Frozen Taiga");
        K.RetireLegacy("Bush", "Ground", "Fence", "Hazards", "Background");

        var st = new K.Style
        {
            top = T(0), surfaceInset = 0.35f, topTint = Frost,
            body = null, bodyColor = new Color(0.1f, 0.12f, 0.12f),
            platform = T(0), platformInset = 0.35f, platformHeight = 1.2f,
        };

        K.Ground(-2, 22, 0f, st);
        K.Ground(22, 36, 2f, st);
        K.Ground(36, 52, 4f, st);
        K.Ground(57, 76, 4f, st);
        K.Ledge(79, 84, 5.6f, st);
        K.Ground(87, 112, 3f, st);
        K.Ledge(95, 101, 7f, st); K.Ledge(103, 108, 10.2f, st);       // frost-key climb
        K.Ground(112, 130, 6f, st);
        K.Ground(130, 152, 8f, st);
        K.Ground(157, 166, 8f, st); K.Ledge(168, 172, 9.4f, st);
        K.Ground(175, 205, 10f, st);
        K.Ground(205, 222, 12f, st);
        K.Ground(222, 240, 10f, st);
        K.Ground(245, 300, 11f, st);
        K.FinishTerrain(st);
        K.Ledge(108, 116, 10.2f, st); // vault ledge behind the frozen gate

        string bgDir = "Assets/Art/Final Taiga Forest/";
        K.Backdrop(K.SpriteFile(bgDir + "Background_0.png"), 0.94f, 46f, 14f, "Background", 0, new Color(0.75f, 0.82f, 0.95f));
        K.Backdrop(K.SpriteFile(bgDir + "Background_1.png"), 0.8f, 40f, 13f, "Background", 1, new Color(0.7f, 0.78f, 0.92f));
        K.Backdrop(K.SpriteFile(bgDir + "Background_3.png"), 0.62f, 34f, 11f, "backmid", 2, new Color(0.7f, 0.78f, 0.9f));
        K.Backdrop(K.SpriteFile(bgDir + "tree.png"), 0.4f, 30f, 10f, "Backnear", 3, new Color(0.65f, 0.72f, 0.85f));

        K.Player(3f);
        K.Hud();
        K.Camera(8.5f, 36f);

        K.Banner(3.8f, "The Frozen Taiga", "The last road to the castle");
        var note = K.SpriteAt("Assets/Art/Castle-Hall/Tiles/Props.png", "Props_1");
        K.Note(12f, "Smith's Letter",
            "We forged for the old king and we will not forge for a usurper. The frost watch can starve us if they like.\n\n" +
            "Our last blade, Frostbrand, we gave to the Warden to keep it from Malvric. Now the Warden serves him.", note, 1.1f);
        K.Note(118f, "Frozen Watchman",
            "His hand is still on his spear. Scratched into the ice beside him:\n\n" +
            "\"The key to the smiths' vault hangs where only a climber can reach it.\"", note, 1.1f);
        K.Note(296f, "The Postern Gate",
            "Ahead, the black towers of the castle. The hermit's map was true: the postern gate stands unguarded.\n\n" +
            "Malvric is waiting on your throne.", note, 1.1f);

        K.Shrine("Shrine of the Pass", 64f);
        K.Shrine("Shrine of the Watch", 178f);
        K.Shrine("Shrine before the Warden", 247f);

        Color frozen = new Color(0.7f, 0.85f, 1f), frostElite = new Color(0.55f, 0.75f, 1f), assassin = new Color(0.75f, 0.85f, 1f);
        K.Enemy(K.Kind.Ninja, 30f, "Frost Assassin", assassin);
        K.Enemy(K.Kind.Skeleton, 46f, "Frozen Dead", frozen);
        K.Enemy(K.Kind.EliteNinja, 70f, "Frost Hunter", frostElite);
        K.Enemy(K.Kind.Skeleton, 90f, "Frozen Dead", frozen, 0, 0, 0, false);
        K.Enemy(K.Kind.EliteSkeleton, 122f, "Frost Watchman", frostElite);
        K.Enemy(K.Kind.Ninja, 140f, "Frost Assassin", assassin);
        K.Enemy(K.Kind.EliteNinja, 148f, "Frost Hunter", frostElite);
        K.Enemy(K.Kind.Axe, 190f, "Frozen Ghoul", frozen);
        K.Enemy(K.Kind.EliteSkeleton, 214f, "Frost Watchman", frostElite);
        K.Enemy(K.Kind.Ninja, 230f, "Frost Assassin", assassin);
        var warden = K.Enemy(K.Kind.GiantSkeleton, 282f, "The Frost Warden", new Color(0.55f, 0.75f, 1f), 360, 24, 360, true,
            K.L("frostbrand", 1f), K.L("frost_amulet", 1f), K.L("soul_shard", 1f, 2, 3), K.L("crimson_flask", 1f, 2, 2));
        var hunter = K.Enemy(K.Kind.EliteNinja, 270f, "Warden's Hunter", frostElite);
        K.BossArena(254f, warden.GetComponent<PatrolSkeletonAI>(), "The Frost Warden", "Ice cracks as the Warden lifts Frostbrand's sheath",
            "The Warden Falls", "Frostbrand is yours");

        K.Pickup("frost_key", 105.5f, 1.3f, 1, "taiga_frost_key");
        K.Gate(107.5f, "frost_key", "taiga_vault", K.SpriteAt("Assets/Art/Graveyard/Tiles.png", "Tiles_234"));
        K.Pickup("frostguard_mail", 112f, 1.3f, 1, "taiga_frostguard");
        K.Pickup("prism_relic", 114.5f, 1.3f, 1, "relic_taiga");
        K.Pickup("crimson_flask", 59f);
        K.Pickup("verdant_shard", 170f, 1.2f, 1, "taiga_ledge_shard");
        K.Pickup("heart_vessel", 218f, 1.3f, 1, "taiga_heart");
        K.Coins(24, 34, 1.3f, 5); K.Coins(132, 150, 1.3f, 6); K.Coins(177, 200, 1.3f, 7);

        // Decor: great stumps, bushes, frozen trees
        foreach (float x in new[] { 16f, 48f, 100f, 136f, 196f, 236f, 290f }) K.Prop(T(1), x, 1f, "Backtiles", 4, false, 0.1f, Frost);
        float[] stumps = { 6f, 60f, 124f, 185f, 228f, 262f };
        for (int i = 0; i < stumps.Length; i++) K.Prop(T(i % 2 == 0 ? 3 : 5), stumps[i], 0.9f, "Backtiles", 2, i % 2 == 1, 0.3f, Frost);
        foreach (float x in new[] { 40f, 116f, 210f, 278f }) K.PointLight(x, 3f, new Color(0.6f, 0.8f, 1f), 0.7f, 7f);

        K.Atmosphere(new Color(0.75f, 0.85f, 1f), 0.85f, new Color(0.85f, 0.92f, 1f), 0.14f,
            new K.VolumeLook("TaigaPost", 0.35f, 0.9f, 12f, -18f, new Color(0.9f, 0.95f, 1.05f)),
            "Snow", new Color(1f, 1f, 1f, 0.9f), new Color(0.8f, 0.9f, 1f, 0.9f));

        K.Exit(296f, "Castle", new[] { warden, hunter }, new Color(0.4f, 0.6f, 1f), "The Warden's ice seals the road");
        K.Save(scene);
        return K.Log.ToString();
    }
}

/// <summary>Chapter V - The Fallen Castle: through the postern gate and the halls Aldric once guarded.</summary>
public static class Level5Castle
{
    public const string Tex = "Assets/Art/Castle-Hall/Tiles/Tiles.png";
    public const string PropsTex = "Assets/Art/Castle-Hall/Tiles/Props.png";
    public static Sprite C(int i) => K.SpriteAt(Tex, "Tiles_" + i);
    public static Sprite P(int i) => K.SpriteAt(PropsTex, "Props_" + i);

    public static K.Style CastleStyle(Color tint) => new K.Style
    {
        top = C(15), surfaceInset = 0.12f, topTint = tint,
        body = C(12), bodyColor = new Color(0.08f, 0.06f, 0.08f),
        platform = C(21), platformInset = 0.1f,
    };

    /// <summary>Hall dressing: tall windows, banners and pillars along the back wall, chandeliers overhead.</summary>
    public static void HallDecor(float x0, float x1, float spacing, Color tint)
    {
        int i = 0;
        for (float x = x0; x < x1; x += spacing, i++)
        {
            float gy = K.FloorY(x);
            K.Sprite(K.Decor, "Column", C(10), new Vector3(x, gy + 3.2f, 0), 1.6f, "backmid", 3, tint);
            K.Sprite(K.Decor, "Window", C(1), new Vector3(x + spacing * 0.5f, gy + 5.4f, 0), 1.4f, "backmid", 2, tint);
            if (i % 2 == 0) K.Sprite(K.Decor, "Banner", C(13), new Vector3(x + spacing * 0.25f, gy + 5.6f, 0), 1.2f, "backmid", 4, tint);
            if (i % 3 == 1) K.Sprite(K.Decor, "Chandelier", P(2 + i % 3), new Vector3(x + spacing * 0.5f, gy + 8.6f, 0), 1.6f, "Grill", 2, Color.white);
            if (i % 3 == 1) K.PointLight(x + spacing * 0.5f, 7.5f, new Color(1f, 0.75f, 0.45f), 1.2f, 8f);
        }
    }

    public static string Build()
    {
        var scene = K.Begin("Assets/Castle.unity", "Chapter V - The Fallen Castle");
        K.RetireLegacy("Bush", "Ground", "Fence", "Hazards", "Background");
        Color stone = new Color(0.78f, 0.72f, 0.8f);
        var st = CastleStyle(Color.white);

        K.Ground(-2, 30, 0f, st);
        K.Ground(30, 44, 2f, st);
        K.Ground(44, 70, 2f, st);
        K.Ground(75, 96, 2f, st);
        K.Ledge(98, 103, 3.6f, st);
        K.Ground(106, 132, 4f, st);
        K.Ledge(112, 118, 7.4f, st); K.Ledge(121, 127, 10.6f, st); K.Ledge(130, 140, 13.6f, st); // vault climb
        K.Ground(132, 160, 6f, st);
        K.Ground(160, 176, 8f, st);
        K.Ground(181, 210, 8f, st);
        K.Ground(210, 228, 10f, st);
        K.Ground(228, 290, 10f, st);
        K.FinishTerrain(st);

        K.Backdrop(K.SpriteFile("Assets/Art/Castle-Hall/Tiles/Background.png"), 0.85f, 40f, 12f, "Background", 0, new Color(0.55f, 0.48f, 0.62f));
        HallDecor(4f, 286f, 16f, stone);

        K.Player(3f);
        K.Hud();
        K.Camera(8.5f, 34f);

        K.Banner(3.8f, "The Fallen Castle", "The halls you once guarded");
        var note = P(9);
        K.Note(14f, "Steward's Ledger",
            "By order of King Malvric, the old guard's names are struck from the rolls.\n\n" +
            "The vault key hangs in the high gallery above the barracks. The old king's sword lies within. None may touch it.", note, 1.1f);
        K.Note(150f, "Prisoner's Scrawl",
            "Aldric would never have let this happen. They say Malvric struck him down in the night with paid knives " +
            "because he could not beat him in the yard.\n\nIf you read this, Captain, finish it.", note, 1.1f);
        K.Note(286f, "To the Throne",
            "The great doors are open. Beyond them the throne room, and the man who wears your king's crown.", note, 1.1f);

        K.Shrine("Shrine of the Postern", 22f);
        K.Shrine("Shrine of the Barracks", 140f);
        K.Shrine("Shrine of the Great Hall", 232f);

        Color guard = new Color(0.85f, 0.6f, 0.6f), black = new Color(0.55f, 0.45f, 0.5f), palace = new Color(0.85f, 0.8f, 1f);
        K.Enemy(K.Kind.Skeleton, 38f, "Dungeon Risen", Color.white);
        K.Enemy(K.Kind.Ninja, 58f, "Palace Knife", palace);
        K.Enemy(K.Kind.EliteSkeleton, 86f, "Royal Guard", guard);
        K.Enemy(K.Kind.Axe, 120f, "Dungeon Ghoul", Color.white);
        K.Enemy(K.Kind.EliteNinja, 150f, "Palace Knife", palace);
        K.Enemy(K.Kind.EliteSkeleton, 170f, "Royal Guard", guard, 0, 0, 0, false);
        K.Enemy(K.Kind.Ninja, 192f, "Palace Knife", palace);
        K.Enemy(K.Kind.EliteSkeleton, 204f, "Royal Guard", guard);
        K.Enemy(K.Kind.EliteNinja, 222f, "Palace Knife", palace);
        var captain = K.Enemy(K.Kind.GiantSkeleton, 270f, "Sir Dravek, Captain of the Black Guard", black, 420, 26, 420, true,
            K.L("royal_guard_plate", 1f), K.L("verdant_shard", 1f, 2, 3), K.L("soul_shard", 1f, 2, 3), K.L("crimson_flask", 1f, 2, 2));
        var g1 = K.Enemy(K.Kind.EliteSkeleton, 258f, "Black Guard", black);
        K.BossArena(242f, captain.GetComponent<PatrolSkeletonAI>(), "Sir Dravek, Captain of the Black Guard",
            "\"Captain? No. I am captain now.\"", "Dravek Falls", "The way to the throne lies open");

        // The vault: royal key from Dravek opens it on the way back, or via the climb if found earlier
        K.Gate(134f, "royal_key", "castle_vault", K.SpriteAt("Assets/Art/Graveyard/Tiles.png", "Tiles_234"));
        K.Pickup("royal_key", 124f, 1.3f, 1, "castle_royal_key");
        K.Pickup("kingsbane", 137f, 1.4f, 1, "castle_kingsbane");
        K.Pickup("prism_relic", 139f, 1.4f, 1, "relic_castle");
        K.Pickup("crimson_flask", 66f);
        K.Pickup("violet_draught", 101f, 1.2f);
        K.Pickup("heart_vessel", 116f, 1.4f, 1, "castle_heart");
        K.Pickup("ember_of_vengeance", 186f);
        K.Coins(4, 26, 1.3f, 6); K.Coins(108, 128, 1.3f, 6); K.Coins(184, 208, 1.3f, 7);

        foreach (float x in new[] { 10f, 80f, 156f, 214f, 284f }) K.Prop(P(5), x, 1.1f, "Backtiles", 6);     // cabinets
        foreach (float x in new[] { 34f, 126f, 198f }) K.Prop(P(10), x, 1.1f, "Backtiles", 6);               // benches
        foreach (float x in new[] { 52f, 94f, 176f, 250f }) K.Prop(P(7), x, 0.8f, "Backtiles", 7);           // skulls
        foreach (float x in new[] { 28f, 146f, 236f }) K.Prop(C(0), x, 1.3f, "Backtiles", 3);                // statues

        K.Atmosphere(new Color(0.8f, 0.72f, 0.95f), 0.7f, new Color(0.6f, 0.5f, 0.75f), 0.08f,
            new K.VolumeLook("CastlePost", 0.42f, 0.8f, 14f, -10f, new Color(0.95f, 0.9f, 1f)),
            "Dust", new Color(1f, 0.85f, 0.6f, 0.5f), new Color(0.9f, 0.8f, 1f, 0.5f));

        K.Exit(287f, "ThroneRoom", new[] { captain, g1 }, new Color(0.8f, 0.2f, 0.3f), "The Black Guard bars the throne room doors");
        K.Save(scene);
        return K.Log.ToString();
    }
}

/// <summary>Chapter VI - The Usurper's Throne: the final confrontation with Lord Malvric.</summary>
public static class Level6Throne
{
    public static string Build()
    {
        var scene = K.Begin("Assets/Scenes/ThroneRoom.unity", "Chapter VI - The Usurper's Throne");
        K.RetireLegacy("Bush", "Ground", "Fence", "Hazards", "Background");
        Color gold = new Color(1f, 0.88f, 0.75f);
        var st = C5.CastleStyle(new Color(1f, 0.85f, 0.85f));

        K.Ground(-2, 40, 0f, st);
        K.Ground(40, 56, 1.5f, st);
        K.Ground(56, 92, 3f, st);
        K.Ledge(66, 72, 6.4f, st);
        K.Ground(92, 110, 3f, st);
        K.Ground(110, 176, 3f, st);   // the throne room floor
        K.Ground(176, 190, 4.5f, st); // the dais
        K.FinishTerrain(st);
        K.Ledge(120, 127, 7f, st); K.Ledge(159, 166, 7f, st); // arena perches

        K.Backdrop(K.SpriteFile("Assets/Art/Castle-Hall/Tiles/Background.png"), 0.85f, 40f, 12f, "Background", 0, new Color(0.65f, 0.45f, 0.5f));
        C5.HallDecor(4f, 108f, 14f, new Color(0.85f, 0.75f, 0.8f));
        C5.HallDecor(112f, 188f, 12f, gold);
        K.Sprite(K.Decor, "Throne Drape", C5.C(2), new Vector3(183f, K.FloorY(183f) + 7f, 0), 2.2f, "backmid", 6, new Color(0.9f, 0.3f, 0.35f));
        K.Prop(C5.C(0), 179f, 1.6f, "Backtiles", 5);
        K.Prop(C5.C(0), 188f, 1.6f, "Backtiles", 5, true);

        K.Player(3f);
        K.Hud();
        K.Camera(8.5f, 30f);

        K.Banner(3.8f, "The Usurper's Throne", "Here it ends");
        K.Note(16f, "Your Own Portrait",
            "Your face, slashed through with a blade. Beneath it someone has written:\n\n\"The traitor captain.\"\n\n" +
            "Malvric has told the realm you betrayed the king. Only you know the truth.", C5.P(1), 1.1f);
        K.Shrine("Shrine of the Last Vigil", 98f);

        Color guard = new Color(0.85f, 0.55f, 0.55f), palace = new Color(0.9f, 0.8f, 1f);
        K.Enemy(K.Kind.EliteSkeleton, 34f, "Throne Guard", guard);
        K.Enemy(K.Kind.EliteNinja, 62f, "Malvric's Shadow", palace);
        K.Enemy(K.Kind.EliteSkeleton, 84f, "Throne Guard", guard, 0, 0, 0, false);

        // Summon templates for phase 3 (configured, inactive copies)
        var t1 = K.Enemy(K.Kind.EliteSkeleton, 125f, "Risen Royal Guard", guard); t1.SetActive(false);
        var t2 = K.Enemy(K.Kind.EliteSkeleton, 165f, "Risen Royal Guard", guard); t2.SetActive(false);

        var malvric = BuildMalvric(150f);
        var pts = new[] { K.Child(K.Content, "Summon West", new Vector3(118f, K.FloorY(118f) + 3f, 0)), K.Child(K.Content, "Summon East", new Vector3(172f, K.FloorY(172f) + 3f, 0)) };
        var ai = malvric.GetComponent<BossKnightAI>();
        var so = new SerializedObject(ai);
        var sp = so.FindProperty("summonPrefabs"); sp.arraySize = 2; sp.GetArrayElementAtIndex(0).objectReferenceValue = t1; sp.GetArrayElementAtIndex(1).objectReferenceValue = t2;
        var pp = so.FindProperty("summonPoints"); pp.arraySize = 2; pp.GetArrayElementAtIndex(0).objectReferenceValue = pts[0]; pp.GetArrayElementAtIndex(1).objectReferenceValue = pts[1];
        so.ApplyModifiedPropertiesWithoutUndo();
        K.BossArena(114f, ai, "Lord Malvric, the Usurper", "\"Sir Aldric. I buried you myself.\"",
            "The Usurper Is Dead", "Vengeance is done. The crown is free.");

        K.Pickup("crimson_flask", 50f);
        K.Pickup("verdant_tonic", 100f, 1.2f);
        K.Pickup("prism_relic", 69f, 1.4f, 1, "relic_throne");
        K.Coins(4, 30, 1.3f, 6);

        K.Atmosphere(new Color(1f, 0.75f, 0.72f), 0.68f, new Color(0.7f, 0.4f, 0.45f), 0.08f,
            new K.VolumeLook("ThronePost", 0.45f, 1f, 16f, -6f, new Color(1f, 0.92f, 0.9f)),
            "Embers", new Color(1f, 0.45f, 0.3f, 0.7f), new Color(1f, 0.8f, 0.5f, 0.7f));

        // Victory: the crown shard on the throne, then the walk to the credits.
        K.Pickup("frozen_crown", 182f, 1.3f, 1, "crown_shard");
        K.Exit(186f, "Credits", new[] { malvric }, new Color(0.9f, 0.15f, 0.2f), "Malvric still lives");
        K.Save(scene);
        return K.Log.ToString();
    }

    static GameObject BuildMalvric(float x)
    {
        var go = new GameObject("Lord Malvric");
        go.transform.SetParent(K.Enemies, false);
        go.layer = LayerMask.NameToLayer("Enemy");
        go.transform.localScale = new Vector3(1.15f, 1.2f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Players/SoulslikeKnight/SoulslikeKnight_SpriteSheets/SoulslikeKnight_Idle.png").OfType<Sprite>().OrderBy(s => s.name.Length).ThenBy(s => s.name).First();
        sr.sortingLayerName = "player";
        sr.flipX = true;
        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Players/SoulslikeKnight/SoulslikeKnight_SpriteSheets/Player.controller");
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f; rb.freezeRotation = true; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(2.0f, 3.25f); box.offset = new Vector2(0.92f, 0f);
        box.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Art/Generated/PlayerNoFriction.physicsMaterial2D");
        go.AddComponent<FlipColliderSync>();
        var ai = go.AddComponent<BossKnightAI>();
        var core = go.AddComponent<EnemyCore>();
        core.Configure("Lord Malvric", 600, 40, 60, true, K.L("ember_of_vengeance", 1f, 2, 2));
        go.transform.position = new Vector3(x, K.FloorY(x) + 1.95f * 1.2f / 1.2f + 0.1f, 0);
        UIKit.Light("Malvric Aura", go.transform, Light2D.LightType.Point, go.transform.position + Vector3.up, new Color(1f, 0.2f, 0.25f), 1f, 5f, 0.5f);
        return go;
    }
}
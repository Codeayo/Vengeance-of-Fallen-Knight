using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent full-screen fade used for every scene change. Call SceneFader.LoadScene(name).
/// Creates itself on first use and fades in automatically after each load.
/// </summary>
public class SceneFader : MonoBehaviour
{
    private static SceneFader instance;

    private CanvasGroup group;
    private bool busy;
    private const float FadeOutTime = 0.6f;
    private const float FadeInTime = 0.8f;

    public static bool IsLoading => instance != null && instance.busy;

    public static SceneFader Instance
    {
        get
        {
            if (instance == null)
                Create();
            return instance;
        }
    }

    private static void Create()
    {
        var go = new GameObject("[SceneFader]", typeof(Canvas), typeof(CanvasGroup));
        DontDestroyOnLoad(go);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var image = new GameObject("Black", typeof(RectTransform), typeof(Image));
        image.transform.SetParent(go.transform, false);
        var rect = (RectTransform)image.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        image.GetComponent<Image>().color = new Color(0.02f, 0.015f, 0.03f, 1f);

        instance = go.AddComponent<SceneFader>();
        instance.group = go.GetComponent<CanvasGroup>();
        instance.group.alpha = 0f;
        instance.group.blocksRaycasts = false;
    }

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"Scene '{sceneName}' cannot be loaded. Check Build Settings.");
            return;
        }

        if (IsLoading)
            return;

        Instance.StartCoroutine(Instance.LoadRoutine(sceneName));
    }

    /// <summary>Fades in from black, e.g. when a scene opens directly.</summary>
    public static void FadeFromBlack(float duration = FadeInTime)
    {
        Instance.StopAllCoroutines();
        Instance.busy = false;
        Instance.StartCoroutine(Instance.Fade(1f, 0f, duration));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        busy = true;
        group.blocksRaycasts = true;
        yield return Fade(group.alpha, 1f, FadeOutTime);

        Time.timeScale = 1f;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone)
            yield return null;

        yield return null;
        busy = false;
        yield return Fade(1f, 0f, FadeInTime);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        group.blocksRaycasts = to > 0.5f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }

        group.alpha = to;
        group.blocksRaycasts = to > 0.5f;
    }
}

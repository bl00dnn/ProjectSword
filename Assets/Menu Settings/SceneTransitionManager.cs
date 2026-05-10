using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SceneTransitionManager : MonoBehaviour
{
    private const float DefaultFadeDuration = 1.15f;
    private static SceneTransitionManager instance;

    private CanvasGroup fadeGroup;
    private bool isTransitioning;

    public static SceneTransitionManager Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("SceneTransitionManager");
        DontDestroyOnLoad(managerObject);
        instance = managerObject.AddComponent<SceneTransitionManager>();
    }

    public static void LoadScene(string sceneName)
    {
        Instance.LoadSceneWithFade(sceneName, DefaultFadeDuration);
    }

    public void LoadSceneWithFade(string sceneName, float fadeDuration)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || isTransitioning)
        {
            return;
        }

        StartCoroutine(TransitionRoutine(sceneName, Mathf.Max(0.05f, fadeDuration)));
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildFadeCanvas();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isTransitioning && fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
    }

    private IEnumerator TransitionRoutine(string sceneName, float fadeDuration)
    {
        isTransitioning = true;
        Time.timeScale = 1f;

        yield return FadeTo(1f, fadeDuration);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (loadOperation != null && !loadOperation.isDone)
        {
            yield return null;
        }

        yield return null;
        yield return FadeTo(0f, fadeDuration);

        fadeGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        fadeGroup.blocksRaycasts = true;
        float startAlpha = fadeGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        fadeGroup.alpha = targetAlpha;
    }

    private void BuildFadeCanvas()
    {
        GameObject canvasObject = new GameObject("SceneFadeCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        fadeGroup = canvasObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.color = Color.black;
    }
}

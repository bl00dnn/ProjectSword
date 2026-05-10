using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class GamePauseMenuController : MonoBehaviour
{
    private const string GameSceneName = "Game";
    private const string MainMenuSceneName = "Menu";
    private const string PauseTitle = "\u041f\u0410\u0423\u0417\u0410";
    private const string MainMenuButtonText = "\u0413\u041b\u0410\u0412\u041d\u041e\u0415 \u041c\u0415\u041d\u042e";
    private const string QuitGameButtonText = "\u0412\u042b\u0419\u0422\u0418 \u0418\u0417 \u0418\u0413\u0420\u042b";
    private static GamePauseMenuController instance;
    private static bool sceneLoadHooked;

    private GameObject menuRoot;
    private bool isOpen;
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadHook()
    {
        if (sceneLoadHooked)
        {
            return;
        }

        sceneLoadHooked = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;

        if (scene.name != GameSceneName || instance != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("GamePauseMenuController");
        SceneManager.MoveGameObjectToScene(controllerObject, scene);
        instance = controllerObject.AddComponent<GamePauseMenuController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildMenu();
        SetMenuOpen(false);
    }

    private void Update()
    {
        if (WasPausePressed())
        {
            SetMenuOpen(!isOpen);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        Time.timeScale = 1f;
    }

    private bool WasPausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void SetMenuOpen(bool open)
    {
        if (menuRoot == null)
        {
            return;
        }

        if (open && !isOpen)
        {
            previousLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
        }

        isOpen = open;
        menuRoot.SetActive(open);
        Time.timeScale = open ? 0f : 1f;

        if (open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = previousLockState;
            Cursor.visible = previousCursorVisible;
        }
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.LoadScene(MainMenuSceneName);
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildMenu()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("GamePauseMenuCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        menuRoot = canvasObject;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        Image overlay = CreateImage("PauseOverlay", root, new Color(0.02f, 0.025f, 0.03f, 0.78f));
        StretchToParent(overlay.rectTransform);

        CreateText("PauseTitle", root, PauseTitle, 58, FontStyle.Bold, new Color(0.92f, 0.84f, 0.68f), new Vector2(0f, 108f), new Vector2(480f, 90f));
        CreateButton(root, "MainMenuButton", MainMenuButtonText, new Vector2(0f, -18f), new Vector2(460f, 82f), ReturnToMainMenu);
        CreateButton(root, "QuitGameButton", QuitGameButtonText, new Vector2(0f, -124f), new Vector2(460f, 82f), QuitGame);
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            InputSystemUIInputModule inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        StandaloneInputModule legacyInput = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyInput != null)
        {
            legacyInput.enabled = false;
        }
    }

    private static Image CreateImage(string name, RectTransform parent, Color color)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Text CreateText(string name, RectTransform parent, string value, int fontSize, FontStyle style, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(14, fontSize - 14);
        text.resizeTextMaxSize = fontSize;
        return text;
    }

    private static void CreateButton(RectTransform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.72f, 0.5f, 0.24f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.72f, 0.5f, 0.24f, 0.95f);
        colors.highlightedColor = new Color(0.95f, 0.72f, 0.36f, 1f);
        colors.pressedColor = new Color(0.48f, 0.31f, 0.13f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.onClick.AddListener(action);

        Text text = CreateText("Label", rect, label, 30, FontStyle.Bold, new Color(0.08f, 0.07f, 0.06f), Vector2.zero, size);
        text.alignment = TextAnchor.MiddleCenter;
    }
}

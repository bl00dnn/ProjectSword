using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Menu.unity";
    private const string SergiusPrefabPath = "Assets/Character/Sergius.prefab";
    private const string MaterialsFolder = "Assets/Menu/Materials";
    private const string LevelSceneName = "Game";
    private const string AutoBuildSessionKey = "ProjectSword.MainMenuSceneSetup.AutoBuildMenu.V1";

    [InitializeOnLoadMethod]
    private static void BuildOpenMenuSceneOnceAfterReload()
    {
        if (SessionState.GetBool(AutoBuildSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoBuildSessionKey, true);
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                return;
            }

            if (GameObject.Find("MainMenuCanvas") != null)
            {
                return;
            }

            BuildMenu();
        };
    }

    [MenuItem("Project Sword/Setup Main Menu Scene")]
    public static void BuildMenu()
    {
        EnsureFolder("Assets/Menu");
        EnsureFolder(MaterialsFolder);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RemoveIfExists("Menu_3D_Environment_Placeholder");
        RemoveIfExists("Menu_Sergius_Showcase");
        RemoveIfExists("MainMenuCanvas");
        RemoveIfExists("EventSystem");

        Camera camera = EnsureMainCamera(scene);
        ConfigureMainLight(scene);
        ConfigureController(scene);
        BuildEnvironment(scene);
        BuildSergius(scene);
        BuildCanvas(scene);

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.075f, 0.082f, 0.095f);
        RenderSettings.fogDensity = 0.018f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.18f, 0.2f, 0.24f);
        RenderSettings.ambientEquatorColor = new Color(0.1f, 0.09f, 0.08f);
        RenderSettings.ambientGroundColor = new Color(0.04f, 0.035f, 0.03f);

        EditorUtility.SetDirty(camera);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Main menu scene layout has been created.");
    }

    private static Camera EnsureMainCamera(Scene scene)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.transform.SetPositionAndRotation(new Vector3(0f, 1.35f, -7.5f), Quaternion.Euler(6f, 0f, 0f));
        camera.fieldOfView = 44f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.065f, 0.075f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 80f;
        return camera;
    }

    private static void ConfigureMainLight(Scene scene)
    {
        Light light = Object.FindAnyObjectByType<Light>();
        if (light == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            light = lightObject.AddComponent<Light>();
        }

        light.name = "Menu Key Light";
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.82f, 0.58f);
        light.intensity = 1.45f;
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(38f, -32f, 0f);

        GameObject fill = GameObject.Find("Menu Blue Fill Light");
        if (fill == null)
        {
            fill = new GameObject("Menu Blue Fill Light");
            SceneManager.MoveGameObjectToScene(fill, scene);
            Light fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = 8f;
            fillLight.intensity = 2.2f;
            fillLight.color = new Color(0.32f, 0.55f, 1f);
        }

        fill.transform.position = new Vector3(-4f, 2f, -1.5f);
    }

    private static void ConfigureController(Scene scene)
    {
        MainMenuController controller = Object.FindAnyObjectByType<MainMenuController>();
        if (controller == null)
        {
            GameObject controllerObject = new GameObject("MainMenuController");
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
            controller = controllerObject.AddComponent<MainMenuController>();
        }

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("levelSceneName").stringValue = LevelSceneName;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void BuildEnvironment(Scene scene)
    {
        GameObject root = new GameObject("Menu_3D_Environment_Placeholder");
        SceneManager.MoveGameObjectToScene(root, scene);

        Material floorMaterial = GetMaterial("M_Menu_DarkStone", new Color(0.11f, 0.105f, 0.095f), 0.15f);
        Material wallMaterial = GetMaterial("M_Menu_BackWall", new Color(0.08f, 0.085f, 0.095f), 0.05f);
        Material brassMaterial = GetMaterial("M_Menu_OldBrass", new Color(0.55f, 0.39f, 0.18f), 0.2f);

        CreateCube("Stone Floor Placeholder", root.transform, new Vector3(-1.65f, -0.08f, 0.8f), new Vector3(7.5f, 0.16f, 5.2f), floorMaterial);
        CreateCube("Rear Dungeon Wall Placeholder", root.transform, new Vector3(-1.65f, 1.65f, 2.75f), new Vector3(7.5f, 3.5f, 0.18f), wallMaterial);
        CreateCube("Left Depth Wall Placeholder", root.transform, new Vector3(-5.15f, 1.35f, 0.6f), new Vector3(0.16f, 2.9f, 4.4f), wallMaterial);
        CreateCube("Right UI Shadow Wall Placeholder", root.transform, new Vector3(2.05f, 1.35f, 1.15f), new Vector3(0.16f, 2.9f, 3.2f), wallMaterial);

        CreateColumn("Left Column Placeholder", root.transform, new Vector3(-4.35f, 0.75f, 1.75f), brassMaterial);
        CreateColumn("Center Column Placeholder", root.transform, new Vector3(-1.65f, 0.75f, 2.05f), brassMaterial);
        CreateColumn("Character Rim Column Placeholder", root.transform, new Vector3(0.1f, 0.75f, 1.75f), brassMaterial);
    }

    private static void BuildSergius(Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SergiusPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("Sergius prefab was not found. The menu scene will keep only the placement marker.");
            return;
        }

        GameObject sergius = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (sergius == null)
        {
            sergius = Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(sergius, scene);
        }

        sergius.name = "Menu_Sergius_Showcase";
        sergius.transform.SetPositionAndRotation(new Vector3(-2.65f, 0f, -0.15f), Quaternion.Euler(0f, 138f, 0f));
        sergius.transform.localScale = Vector3.one * 1.05f;

        DisableIfPresent<SergiusThirdPersonController>(sergius);
        DisableIfPresent<SergiusFootstepAudio>(sergius);
        DisableIfPresent<AudioSource>(sergius);

        MenuCharacterShowcase showcase = sergius.GetComponent<MenuCharacterShowcase>();
        if (showcase == null)
        {
            showcase = sergius.AddComponent<MenuCharacterShowcase>();
        }

        EditorUtility.SetDirty(showcase);
    }

    private static void BuildCanvas(Scene scene)
    {
        GameObject canvasObject = new GameObject("MainMenuCanvas");
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject eventSystem = new GameObject("EventSystem");
        SceneManager.MoveGameObjectToScene(eventSystem, scene);
        eventSystem.AddComponent<EventSystem>();
        InputSystemUIInputModule inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        CreateText("GameTitle", root, "PROJECT SWORD", 52, FontStyle.Bold, new Color(0.92f, 0.84f, 0.68f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-470f, 118f), new Vector2(520f, 90f));
        CreateText("CharacterName", root, "СЕРГИУС", 24, FontStyle.Normal, new Color(0.58f, 0.72f, 0.95f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-470f, 58f), new Vector2(520f, 46f));
        CreateButton(root, "PlayButton", "ИГРАТЬ", new Vector2(-470f, -58f), new Vector2(420f, 86f));
    }

    private static void CreateButton(RectTransform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.72f, 0.5f, 0.24f, 0.92f);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.72f, 0.5f, 0.24f, 0.92f);
        colors.highlightedColor = new Color(0.95f, 0.72f, 0.36f, 1f);
        colors.pressedColor = new Color(0.48f, 0.31f, 0.13f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        Text text = CreateText("Label", rect, label, 34, FontStyle.Bold, new Color(0.08f, 0.07f, 0.06f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        text.alignment = TextAnchor.MiddleCenter;

        MainMenuController controller = Object.FindAnyObjectByType<MainMenuController>();
        if (controller != null)
        {
            UnityEventTools.AddPersistentListener(button.onClick, controller.PlayGame);
            EditorUtility.SetDirty(button);
        }
    }

    private static Text CreateText(string name, RectTransform parent, string value, int fontSize, FontStyle fontStyle, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(12, fontSize - 10);
        text.resizeTextMaxSize = fontSize;
        return text;
    }

    private static void CreateColumn(string name, Transform parent, Vector3 position, Material material)
    {
        CreateCube(name + " Shaft", parent, position + Vector3.up * 0.45f, new Vector3(0.28f, 1.85f, 0.28f), material);
        CreateCube(name + " Base", parent, position - Vector3.up * 0.48f, new Vector3(0.52f, 0.22f, 0.52f), material);
        CreateCube(name + " Cap", parent, position + Vector3.up * 1.4f, new Vector3(0.58f, 0.2f, 0.58f), material);
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        return cube;
    }

    private static Material GetMaterial(string name, Color color, float metallic)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (material.shader == null)
            {
                material = new Material(Shader.Find("Standard"));
            }

            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.32f);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void DisableIfPresent<T>(GameObject root) where T : Behaviour
    {
        T component = root.GetComponent<T>();
        if (component != null)
        {
            component.enabled = false;
            EditorUtility.SetDirty(component);
        }
    }

    private static void RemoveIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(folder)?.Replace("\\", "/");
        string name = System.IO.Path.GetFileName(folder);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }
}

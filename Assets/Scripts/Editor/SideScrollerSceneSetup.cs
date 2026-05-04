using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SideScrollerSceneSetup
{
    private const string FirstLevelScenePath = "Assets/Scenes/1st2ndLevelScene.unity";

    [MenuItem("ProjectSword/Setup 2.5D Side Scroller Scene")]
    public static void SetupScene()
    {
        SetupActiveScene();
    }

    [MenuItem("ProjectSword/Create Player Spawn Point")]
    public static void CreatePlayerSpawnPoint()
    {
        GameObject player = GameObject.Find("Player") ?? GameObject.Find("Capsule");
        Vector3 spawnPosition = player != null ? player.transform.position : Vector3.zero;

        GameObject spawnObject = GameObject.Find("Player Spawn Point");
        if (spawnObject == null)
        {
            spawnObject = new GameObject("Player Spawn Point");
        }

        spawnObject.transform.position = spawnPosition;

        if (spawnObject.GetComponent<PlayerSpawnPoint>() == null)
        {
            spawnObject.AddComponent<PlayerSpawnPoint>();
        }

        Selection.activeGameObject = spawnObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    public static void SetupFirstLevelScene()
    {
        EditorSceneManager.OpenScene(FirstLevelScenePath, OpenSceneMode.Single);
        SetupActiveScene();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    private static void SetupActiveScene()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = GameObject.Find("Capsule");
        }

        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1.2f, 0f);
            player.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
        }
        else if (player.name == "Capsule")
        {
            player.name = "Player";
        }

        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = player.AddComponent<Rigidbody>();
        }

        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        if (player.GetComponent<SideScrollerPlayerController>() == null)
        {
            player.AddComponent<SideScrollerPlayerController>();
        }

        if (GameObject.Find("Ground") == null && GameObject.Find("ground") == null)
        {
            CreatePlatform("Ground", new Vector3(0f, -0.25f, 0f), new Vector3(18f, 0.5f, 2.5f));
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.orthographic = false;
        camera.fieldOfView = 45f;
        camera.transform.position = player.transform.position + new Vector3(0f, 5f, -9f);

        SideScrollerCameraFollow follow = camera.GetComponent<SideScrollerCameraFollow>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<SideScrollerCameraFollow>();
        }

        follow.Target = player.transform;
        follow.SnapToTarget();

        if (Object.FindAnyObjectByType<SideScrollerSceneBootstrap>() == null)
        {
            new GameObject("2.5D Scene Bootstrap").AddComponent<SideScrollerSceneBootstrap>();
        }

        if (Object.FindAnyObjectByType<PlayerSpawnPoint>() == null)
        {
            GameObject spawnObject = new GameObject("Player Spawn Point");
            spawnObject.transform.position = player.transform.position;
            spawnObject.AddComponent<PlayerSpawnPoint>();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void CreatePlatform(string name, Vector3 position, Vector3 scale)
    {
        GameObject platform = GameObject.Find(name);
        if (platform == null)
        {
            platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
        }

        platform.transform.position = position;
        platform.transform.localScale = scale;
        platform.isStatic = true;

        Collider collider = platform.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false;
        }
    }
}

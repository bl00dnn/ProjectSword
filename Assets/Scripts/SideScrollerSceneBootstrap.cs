using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SideScrollerSceneBootstrap : MonoBehaviour
{
    [SerializeField] private SideScrollerPlayerController player;
    [SerializeField] private SideScrollerCameraFollow cameraFollow;

    private static bool isSubscribedToSceneLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SubscribeToSceneLoaded()
    {
        if (isSubscribedToSceneLoaded)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        isSubscribedToSceneLoaded = true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSideScrollerScene(false);
    }

    private static void EnsureSideScrollerScene(bool forceSetup)
    {
        if (!forceSetup && !ShouldSetupCurrentScene())
        {
            return;
        }

        SideScrollerPlayerController existingPlayer = FindAnyObjectByType<SideScrollerPlayerController>();
        if (existingPlayer == null)
        {
            GameObject capsule = GameObject.Find("Capsule");
            existingPlayer = capsule != null ? ConfigurePlayer(capsule) : CreateDefaultPlayer();
        }

        MovePlayerToSpawnPoint(existingPlayer);

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
        camera.transform.rotation = Quaternion.identity;

        SideScrollerCameraFollow follow = camera.GetComponent<SideScrollerCameraFollow>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<SideScrollerCameraFollow>();
        }

        follow.Target = existingPlayer.transform;
        follow.SnapToTarget();
    }

    private void Awake()
    {
        EnsureSideScrollerScene(true);

        if (player == null)
        {
            player = FindAnyObjectByType<SideScrollerPlayerController>();
        }

        if (cameraFollow == null && Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<SideScrollerCameraFollow>();
        }

        if (cameraFollow != null && cameraFollow.Target == null && player != null)
        {
            cameraFollow.Target = player.transform;
        }
    }

    private static bool ShouldSetupCurrentScene()
    {
        return FindAnyObjectByType<PlayerSpawnPoint>() != null
            || FindAnyObjectByType<SideScrollerSceneBootstrap>() != null
            || GameObject.Find("Player") != null
            || GameObject.Find("Capsule") != null;
    }

    private static SideScrollerPlayerController CreateDefaultPlayer()
    {
        GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObject.name = "Player";
        playerObject.transform.position = new Vector3(0f, 1.2f, 0f);
        playerObject.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);

        return ConfigurePlayer(playerObject);
    }

    private static SideScrollerPlayerController ConfigurePlayer(GameObject playerObject)
    {
        Rigidbody body = playerObject.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = playerObject.AddComponent<Rigidbody>();
        }

        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        SideScrollerPlayerController controller = playerObject.GetComponent<SideScrollerPlayerController>();
        if (controller == null)
        {
            controller = playerObject.AddComponent<SideScrollerPlayerController>();
        }

        return controller;
    }

    private static void MovePlayerToSpawnPoint(SideScrollerPlayerController playerController)
    {
        PlayerSpawnPoint spawnPoint = FindAnyObjectByType<PlayerSpawnPoint>();
        if (spawnPoint == null || playerController == null)
        {
            return;
        }

        Rigidbody body = playerController.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = spawnPoint.SpawnPosition;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        else
        {
            playerController.transform.position = spawnPoint.SpawnPosition;
        }
    }

    private static void CreatePlatform(string name, Vector3 position, Vector3 scale)
    {
        if (GameObject.Find(name) != null)
        {
            return;
        }

        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = name;
        platform.transform.position = position;
        platform.transform.localScale = scale;
        platform.isStatic = true;
    }
}

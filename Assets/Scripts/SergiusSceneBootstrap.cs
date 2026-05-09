using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class SergiusSceneBootstrap : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private string characterName = "PlayerCapsule";
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private Transform characterRoot;

    [Header("Spawn")]
    [SerializeField] private PlayerSpawnPoint spawnPoint;
    [SerializeField] private string spawnObjectName = "PlayerSpawn";

    [Header("Third Person Camera")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0.45f, 1.85f, -3.25f);
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float followSmoothTime = 0.08f;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 55f;
    [SerializeField] private bool lockCursorOnPlay = true;

    private void Awake()
    {
        Transform player = ResolveCharacter();
        if (player == null)
        {
            Debug.LogError($"[{nameof(SergiusSceneBootstrap)}] Character '{characterName}' was not found and no prefab is assigned.", this);
            return;
        }

        ApplySpawn(player);
        ConfigureCamera(player);
    }

    private Transform ResolveCharacter()
    {
        if (characterRoot != null)
        {
            return characterRoot;
        }

        GameObject existing = GameObject.Find(characterName);
        if (existing != null)
        {
            characterRoot = existing.transform;
            return characterRoot;
        }

        if (characterPrefab == null)
        {
            return null;
        }

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        Transform spawn = ResolveSpawnTransform();
        if (spawn != null)
        {
            position = spawn.position;
            rotation = spawn.rotation;
        }

        GameObject instance = Instantiate(characterPrefab, position, rotation);
        instance.name = string.IsNullOrWhiteSpace(characterName) ? characterPrefab.name : characterName;
        characterRoot = instance.transform;
        return characterRoot;
    }

    private void Reset()
    {
        characterName = "PlayerCapsule";
        spawnObjectName = "PlayerSpawn";
    }

    private void ApplySpawn(Transform player)
    {
        Transform spawn = ResolveSpawnTransform();
        if (spawn == null)
        {
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (controller != null)
        {
            controller.enabled = false;
        }

        player.SetPositionAndRotation(spawn.position, spawn.rotation);

        if (controller != null)
        {
            controller.enabled = wasEnabled;
        }
    }

    private Transform ResolveSpawnTransform()
    {
        if (spawnPoint != null)
        {
            return spawnPoint.transform;
        }

        spawnPoint = FindAnyObjectByType<PlayerSpawnPoint>();
        if (spawnPoint != null)
        {
            return spawnPoint.transform;
        }

        GameObject namedSpawn = GameObject.Find(spawnObjectName);
        if (namedSpawn != null)
        {
            return namedSpawn.transform;
        }

        try
        {
            GameObject taggedSpawn = GameObject.FindWithTag("PlayerSpawn");
            if (taggedSpawn != null)
            {
                return taggedSpawn.transform;
            }
        }
        catch (UnityException)
        {
            // The tag is optional. A cube named PlayerSpawn is enough.
        }

        return null;
    }

    private void ConfigureCamera(Transform player)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }

        WitcherLikeCamera cameraRig = mainCamera.GetComponent<WitcherLikeCamera>();
        if (cameraRig == null)
        {
            cameraRig = mainCamera.gameObject.AddComponent<WitcherLikeCamera>();
        }

        cameraRig.Configure(player, cameraOffset, mouseSensitivity, followSmoothTime, minPitch, maxPitch, lockCursorOnPlay);

        SergiusThirdPersonController controller = player.GetComponent<SergiusThirdPersonController>();
        if (controller != null)
        {
            controller.SetCamera(mainCamera);
        }
    }
}

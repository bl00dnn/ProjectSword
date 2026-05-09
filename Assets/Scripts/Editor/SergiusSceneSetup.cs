using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SergiusSceneSetup
{
    private const string ScenePath = "Assets/Scenes/1st2ndLevelScene.unity";
    private const string CharacterModelPath = "Assets/Character/T-Pose.fbx";
    private const string CharacterPrefabPath = "Assets/Character/Sergius.prefab";
    private const string ControllerPath = "Assets/Character/Sergius.controller";
    private const string IdlePath = "Assets/Character/Idle.fbx";
    private const string WalkingPath = "Assets/Character/Walking.fbx";
    private const string RunPath = "Assets/Character/Slow Run.fbx";
    private const string IdleClipPath = "Assets/Character/Sergius_Idle.anim";
    private const string WalkingClipPath = "Assets/Character/Sergius_Walking.anim";
    private const string RunClipPath = "Assets/Character/Sergius_Run.anim";
    private static readonly string[] FootstepClipPaths =
    {
        "Assets/Character/Audio/Footstep_HardSurface_01.wav",
        "Assets/Character/Audio/Footstep_HardSurface_03.wav"
    };
    private const string CharacterName = "PlayerCapsule";
    private const string ExistingCapsuleName = "Capsule";
    private const string SpawnName = "PlayerSpawn";

    [InitializeOnLoadMethod]
    private static void BuildOnceWhenEditorReloads()
    {
        const string sessionKey = "ProjectSword.SergiusCharacterPrefabBuilt.V8";
        if (SessionState.GetBool(sessionKey, false))
        {
            return;
        }

        SessionState.SetBool(sessionKey, true);
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EnsureSergiusInScene();
            }
        };
    }

    [MenuItem("Project Sword/Setup Sergius In Scene")]
    public static void EnsureSergiusInScene()
    {
        ConfigureAnimationImports();
        BuildAnimationClipAssets();

        RuntimeAnimatorController animatorController = BuildAnimatorController();
        if (animatorController == null)
        {
            Debug.LogError("Sergius setup failed: Animator Controller was not created.");
            return;
        }

        GameObject prefab = BuildCharacterPrefab();
        if (prefab == null)
        {
            Debug.LogError("Sergius setup failed: configured character prefab was not created.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform spawn = FindOrCreateSpawn(scene).transform;
        GameObject character = FindExistingCharacterCapsule(scene);
        if (character != null)
        {
            ConfigureCharacterObject(character);
            SceneManager.MoveGameObjectToScene(character, scene);
        }
        else
        {
            RemoveDuplicateCharacters(scene);

            character = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (character == null)
            {
                character = Object.Instantiate(prefab);
                SceneManager.MoveGameObjectToScene(character, scene);
            }
        }

        character.name = CharacterName;
        character.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        SergiusSceneBootstrap bootstrap = Object.FindAnyObjectByType<SergiusSceneBootstrap>();
        if (bootstrap == null)
        {
            GameObject bootstrapObject = new GameObject("Sergius Gameplay Bootstrap");
            SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
            bootstrap = bootstrapObject.AddComponent<SergiusSceneBootstrap>();
        }

        SerializedObject bootstrapSerialized = new SerializedObject(bootstrap);
        bootstrapSerialized.FindProperty("characterName").stringValue = CharacterName;
        bootstrapSerialized.FindProperty("characterPrefab").objectReferenceValue = prefab;
        bootstrapSerialized.FindProperty("characterRoot").objectReferenceValue = character.transform;
        bootstrapSerialized.FindProperty("spawnPoint").objectReferenceValue = spawn.GetComponent<PlayerSpawnPoint>();
        bootstrapSerialized.FindProperty("spawnObjectName").stringValue = SpawnName;
        bootstrapSerialized.FindProperty("cameraOffset").vector3Value = new Vector3(0.45f, 1.85f, -3.25f);
        bootstrapSerialized.FindProperty("mouseSensitivity").floatValue = 0.12f;
        bootstrapSerialized.FindProperty("followSmoothTime").floatValue = 0.08f;
        bootstrapSerialized.FindProperty("minPitch").floatValue = -25f;
        bootstrapSerialized.FindProperty("maxPitch").floatValue = 55f;
        bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

        SergiusThirdPersonController thirdPersonController = character.GetComponent<SergiusThirdPersonController>();
        if (thirdPersonController != null)
        {
            thirdPersonController.SetCamera(Camera.main);
            EditorUtility.SetDirty(thirdPersonController);
        }

        EditorUtility.SetDirty(character);
        EditorUtility.SetDirty(bootstrap);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Sergius prefab and scene are wired with serialized character components.");
    }

    private static GameObject BuildCharacterPrefab()
    {
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

        if (controller == null)
        {
            Debug.LogError("Sergius prefab setup failed: missing Animator Controller.");
            return null;
        }

        GameObject root = new GameObject(CharacterName);
        ConfigureCharacterObject(root);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, CharacterPrefabPath);
        Object.DestroyImmediate(root);

        return savedPrefab;
    }

    private static void ConfigureCharacterObject(GameObject root)
    {
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModelPath);
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(CharacterModelPath);
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        AnimationClip walkingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkingClipPath);
        AnimationClip runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunClipPath);

        if (modelPrefab == null || controller == null || avatar == null)
        {
            Debug.LogError("Sergius character setup failed: missing T-Pose model, Avatar, or Animator Controller.");
            return;
        }

        Transform oldModel = root.transform.Find("Sergius Model");
        if (oldModel != null)
        {
            Object.DestroyImmediate(oldModel.gameObject);
        }

        GameObject model = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
        if (model == null)
        {
            model = Object.Instantiate(modelPrefab);
        }

        model.name = "Sergius Model";
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        AlignModelFeetToRoot(root.transform, model.transform);

        Animator animator = model.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = model.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.avatar = avatar;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        CharacterController characterController = root.GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = root.AddComponent<CharacterController>();
        }

        characterController.height = 1.95f;
        characterController.center = new Vector3(0f, 0.975f, 0f);
        characterController.radius = 0.35f;
        characterController.stepOffset = 0.45f;

        CapsuleCollider capsuleCollider = root.GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            capsuleCollider.enabled = false;
        }

        Renderer capsuleRenderer = root.GetComponent<Renderer>();
        if (capsuleRenderer != null)
        {
            capsuleRenderer.enabled = false;
        }

        SergiusAnimationDriver animationDriver = root.GetComponent<SergiusAnimationDriver>();
        if (animationDriver == null)
        {
            animationDriver = root.AddComponent<SergiusAnimationDriver>();
        }

        SerializedObject animationDriverSerialized = new SerializedObject(animationDriver);
        animationDriverSerialized.FindProperty("animator").objectReferenceValue = animator;
        animationDriverSerialized.FindProperty("idleClip").objectReferenceValue = idleClip;
        animationDriverSerialized.FindProperty("walkingClip").objectReferenceValue = walkingClip;
        animationDriverSerialized.FindProperty("runClip").objectReferenceValue = runClip;
        animationDriverSerialized.FindProperty("additionalClips").arraySize = 0;
        animationDriverSerialized.FindProperty("animatorController").objectReferenceValue = controller;
        animationDriverSerialized.FindProperty("characterAvatar").objectReferenceValue = avatar;
        animationDriverSerialized.FindProperty("speedParameter").stringValue = "Speed";
        animationDriverSerialized.FindProperty("sprintParameter").stringValue = "Sprint";
        animationDriverSerialized.FindProperty("jumpTrigger").stringValue = "Jump";
        animationDriverSerialized.FindProperty("driveClipsDirectly").boolValue = true;
        animationDriverSerialized.ApplyModifiedPropertiesWithoutUndo();

        SergiusThirdPersonController thirdPersonController = root.GetComponent<SergiusThirdPersonController>();
        if (thirdPersonController == null)
        {
            thirdPersonController = root.AddComponent<SergiusThirdPersonController>();
        }

        SerializedObject thirdPersonSerialized = new SerializedObject(thirdPersonController);
        thirdPersonSerialized.FindProperty("characterController").objectReferenceValue = characterController;
        thirdPersonSerialized.FindProperty("animationDriver").objectReferenceValue = animationDriver;
        thirdPersonSerialized.ApplyModifiedPropertiesWithoutUndo();

        AudioSource footstepSource = root.GetComponent<AudioSource>();
        if (footstepSource == null)
        {
            footstepSource = root.AddComponent<AudioSource>();
        }

        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = 1f;
        footstepSource.rolloffMode = AudioRolloffMode.Linear;
        footstepSource.minDistance = 1.5f;
        footstepSource.maxDistance = 14f;

        SergiusFootstepAudio footstepAudio = root.GetComponent<SergiusFootstepAudio>();
        if (footstepAudio == null)
        {
            footstepAudio = root.AddComponent<SergiusFootstepAudio>();
        }

        SerializedObject footstepSerialized = new SerializedObject(footstepAudio);
        footstepSerialized.FindProperty("characterController").objectReferenceValue = characterController;
        footstepSerialized.FindProperty("audioSource").objectReferenceValue = footstepSource;
        footstepSerialized.FindProperty("volume").floatValue = 0.35f;

        SerializedProperty clipsProperty = footstepSerialized.FindProperty("footstepClips");
        clipsProperty.arraySize = FootstepClipPaths.Length;
        for (int i = 0; i < FootstepClipPaths.Length; i++)
        {
            clipsProperty.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(FootstepClipPaths[i]);
        }

        footstepSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(root);
    }

    private static RuntimeAnimatorController BuildAnimatorController()
    {
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        AnimationClip walkingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkingClipPath);
        AnimationClip runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunClipPath);

        if (idleClip == null || walkingClip == null || runClip == null)
        {
            Debug.LogError("Sergius controller setup failed: missing Sergius_Idle, Sergius_Walking, or Sergius_Run animation clip.");
            return null;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        while (controller.layers.Length > 1)
        {
            controller.RemoveLayer(controller.layers.Length - 1);
        }

        controller.parameters = new AnimatorControllerParameter[0];
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Sprint", AnimatorControllerParameterType.Bool);

        AnimatorControllerLayer layer = controller.layers[0];
        layer.name = "Base Layer";
        layer.iKPass = true;

        AnimatorStateMachine stateMachine = layer.stateMachine;
        ClearStateMachine(stateMachine);

        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(300f, 80f, 0f));
        idleState.motion = idleClip;
        idleState.iKOnFeet = true;

        AnimatorState walkingState = stateMachine.AddState("Walking", new Vector3(540f, 80f, 0f));
        walkingState.motion = walkingClip;
        walkingState.iKOnFeet = true;

        AnimatorState runState = stateMachine.AddState("Run", new Vector3(780f, 80f, 0f));
        runState.motion = runClip;
        runState.iKOnFeet = true;

        stateMachine.defaultState = idleState;

        AnimatorStateTransition idleToWalking = idleState.AddTransition(walkingState);
        idleToWalking.hasExitTime = false;
        idleToWalking.duration = 0.15f;
        idleToWalking.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");
        idleToWalking.AddCondition(AnimatorConditionMode.IfNot, 0f, "Sprint");

        AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.15f;
        idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");
        idleToRun.AddCondition(AnimatorConditionMode.If, 0f, "Sprint");

        AnimatorStateTransition walkingToIdle = walkingState.AddTransition(idleState);
        walkingToIdle.hasExitTime = false;
        walkingToIdle.duration = 0.15f;
        walkingToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");

        AnimatorStateTransition walkingToRun = walkingState.AddTransition(runState);
        walkingToRun.hasExitTime = false;
        walkingToRun.duration = 0.15f;
        walkingToRun.AddCondition(AnimatorConditionMode.If, 0f, "Sprint");

        AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.15f;
        runToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");

        AnimatorStateTransition runToWalking = runState.AddTransition(walkingState);
        runToWalking.hasExitTime = false;
        runToWalking.duration = 0.15f;
        runToWalking.AddCondition(AnimatorConditionMode.IfNot, 0f, "Sprint");

        controller.layers = new[] { layer };
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        return controller;
    }

    private static void BuildAnimationClipAssets()
    {
        CreateAnimationClipAsset(IdlePath, IdleClipPath, "Sergius_Idle", true);
        CreateAnimationClipAsset(WalkingPath, WalkingClipPath, "Sergius_Walking", true);
        CreateAnimationClipAsset(RunPath, RunClipPath, "Sergius_Run", true);
    }

    private static void CreateAnimationClipAsset(string sourcePath, string destinationPath, string clipName, bool loop)
    {
        AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(sourcePath);
        if (sourceClip == null)
        {
            Debug.LogError($"Sergius animation setup failed: missing source clip at {sourcePath}.");
            return;
        }

        AnimationClip generatedClip = Object.Instantiate(sourceClip);
        generatedClip.name = clipName;
        generatedClip.wrapMode = loop ? WrapMode.Loop : WrapMode.Default;

        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
        if (existingClip == null)
        {
            AssetDatabase.CreateAsset(generatedClip, destinationPath);
            existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
        }
        else
        {
            EditorUtility.CopySerialized(generatedClip, existingClip);
        }

        if (existingClip != null)
        {
            existingClip.name = clipName;
            existingClip.wrapMode = loop ? WrapMode.Loop : WrapMode.Default;

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(existingClip);
            settings.loopTime = loop;
            settings.loopBlend = loop;
            AnimationUtility.SetAnimationClipSettings(existingClip, settings);
            EditorUtility.SetDirty(existingClip);
        }

        Object.DestroyImmediate(generatedClip);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureAnimationImports()
    {
        ConfigureAnimationImport(CharacterModelPath, false);
        ConfigureAnimationImport(IdlePath, true);
        ConfigureAnimationImport(WalkingPath, true);
        ConfigureAnimationImport(RunPath, true);
    }

    private static void ConfigureAnimationImport(string path, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"Sergius import setup failed: missing model importer at {path}.");
            return;
        }

        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            changed = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }

        if (!importer.importAnimation)
        {
            importer.importAnimation = true;
            changed = true;
        }

        WrapMode wrapMode = loop ? WrapMode.Loop : WrapMode.Default;
        if (importer.animationWrapMode != wrapMode)
        {
            importer.animationWrapMode = wrapMode;
            changed = true;
        }

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].loopTime != loop)
            {
                clips[i].loopTime = loop;
                changed = true;
            }

            if (clips[i].loopPose != loop)
            {
                clips[i].loopPose = loop;
                changed = true;
            }
        }

        if (changed || importer.clipAnimations.Length != clips.Length)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    private static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        ChildAnimatorState[] states = stateMachine.states;
        for (int i = states.Length - 1; i >= 0; i--)
        {
            stateMachine.RemoveState(states[i].state);
        }

        AnimatorStateTransition[] anyStateTransitions = stateMachine.anyStateTransitions;
        for (int i = anyStateTransitions.Length - 1; i >= 0; i--)
        {
            stateMachine.RemoveAnyStateTransition(anyStateTransitions[i]);
        }

        ChildAnimatorStateMachine[] stateMachines = stateMachine.stateMachines;
        for (int i = stateMachines.Length - 1; i >= 0; i--)
        {
            stateMachine.RemoveStateMachine(stateMachines[i].stateMachine);
        }
    }

    private static void AlignModelFeetToRoot(Transform root, Transform model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float rootY = root.position.y;
        float feetOffset = rootY - bounds.min.y;
        if (Mathf.Abs(feetOffset) > 0.001f)
        {
            model.position += Vector3.up * feetOffset;
        }
    }

    private static void RemoveDuplicateCharacters(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = roots.Length - 1; i >= 0; i--)
        {
            if (roots[i].name == CharacterName)
            {
                Object.DestroyImmediate(roots[i]);
            }
        }
    }

    private static GameObject FindExistingCharacterCapsule(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == CharacterName || roots[i].name == ExistingCapsuleName)
            {
                return roots[i];
            }
        }

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].GetComponent<CharacterController>() != null)
            {
                return roots[i];
            }
        }

        for (int i = 0; i < roots.Length; i++)
        {
            CapsuleCollider capsule = roots[i].GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                return roots[i];
            }
        }

        return null;
    }

    private static GameObject FindOrCreateSpawn(Scene scene)
    {
        GameObject spawn = GameObject.Find(SpawnName);
        if (spawn != null)
        {
            return spawn;
        }

        spawn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spawn.name = SpawnName;
        SceneManager.MoveGameObjectToScene(spawn, scene);
        spawn.transform.position = new Vector3(16.006264f, 4.657f, -6.144177f);
        spawn.transform.localScale = new Vector3(0.6f, 0.15f, 0.6f);
        spawn.AddComponent<PlayerSpawnPoint>();
        return spawn;
    }
}

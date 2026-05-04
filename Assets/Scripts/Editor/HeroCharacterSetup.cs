using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HeroCharacterSetup
{
    private const string ScenePath = "Assets/Scenes/1st2ndLevelScene.unity";
    private const string HeroModelPath = "Assets/Character/T-Pose.fbx";
    private const string IdleModelPath = "Assets/Character/Idle.fbx";
    private const string WalkingModelPath = "Assets/Character/Walking.fbx";
    private const string ControllerPath = "Assets/Character/Hero.controller";
    private const float DefaultColliderHeight = 1.8f;

    [MenuItem("ProjectSword/Setup Hero Character")]
    public static void SetupHeroInActiveScene()
    {
        SetupHero();
    }

    public static void SetupHeroInLevelScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SetupHero();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    private static void SetupHero()
    {
        GameObject player = GameObject.Find("Player") ?? GameObject.Find("Capsule");
        if (player == null)
        {
            Debug.LogError("Player object was not found in the active scene.");
            return;
        }

        player.name = "Player";
        EnsurePlayerComponents(player);
        HidePrimitiveCapsule(player);
        ConfigureAnimationImports();

        AnimatorController controller = CreateOrUpdateAnimatorController();
        GameObject visual = CreateOrReplaceHeroVisual(player.transform);

        Animator animator = visual.GetComponent<Animator>();
        if (animator == null)
        {
            animator = visual.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        PlayerVisualAnimator visualAnimator = player.GetComponent<PlayerVisualAnimator>();
        if (visualAnimator == null)
        {
            visualAnimator = player.AddComponent<PlayerVisualAnimator>();
        }

        SerializedObject serializedAnimator = new SerializedObject(visualAnimator);
        serializedAnimator.FindProperty("animator").objectReferenceValue = animator;
        serializedAnimator.FindProperty("fullWalkSpeed").floatValue = 4f;
        serializedAnimator.FindProperty("locomotionStateName").stringValue = "Locomotion";
        serializedAnimator.FindProperty("keepVisualRootFixed").boolValue = true;
        serializedAnimator.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void EnsurePlayerComponents(GameObject player)
    {
        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = player.AddComponent<Rigidbody>();
        }

        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        player.transform.localScale = Vector3.one;

        CapsuleCollider collider = player.GetComponent<CapsuleCollider>();
        if (collider == null)
        {
            collider = player.AddComponent<CapsuleCollider>();
        }

        collider.radius = 0.4f;
        collider.height = DefaultColliderHeight;
        collider.center = new Vector3(0f, DefaultColliderHeight * 0.5f, 0f);

        if (player.GetComponent<SideScrollerPlayerController>() == null)
        {
            player.AddComponent<SideScrollerPlayerController>();
        }
    }

    private static void HidePrimitiveCapsule(GameObject player)
    {
        MeshRenderer renderer = player.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Object.DestroyImmediate(renderer);
        }

        MeshFilter filter = player.GetComponent<MeshFilter>();
        if (filter != null)
        {
            Object.DestroyImmediate(filter);
        }
    }

    private static AnimatorController CreateOrUpdateAnimatorController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        controller.parameters = controller.parameters.Where(parameter => parameter.name != "Speed").ToArray();
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState state in stateMachine.states)
        {
            stateMachine.RemoveState(state.state);
        }

        BlendTree blendTree = new BlendTree
        {
            name = "Locomotion",
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };

        AssetDatabase.AddObjectToAsset(blendTree, controller);
        AnimationClip idleClip = FindFirstClip(IdleModelPath);
        AnimationClip walkingClip = FindFirstClip(WalkingModelPath);

        blendTree.AddChild(idleClip, 0f);
        blendTree.AddChild(walkingClip, 0.25f);
        blendTree.AddChild(walkingClip, 1f);

        AnimatorState locomotion = stateMachine.AddState("Locomotion");
        locomotion.motion = blendTree;
        stateMachine.defaultState = locomotion;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void ConfigureAnimationImports()
    {
        ConfigureLoopingClip(IdleModelPath, true);
        ConfigureLoopingClip(WalkingModelPath, true);
        ConfigureLoopingClip(HeroModelPath, false);
    }

    private static void ConfigureLoopingClip(string modelPath, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"Model importer was not found for {modelPath}.");
            return;
        }

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].loopTime = loop;
            clips[i].loopPose = loop;
            clips[i].lockRootRotation = true;
            clips[i].lockRootHeightY = true;
            clips[i].lockRootPositionXZ = true;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static AnimationClip FindFirstClip(string modelPath)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", System.StringComparison.Ordinal));

        if (clip == null)
        {
            Debug.LogError($"No animation clip found in {modelPath}.");
        }

        return clip;
    }

    private static GameObject CreateOrReplaceHeroVisual(Transform player)
    {
        Transform oldVisual = player.Find("HeroVisual");
        if (oldVisual != null)
        {
            Object.DestroyImmediate(oldVisual.gameObject);
        }

        GameObject heroAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HeroModelPath);
        if (heroAsset == null)
        {
            Debug.LogError($"Hero model was not found at {HeroModelPath}.");
            return new GameObject("HeroVisual");
        }

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(heroAsset, player);
        visual.name = "HeroVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        return visual;
    }

}

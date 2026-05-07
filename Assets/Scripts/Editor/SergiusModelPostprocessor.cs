using UnityEditor;
using UnityEngine;

public sealed class SergiusModelPostprocessor : AssetPostprocessor
{
    private static readonly string[] SergiusModels =
    {
        "Assets/Character/T-Pose.fbx",
        "Assets/Character/Idle.fbx",
        "Assets/Character/Walking.fbx"
    };

    [InitializeOnLoadMethod]
    private static void ReimportSergiusModelsOnce()
    {
        const string sessionKey = "ProjectSword.SergiusModelsReimported.V2";
        if (SessionState.GetBool(sessionKey, false))
        {
            return;
        }

        SessionState.SetBool(sessionKey, true);
        EditorApplication.delayCall += () =>
        {
            for (int i = 0; i < SergiusModels.Length; i++)
            {
                AssetDatabase.ImportAsset(SergiusModels[i], ImportAssetOptions.ForceUpdate);
            }
        };
    }

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/Character/"))
        {
            return;
        }

        ModelImporter importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.animationWrapMode = assetPath.Contains("T-Pose")
            ? WrapMode.Default
            : WrapMode.Loop;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        bool shouldLoop = !assetPath.Contains("T-Pose");
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].loopTime = shouldLoop;
            clips[i].loopPose = shouldLoop;
        }

        importer.clipAnimations = clips;
    }
}

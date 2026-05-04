using UnityEditor;

public sealed class HeroAnimationPostprocessor : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        if (!assetPath.EndsWith("Assets/Character/Idle.fbx", System.StringComparison.Ordinal)
            && !assetPath.EndsWith("Assets/Character/Walking.fbx", System.StringComparison.Ordinal))
        {
            return;
        }

        ModelImporter importer = (ModelImporter)assetImporter;
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;

        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].loopTime = true;
            clips[i].loopPose = true;
            clips[i].lockRootRotation = true;
            clips[i].lockRootHeightY = true;
            clips[i].lockRootPositionXZ = true;
        }

        importer.clipAnimations = clips;
    }
}

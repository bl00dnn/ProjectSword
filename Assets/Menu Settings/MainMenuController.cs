using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private string levelSceneName = "Game";

    public void PlayGame()
    {
        if (string.IsNullOrWhiteSpace(levelSceneName))
        {
            Debug.LogWarning("Main menu play button has no target scene name.", this);
            return;
        }

        SceneTransitionManager.LoadScene(levelSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

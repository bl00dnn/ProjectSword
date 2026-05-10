using UnityEngine;
using UnityEngine.SceneManagement;

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

        SceneManager.LoadScene(levelSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

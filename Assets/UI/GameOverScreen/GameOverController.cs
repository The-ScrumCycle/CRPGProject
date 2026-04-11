using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private GameObject loadGameModalRoot;
    [SerializeField] private GameObject exitGameButton;

    public void LoadGame()
    {
        if (loadGameModalRoot == null)
        {
            Debug.LogWarning("loadGameModalRoot is not assigned in editor");
            return;
        }

        exitGameButton.SetActive(false);
        loadGameModalRoot.SetActive(true);
    }

    public void ExitGame()
    {
        Debug.Log("Quit game");

        Application.Quit();

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }

    void Start()
    {
        // if not in exploration scene , player and other persistant objects should not exist 
        if(SceneManager.GetActiveScene().name == "GameOverScene")
        {
            // CleanupSceneForGameOver();
        }
    }

  

}

using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    [SerializeField] private string newGameSceneName = "Exploration";
    [SerializeField] private SaveSlotPanelController saveSlotPanel;


// TODO: instantiate new game, need to decide overwriting existign save?
    public void NewGame()
    {
        SceneManager.LoadScene(newGameSceneName);
    }

// TODO: hook up to save/load system
    public void LoadGame()
    {
        saveSlotPanel.Open();
    }

    public void ExitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
}
}
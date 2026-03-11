using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField] private string newGameSceneName = "Exploration";

    [Header("UI References")]
    [SerializeField] private GameObject loadGameModalRoot;

    private bool isLoadModalOpen;

    private void Start()
    {
        if (loadGameModalRoot != null)
        {
            loadGameModalRoot.SetActive(false);
        }

        isLoadModalOpen = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isLoadModalOpen)
        {
            CloseLoadGameModal();
        }
    }

    // TODO: instantiate new game, need to decide overwriting existing save?
    public void NewGame()
    {
        SceneManager.LoadScene(newGameSceneName);
    }

    // opens the multi-slot load game menu
    public void LoadGame()
    {
        OpenLoadGameModal();
    }

    public void OpenLoadGameModal()
    {
        if (loadGameModalRoot == null)
        {
            Debug.LogWarning("StartMenuController: Load Game modal root is not assigned.");
            return;
        }

        loadGameModalRoot.SetActive(true);
        isLoadModalOpen = true;
    }

    public void CloseLoadGameModal()
    {
        if (loadGameModalRoot == null)
        {
            Debug.LogWarning("StartMenuController: Load Game modal root is not assigned.");
            return;
        }

        loadGameModalRoot.SetActive(false);
        isLoadModalOpen = false;
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
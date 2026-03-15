using UnityEngine;
using UnityEngine.InputSystem;
using Core.Save;

public class EscapeMenuController : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject escapeMenuRoot;        // main escape menu
    [SerializeField] private GameObject confirmExitModalRoot;  // confirmation popup

    [Header("State")]
    [SerializeField] private bool hasSaved = false; // flag for now (later tie to real save system)


    private bool isMenuOpen;
    private bool isConfirmOpen;
    private PlayerController playerController;


    void Start()
    {
        SetMenuOpen(false);
        SetConfirmOpen(false);

        playerController = PlayerController.Instance;
        if (playerController == null)
        {
            Debug.LogError("PlayerController not found");
        }
    }

    void Update()
    {
        if (playerController.GetInDialogue()) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
       

        // close confirm modal if open, otherwise toggle menu
        if (isConfirmOpen)
        {
            CloseConfirmExitModal();
            return;
        }

        if (isMenuOpen)
        {
            CloseMenu();
            return;
        }

        OpenMenu();
    }

    // escape menu open/close
    public void OpenMenu()
    {
        SetMenuOpen(true);
        SetConfirmOpen(false);
    }

    public void CloseMenu()
    {
        SetConfirmOpen(false);
        SetMenuOpen(false);
    }

    private void SetMenuOpen(bool open)
    {
        isMenuOpen = open;

        if (escapeMenuRoot != null)
            escapeMenuRoot.SetActive(open);

        //pause gameplay while escape menu is open
        Time.timeScale = open ? 0f : 1f;
    }

    //confirm modla open/close
    private void OpenConfirmExitModal()
    {
        //ensure escape menu is open when showing confirm modal
        if (!isMenuOpen) OpenMenu();
        SetConfirmOpen(true);
    }

    private void CloseConfirmExitModal()
    {
        SetConfirmOpen(false);
    }

    private void SetConfirmOpen(bool open)
    {
        isConfirmOpen = open;

        if (confirmExitModalRoot != null)
            confirmExitModalRoot.SetActive(open);
    }

   //button handlers (escape menu)
    public void SaveGame()
    {
        SaveManager.Instance.Save();
        Debug.Log("Save requested (placeholder). Setting hasSaved = true.");
        hasSaved = true;

        // optional: keep menu open so user can choose exit after saving
        // CloseMenu();
    }

    public void ExitGame()
    {
        if (hasSaved)
        {
            Debug.Log("Exit requested; hasSaved=true -> quitting.");
            QuitGame();
        }
        else
        {
            Debug.Log("Exit requested; hasSaved=false -> opening confirm modal.");
            OpenConfirmExitModal();
        }
    }

   // button handlers (confirmation modal)
    public void ConfirmSaveAndCloseGame()

    {
        SaveManager.Instance.Save();
        Debug.Log("Confirm: Save & Close Game (placeholder). Setting hasSaved = true then quitting.");
        hasSaved = true;
        QuitGame();
    }

    public void ConfirmExitWithoutSaving()
    {
        Debug.Log("Confirm: Exit without saving -> quitting.");
        QuitGame();
    }

    private void QuitGame()
    {
        // prevent editor from staying paused
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
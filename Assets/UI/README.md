UI System (Start Menu & Escape Menu)

This folder contains the UI implementation for the Start Menu and Escape Menu. The menus are fully wired for interaction and navigation.

NOTE: Remaining work mainly involves integrating the save/load system

Structure:

Assets
├── UI
│   ├── EscapeMenu
│   │   └── Scripts/EscapeMenuController.cs
│   ├── StartMenu
│   │   └── Scripts/StartMenuController.cs
│   └── README.md
│
└── Scenes
    └── StartMenu.unity

------------------------------------------------------
START MENU

Scene: Assets/Scenes/StartMenu.unity
Controller: StartMenuController.cs

BUTTONS:

New Game
Loads the exploration scene. Should instantiate a new instace of the game 
    - Need to decide whetehr to overwrite old save or not if user already has a game saved
Method: StartMenuController.NewGame()

Load Game
Placeholder for save system integration.
Method: StartMenuController.LoadGame()

Expected behavior:
	•	Check if a save file exists
	•	Load saved state
	•	Load the appropriate scene

Exit Game
Quits the game (stops Play Mode in the editor).
Method: StartMenuController.ExitGame()

------------------------------------------------------

ESCAPE MENU

Controller: EscapeMenuController.cs

Pressing Escape toggles the menu during gameplay.

When opened: Time.timescale = 0 (pause the game)
When closed: Time.timescale = 1 (resume game after menu close)

BUTTONS: 

Save Game
Placholder for now, hook it up to save system
Method: EscapeMenuController.SaveGame() 
    - Saving will set the hasSaved flag = true 

Exit Game
If the player has already saved, quit game 
If not (hasSaved flag = false), show confirmation modal
Method: EscapeMenuControlller.ExitGame()

CLose menu
Closes the escape menu and resumes gampeplay
Method: EscapeMenuController.CloseMenu()

NOTE: The menu can also be closed by presing Escape again

------------------------------------------------------

EXIT CONFIRMATION MODAL

Pops up when player attempts to exit without saving (hasSaved = false)

BUTTONS:

Save & Close Game
Placeholder for now, sets hasSaved = true and exits. Need to hook up to save system
Method: EscapeMenuController.ConfirmSaveAndCloseGame()

Close Game Without Save
Immediately exits the gave
Method: EscapeMenuController.ConfirmExitWithoutSaving()

------------------------------------------------------

ESCAPE KEY PRIORITY

1. If confirmation modal open -> close modal
2. Else if escape menu open -> close menu
3. Else -> open escape menu

------------------------------------------------------

SAVE SYSTEM INTEGRATION POINTS

Save system hooks need to be added in:
EscapeMenuController.SaveGame()
EscapeMenuController.ConfirmSaveAndCloseGame()

and 

StartMenuController.LoadGame()

NOTE: Current implementation uses a temp flag: bool hasSaved, should be later replaced with a real save-state check

------------------------------------------------------

FINAL NOTES:

- Application.Quit() does not exit PLay Mode in Unity Editor. Current scripts use UnityEditor.EditorApplication.isPlaying = false to simulate quitting during dev

- Confirmation modal is a child of the escape menu root and is disabled by default

BUGS: 

- If user exits captain dialogue by pressing escape, gameplay gets stuck after you close the escape menu (cannot click to move, cannot click on captain to see dialogue again)

Error log when clicking Captain again:

UIRunner: dialogue graph is not ready on DialogueRunner.
UnityEngine.Debug:LogError (object)
Dialogue.Core.UIRunner:BeginDialogue (string) (at Assets/Dialogue/Core/UIRunner.cs:118)
CaptainController:BeginCaptainDialogue () (at Assets/Exploration/Scripts/NPCS/CaptainController.cs:187)
CaptainController:OnMouseDown () (at Assets/Exploration/Scripts/NPCS/CaptainController.cs:138)
UnityEngine.SendMouseEvents:DoSendMouseEvents (int) (at /Users/bokken/build/output/unity/unity/Modules/InputLegacy/MouseEvents.cs:208)


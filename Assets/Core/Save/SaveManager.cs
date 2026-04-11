using Core.Save;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance {get; private set;}
    private List<ISaveable> saveables = new List<ISaveable>();

    private int slot_index;

    private int pendingLoadSlot;

    private MusicController musicController;



    void Awake()
    {
        Debug.Log($"[SaveManager] Awake. persistentDataPath: {Application.persistentDataPath}");

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        slot_index = GetSlotCount();
    }


    public void Register(ISaveable saveable)
    {
        saveables.Add(saveable);
        Debug.Log($"[SaveManager] Registered: {saveable}. Total: {saveables.Count}");
    }

    public void SetSlot(int index)
    {
        slot_index = index;
    }

    private string GetSavePath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, "Saves", $"slot_{slot}");
    }

    public string GetScreenshotPath(int slot)
    {
        return Path.Combine(GetSavePath(slot), "game_scene.png");
    }

    public int GetSlotCount()
    //files will be saves locally to the user, under Documents/slot_{index}/data.json and capture.png
    //idea is at load we find out how many slots there are so we don't accidentally overwrite old data.
    {
        int slot = 0;
        while (Directory.Exists(GetSavePath(slot)))
        {
            slot++;
        }
        return slot;
    }
    public void Save()
    {
        Debug.Log($"[SaveManager] Save() called. Saveables count: {saveables.Count}. Path: {GetSavePath(slot_index)}");
        SaveData saveData = new SaveData();
        foreach(ISaveable saveable in saveables)
        {
            saveable.SetSaveData(saveData);

        }
        Debug.Log($"[SaveManager] Save complete at slot {slot_index - 1}");
  
        //serialized object into JSON save data
        saveData.saveDateTime = System.DateTime.Now.ToString("dd MMM yyyy  HH:mm");
        //Application.OpenURL(Application.persistentDataPath);

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        byte[] screenshotBytes = screenshot.EncodeToPNG();
        Destroy(screenshot);

        saveData.screenshotData = screenshotBytes;
        StartCoroutine(SaveScreenshot(saveData));
    }

    private IEnumerator SaveScreenshot(SaveData saveData)
        {
            yield return new WaitForEndOfFrame();
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] screenshotBytes = screenshot.EncodeToPNG();
            Destroy(screenshot);
            saveData.screenshotData = screenshotBytes;
            string json = JsonUtility.ToJson(saveData);
            string savePath = GetSavePath(slot_index);
            Directory.CreateDirectory(savePath);
            File.WriteAllText(Path.Combine(savePath, "save.json"), json);
            File.WriteAllBytes(Path.Combine(savePath, "game_scene.png"), screenshotBytes);
            slot_index++;
            Debug.Log($"[SaveManager] Save complete at slot {slot_index - 1}");
        }

    // Returns save metadata for a slot, or null if the slot is empty
    public SaveData GetSlotData(int slot)
    {
        string path = Path.Combine(GetSavePath(slot), "save.json");
        if (!File.Exists(path)) return null;
        return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
    }

    public void Load(int slot)
    {
        string savePath = GetSavePath(slot);
        if (!File.Exists(Path.Combine(savePath, "save.json")))
        {
            Debug.LogWarning("No save file found at slot " + slot);
            return;
        }       
        string json = File.ReadAllText(Path.Combine(savePath, "save.json"));
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        foreach (ISaveable saveable in saveables)
        {
            saveable.LoadSaveData(saveData);
        }
    }

    //these functions ensure whenever a new game is loaded, ISaveable objects are able to subscribe to SaveManager
    //before being loaded
    public void RequestLoad(int slot)
    {
        if (SceneManager.GetActiveScene().name == "Exploration")
        {
            CleanUpController.Instance.CleanUp();
        }

        saveables = new List<ISaveable>();
        pendingLoadSlot = slot;
        SceneManager.sceneLoaded += OnSceneLoaded;  // subscribe BEFORE loading
        SceneManager.LoadScene("Exploration"); 

    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;  // unsubscribe immediately
        StartCoroutine(LoadAfterFrame());           // wait one frame for Start() calls
    }

    private IEnumerator LoadAfterFrame()
    {
        yield return null;                          // one frame passes, all Start()s run
        Load(pendingLoadSlot);                      // saveables list is now populated
        pendingLoadSlot = -1;

        CameraController cam = FindObjectOfType<CameraController>();
        if (cam != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) cam.SetTarget(player.transform);
        }
    }

   
}

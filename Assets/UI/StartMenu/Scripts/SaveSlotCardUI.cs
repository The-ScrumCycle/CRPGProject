using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using Core.Save;

public class SaveSlotCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI slotLabel;
    [SerializeField] private TextMeshProUGUI dateLabel;
    [SerializeField] private RawImage screenshotImage;
    [SerializeField] private Button selectButton;

    private int slotIndex;
    private SaveSlotPanelController panel;

    public void Init(int slot, SaveSlotPanelController panelController)
    {
        slotIndex = slot;
        panel = panelController;

        slotLabel.text = $"Slot {slot + 1}";
        selectButton.onClick.AddListener(() => panel.OnSlotSelected(slotIndex));

        LoadSlotPreview(slot);
    }

    private void LoadSlotPreview(int slot)
    {
        string savePath = Path.Combine(Application.persistentDataPath, "Saves", $"slot_{slot}", "save.json");

        if (!File.Exists(savePath))
        {
            dateLabel.text = "No Data";
            return;
        }

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        dateLabel.text = data.saveDateTime;

        if (data.screenshotData != null && data.screenshotData.Length > 0)
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(data.screenshotData);
            screenshotImage.texture = tex;
        }
    }
}

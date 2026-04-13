using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotPanelController : MonoBehaviour
{
    [SerializeField] private Transform slotCardContainer;
    [SerializeField] private Button backButton;

    private readonly List<Texture2D> _slotTextures = new List<Texture2D>();

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(Close);
    }

    // Called automatically by Unity when LoadGameModalRoot is SetActive(true)
    void OnEnable()
    {
        PopulateSlots();
    }

    // Called automatically by Unity when LoadGameModalRoot is SetActive(false)
    void OnDisable()
    {
        ClearSlots();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void PopulateSlots()
    {
        if (SaveManager.Instance == null) return;

        int slotIndex = 0;
        foreach (Transform child in slotCardContainer)
        {
            var btn = child.GetComponent<Button>();
            if (btn == null) { slotIndex++; continue; }

            var labels     = child.GetComponentsInChildren<TextMeshProUGUI>();
            var titleLabel = labels.Length > 0 ? labels[0] : null;
            var metaLabel  = labels.Length > 1 ? labels[1] : null;

            btn.onClick.RemoveAllListeners();

            var data = SaveManager.Instance.GetSlotData(slotIndex);
            if (data != null)
            {
                if (titleLabel != null) titleLabel.text = $"Slot {slotIndex + 1}";
                if (metaLabel  != null) metaLabel.text  = data.saveDateTime;
                LoadScreenshot(child, slotIndex);
                int captured = slotIndex;
                btn.onClick.AddListener(() => OnSlotSelected(captured));
                btn.interactable = true;
            }
            else
            {
                if (titleLabel != null) titleLabel.text = $"Slot {slotIndex + 1}";
                if (metaLabel  != null) metaLabel.text  = "Empty";
                ClearScreenshot(child);
                btn.interactable = false;
            }

            slotIndex++;
        }
    }

    private void LoadScreenshot(Transform slotRoot, int slotIndex)
    {
        var img = FindScreenshotImage(slotRoot);
        if (img == null) return;

        string path = SaveManager.Instance.GetScreenshotPath(slotIndex);
        if (!File.Exists(path)) { img.enabled = false; return; }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        _slotTextures.Add(tex);

        img.sprite  = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        img.enabled = true;
    }

    private void ClearScreenshot(Transform slotRoot)
    {
        var img = FindScreenshotImage(slotRoot);
        if (img != null) img.enabled = false;
    }

    private Image FindScreenshotImage(Transform slotRoot)
    {
        Transform t = FindDescendantByName(slotRoot, "ScreenshotImage");
        return t != null ? t.GetComponent<Image>() : null;
    }

    private Transform FindDescendantByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindDescendantByName(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void ClearSlots()
    {
        foreach (Transform child in slotCardContainer)
        {
            var btn = child.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();
        }

        foreach (var tex in _slotTextures) Destroy(tex);
        _slotTextures.Clear();
    }

    public void OnSlotSelected(int slot)
    {
        SaveManager.Instance.OnLoadComplete += OnLoadComplete; //once we finish loading in the background --> we will invoke OnLoadComplete
        SaveManager.Instance.RequestLoad(slot);
    }

    private void OnLoadComplete()
    {
        SaveManager.Instance.OnLoadComplete -= OnLoadComplete;
        if (this != null) Close();
    }
}

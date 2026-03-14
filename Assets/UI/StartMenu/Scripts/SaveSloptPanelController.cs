using UnityEngine;
using UnityEngine.UI;

public class SaveSlotPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform slotCardContainer;
    [SerializeField] private GameObject slotCardPrefab;
    [SerializeField] private Button backButton;

    void Start()
    {
        panelRoot.SetActive(false);
        backButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        panelRoot.SetActive(true);
        PopulateSlots();
    }

    public void Close()
    {
        panelRoot.SetActive(false);
        ClearSlots();
    }

    private void PopulateSlots()
    {
        ClearSlots();

        int slotCount = SaveManager.Instance.GetSlotCount();

        if (slotCount == 0)
        {
            Debug.Log("No save slots found.");
            return;
        }

        for (int i = 0; i < slotCount; i++)
        {
            GameObject card = Instantiate(slotCardPrefab, slotCardContainer);
            SaveSlotCardUI cardUI = card.GetComponent<SaveSlotCardUI>();
            cardUI.Init(i, this);
        }
    }

    private void ClearSlots()
    {
        foreach (Transform child in slotCardContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void OnSlotSelected(int slot)
    {
        SaveManager.Instance.RequestLoad(slot);
    }
}

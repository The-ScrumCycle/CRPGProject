using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Combat.UI
{
    /// <summary>
    /// Represents a clickable action choice (Move, attack) in the UI
    /// also helps to make clear to user which action is currently selected
    /// </summary>
    public class ActionCardUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI hotkeyText;
        [SerializeField] private Image highlightImage;
        [SerializeField] private Button cardButton;

        private PlayerActionMode _mode;

        void Awake()
        {
            if (cardButton != null)
            {
                cardButton.onClick.AddListener(OnCardClicked);
            }
        }

        public void Setup(string title, string hotkey, PlayerActionMode mode)
        {
            if (titleText != null) titleText.text = title;
            if (hotkeyText != null) hotkeyText.text = hotkey;
            _mode = mode;
        }

        public void SetSelected(bool isSelected)
        {
            if (highlightImage != null)
            {
                highlightImage.enabled = isSelected;
            }
        }

        public void OnCardClicked()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.SetActionMode(_mode);
            }
        }
    }
}

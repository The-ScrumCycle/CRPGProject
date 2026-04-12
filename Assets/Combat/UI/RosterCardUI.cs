using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Game.Combat.Units;

namespace Game.Combat.UI
{
    /// <summary>
    /// Represents a single player unit's current status (health, turn, etc)
    /// visually to the user.
    /// </summary>
    public class RosterCardUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image backgroundImage;

        [Header("Colors")]
        [SerializeField] private Color defaultBgColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color activeBgColor = new Color(0.1f, 0.4f, 0.1f, 1f); // Green
        [SerializeField] private Color activeStatusColor = Color.yellow;

        private Unit _trackedUnit;

        public void Setup(Unit unit)
        {
            _trackedUnit = unit;
            if (nameText != null) nameText.text = unit.DisplayName;
            UpdateHealthText();
        }

        public void Refresh(bool isCurrentTurn, bool hasActed)
        {
            if (_trackedUnit == null) return;

            UpdateHealthText();

            if (isCurrentTurn && !hasActed)
            {
                if (backgroundImage != null) backgroundImage.color = activeBgColor;
                if (statusText != null)
                {
                    statusText.text = "READY";
                    statusText.color = activeStatusColor;
                    statusText.gameObject.SetActive(true);
                }
            }
            else
            {
                if (backgroundImage != null) backgroundImage.color = defaultBgColor;
                if (statusText != null) statusText.gameObject.SetActive(false);
            }
        }

        // Allow player to cycle roster by clicking on card
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_trackedUnit != null && _trackedUnit.IsPlayerControlled && _trackedUnit.IsAlive)
            {
                // Call the manager to swap to this unit
                CombatManager.Instance.TrySelectPlayerUnit(_trackedUnit);
            }
        }

        private void UpdateHealthText()
        {
            if (healthText != null)
            {
                healthText.text = $"HP: {_trackedUnit.Stats.currentHealth}/{_trackedUnit.Stats.maxHealth}";
            }
        }

        public Unit GetTrackedUnit() => _trackedUnit;
    }
}

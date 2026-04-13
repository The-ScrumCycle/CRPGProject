using UnityEngine;
using Game.Combat.Units;
using Game.Combat.Actions;

namespace Game.Combat.UI
{
    /// <summary>
    /// Manages the Action Bar container and its action cards
    /// </summary>
    public class ActionBarUI : MonoBehaviour
    {
        [SerializeField] private ActionCardUI moveCard;
        [SerializeField] private ActionCardUI attackCard;
        [SerializeField] private ActionCardUI secondaryCard;
        [SerializeField] private GameObject actionBarContainer;

        // Dynamically refresh our Action bar depending on currently selected unit and the unit's loadout
        public void RefreshActionBar(PlayerActionMode currentMode, bool isPlayerTurn, bool hasActed, bool hasMoved, bool isPinned, Unit currentUnit)
        {
            bool shouldShow = isPlayerTurn && !hasActed;
            if (actionBarContainer != null) actionBarContainer.SetActive(shouldShow);
            if (!shouldShow || currentUnit == null) return;

            // 1. Move Card is always standard and guaranteed
            if (moveCard != null) 
            {
                // Disable the move card if unit already moved this turn
                moveCard.gameObject.SetActive(!hasMoved);

                // Display either "Move" or "Dodge" for action card depending on pinned status
                if (!hasMoved)
                {
                    string moveText = isPinned ? "Dodge" : "Move";
                    moveCard.Setup(moveText, "[1]", PlayerActionMode.Move);
                    moveCard.SetSelected(currentMode == PlayerActionMode.Move);
                }
            }

            // 2. Dynamically assign the 2nd and 3rd cards based on loadout
            var abilities = currentUnit.AvailableActions;
            
            if (attackCard != null)
            {
                attackCard.gameObject.SetActive(abilities.Count > 0);
                if (abilities.Count > 0)
                {
                    SetupCard(attackCard, abilities[0], "[2]", PlayerActionMode.Attack, currentMode == PlayerActionMode.Attack);
                }
            }

            if (secondaryCard != null)
            {
                secondaryCard.gameObject.SetActive(abilities.Count > 1);
                if (abilities.Count > 1)
                {
                    SetupCard(secondaryCard, abilities[1], "[3]", PlayerActionMode.SecondaryAction, currentMode == PlayerActionMode.SecondaryAction);
                }
            }
        }

        private void SetupCard(ActionCardUI card, CombatActionType actionType, string hotkey, PlayerActionMode mode, bool isSelected)
        {
            // Map the internal Enum to a player-facing localized string
            string actionName = actionType switch
            {
                CombatActionType.HeavyMeleeAttack => "Heavy Strike",
                CombatActionType.PullAlly => "Rescue Pull", // <-- Changed from Pull to PullAlly
                CombatActionType.RangedHeal => "Heal",
                CombatActionType.RangedAttack => "Shoot",
                CombatActionType.MeleeAttack => "Attack",
                _ => actionType.ToString()
            }; 

            card.Setup(actionName, hotkey, mode);
            card.SetSelected(isSelected);
        }
    }
}
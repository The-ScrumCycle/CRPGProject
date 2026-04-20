using UnityEngine;
using Game.Combat.Units;

namespace Game.Combat.UI
{
    /// <summary>
    /// The root manager for the Combat UI
    /// Bridges the gap between the CombatManager (Controller) and the UI components (View)
    /// </summary>
    public class CombatUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UnitRosterUI unitRosterUI;
        [SerializeField] private ActionBarUI actionBarUI;

        [Header("Unit Controls")]
        [SerializeField] private GameObject endUnitTurnButton;

        void Update()
        {
            if (CombatManager.Instance == null) return;

            var allUnits = CombatManager.Instance.GetAllUnits();
            var currentUnit = CombatManager.Instance.GetCurrentUnit();
            bool isPlayerTurn = CombatManager.Instance.IsPlayerTurn;
            var currentMode = CombatManager.Instance.CurrentActionMode;

            // Poll state from the CombatManager
            var playerUnits = new System.Collections.Generic.List<Unit>();
            foreach (var u in allUnits)
            {
                if (u.IsPlayerControlled) playerUnits.Add(u);
            }

            // Dynamically show/hide the unit end turn button
            if (endUnitTurnButton != null)
            {
                bool canUnitWait = isPlayerTurn && currentUnit != null && currentUnit.IsPlayerControlled && !CombatManager.Instance.HasUnitActed(currentUnit);
                endUnitTurnButton.SetActive(canUnitWait);

                if (canUnitWait && unitRosterUI != null)
                {
                    RectTransform lastCard = unitRosterUI.GetLastActiveCard();
                    if (lastCard != null)
                    {
                        RectTransform btnRect = endUnitTurnButton.GetComponent<RectTransform>();
                        
                        // Get the absolute screen position of the last active card
                        // corners[0] is the bottom-left corner, corners[1] is top-left, etc.
                        Vector3[] cardCorners = new Vector3[4];
                        lastCard.GetWorldCorners(cardCorners);
                        
                        // Calculate offset based on the button's pivot so its top edge sits cleanly below the card
                        float pivotOffset = btnRect.pivot.y * btnRect.rect.height * btnRect.lossyScale.y;
                        float padding = 15f; // The pixel gap between the card and the button
                        
                        btnRect.position = new Vector3(btnRect.position.x, cardCorners[0].y - pivotOffset - padding, btnRect.position.z);
                    }
                }
            } 
            
            // Push state down to view layer
            if (unitRosterUI != null)
            {
                unitRosterUI.RefreshRoster(playerUnits, currentUnit);
            }

            if (actionBarUI != null)
            {
                // Determine if unit is pinned to see if they have dodge or move action available
                bool activeUnitHasActed = currentUnit != null && CombatManager.Instance.HasUnitActed(currentUnit);
                bool activeUnitHasMoved = currentUnit != null && CombatManager.Instance.HasUnitMoved(currentUnit);
                bool isPinned = CombatManager.Instance.IsUnitPinned(currentUnit);
                
                actionBarUI.RefreshActionBar(currentMode, isPlayerTurn, activeUnitHasActed, activeUnitHasMoved, isPinned, currentUnit); 
            } 
        }

        // If player skips their turn in the UI end that unit's turn
        public void OnSkipUnitTurnClicked()
        {
            if (CombatManager.Instance != null && CombatManager.Instance.IsPlayerTurn)
            {
                CombatManager.Instance.SkipCurrentUnitTurn();
            }
        }
    }
}

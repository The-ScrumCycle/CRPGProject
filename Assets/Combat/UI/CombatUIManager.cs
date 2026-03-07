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

        void Update()
        {
            if (CombatManager.Instance == null) return;

            // Poll state from the CombatManager
            var playerUnits = new System.Collections.Generic.List<Unit>();
                foreach(var u in CombatManager.Instance.GetAllUnits()) {
                if (u.IsPlayerControlled) playerUnits.Add(u);
            }
            var currentUnit = CombatManager.Instance.GetCurrentUnit();
            bool hasActed = CombatManager.Instance.HasPlayerActed;
            bool isPlayerTurn = CombatManager.Instance.IsPlayerTurn;
            var currentMode = CombatManager.Instance.CurrentActionMode;

            // Push state down to view layer
            if (unitRosterUI != null)
            {
                unitRosterUI.RefreshRoster(playerUnits, currentUnit, hasActed);
            }

            if (actionBarUI != null)
            {
                actionBarUI.RefreshActionBar(currentMode, isPlayerTurn, hasActed);
            }
        }
    }
}

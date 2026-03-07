using UnityEngine;

namespace Game.Combat.UI
{
    /// <summary>
    /// Manages the Action Bar container and its action cards
    /// </summary>
    public class ActionBarUI : MonoBehaviour
    {
        [SerializeField] private ActionCardUI moveCard;
        [SerializeField] private ActionCardUI attackCard;
        [SerializeField] private GameObject actionBarContainer;

        void Start()
        {
            // Very simplified, later this might be data-driven per unit.
            if (moveCard != null) moveCard.Setup("Move", "[1]", PlayerActionMode.Move);
            if (attackCard != null) attackCard.Setup("Attack", "[2]", PlayerActionMode.Attack);
        }

        public void RefreshActionBar(PlayerActionMode currentMode, bool isPlayerTurn, bool hasActed)
        {
            bool shouldShow = isPlayerTurn && !hasActed;

            if (actionBarContainer != null)
            {
                actionBarContainer.SetActive(shouldShow);
            }

            if (!shouldShow) return;

            if (moveCard != null) moveCard.SetSelected(currentMode == PlayerActionMode.Move);
            if (attackCard != null) attackCard.SetSelected(currentMode == PlayerActionMode.Attack);
        }
    }
}

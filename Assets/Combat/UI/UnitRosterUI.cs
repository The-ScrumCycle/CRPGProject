using System.Collections.Generic;
using UnityEngine;
using Game.Combat.Units;

namespace Game.Combat.UI
{
    /// <summary>
    /// Manages the collection of RosterCardUI elements
    /// </summary>
    public class UnitRosterUI : MonoBehaviour
    {
        [SerializeField] private RosterCardUI rosterCardPrefab;
        [SerializeField] private Transform rosterContainer;
        
        [Header("Dynamic Layout")]
        [SerializeField] private Transform endTurnButton;

        private List<RosterCardUI> _cards = new List<RosterCardUI>();

        public void RefreshRoster(IReadOnlyList<Unit> playerUnits, Unit currentActiveUnit)
        {
            // 1. Filter out dead units so the roster dynamically shrinks during combat
            List<Unit> aliveUnits = new List<Unit>();
            foreach(var u in playerUnits)
            {
                if (u != null && u.IsAlive) aliveUnits.Add(u);
            }

            // 2. Spawn cards if we need more
            while (_cards.Count < aliveUnits.Count)
            {
                var newCard = Instantiate(rosterCardPrefab, rosterContainer);
                _cards.Add(newCard);
            }

            // 3. Update the UI
            for (int i = 0; i < _cards.Count; i++)
            {
                if (i < aliveUnits.Count)
                {
                    var unit = aliveUnits[i];
                    _cards[i].gameObject.SetActive(true);

                    if (_cards[i].GetTrackedUnit() != unit) _cards[i].Setup(unit);

                    bool isCurrentTurn = (unit == currentActiveUnit);
                    bool hasActed = CombatManager.Instance.HasUnitActed(unit);
                    
                    _cards[i].Refresh(isCurrentTurn, hasActed);
                }
                else
                {
                    // Hide cards we don't need right now
                    _cards[i].gameObject.SetActive(false);
                }
            }

            // 4. Magnetize the button to the bottom of the active cards
            if (endTurnButton != null && endTurnButton.parent == rosterContainer)
            {
                endTurnButton.SetAsLastSibling();
            }
        } 

        // Finds the last card in the list that is currently visible
        public RectTransform GetLastActiveCard()
        {
            for (int i = _cards.Count - 1; i >= 0; i--)
            {
                if (_cards[i] != null && _cards[i].gameObject.activeSelf)
                {
                    return _cards[i].GetComponent<RectTransform>();
                }
            }
            return null;
        }
    }
} 
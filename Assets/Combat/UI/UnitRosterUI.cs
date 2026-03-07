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

        private List<RosterCardUI> _cards = new List<RosterCardUI>();

        public void RefreshRoster(IReadOnlyList<Unit> allUnits, Unit currentActiveUnit, bool playerHasActed)
        {
            // Ensure we have enough cards instantiated
            while (_cards.Count < allUnits.Count)
            {
                var newCard = Instantiate(rosterCardPrefab, rosterContainer);
                _cards.Add(newCard);
            }

            // Update visible cards and hide excess ones
            for (int i = 0; i < _cards.Count; i++)
            {
                if (i < allUnits.Count)
                {
                    var unit = allUnits[i];
                    _cards[i].gameObject.SetActive(true);

                    if (_cards[i].GetTrackedUnit() != unit)
                    {
                        _cards[i].Setup(unit);
                    }

                    bool isCurrentTurn = (unit == currentActiveUnit);
                    _cards[i].Refresh(isCurrentTurn, playerHasActed);
                }
                else
                {
                    _cards[i].gameObject.SetActive(false);
                }
            }
        }
    }
}

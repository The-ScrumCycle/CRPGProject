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

        public void RefreshRoster(IReadOnlyList<Unit> playerUnits, Unit currentActiveUnit)
        {
            while (_cards.Count < playerUnits.Count)
            {
                var newCard = Instantiate(rosterCardPrefab, rosterContainer);
                _cards.Add(newCard);
            }

            for (int i = 0; i < _cards.Count; i++)
            {
                if (i < playerUnits.Count)
                {
                    var unit = playerUnits[i];
                    _cards[i].gameObject.SetActive(true);

                    if (_cards[i].GetTrackedUnit() != unit) _cards[i].Setup(unit);

                    bool isCurrentTurn = (unit == currentActiveUnit);
                    bool hasActed = CombatManager.Instance.HasUnitActed(unit);
                    
                    _cards[i].Refresh(isCurrentTurn, hasActed);
                }
                else
                {
                    _cards[i].gameObject.SetActive(false);
                }
            }
        } 
    }
}

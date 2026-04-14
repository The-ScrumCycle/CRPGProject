using Game.Core.Party;
using Game.Combat.Grid;
using System.Collections.Generic;
using UnityEngine;
using Game.Combat.Actions;

namespace Game.Combat.Units
{
    /// <summary>
    /// Represents a single combat participant fully.
    /// A combat participant or unit has a role, stats (health, damage, movement range), and if enemy an associated AI.
    /// </summary>
    public class Unit
    {
        public string Id { get; }
        public string DisplayName { get; }
        public UnitRole Role { get; }
        public UnitStats Stats { get; private set; }
        public HexCell CurrentCell { get; set; }
        public AIBehavior AIBehavior { get; }
        public UnitVisual Visual { get; set; }

        public Unit grappler { get; set; }
        public bool IsAlive => Stats.currentHealth > 0;
        public bool IsPlayerControlled => Role == UnitRole.Player;
        public HexCoordinates Coordinates => CurrentCell != null ? CurrentCell.Coordinates : HexCoordinates.Invalid;
        public bool IsGrappled => grappler != null; 
        public List<CombatActionType> AvailableActions { get; } // actions a unit has avail

        public Unit(string id, string displayName, UnitRole role, UnitStats stats, AIBehavior aiBehavior = AIBehavior.Aggressive, List<CombatActionType> availableActions = null)
        {
            Id = id;
            DisplayName = displayName;
            Role = role;
            Stats = stats;
            AIBehavior = aiBehavior;
            // Assign a unit's valid actions or fallback to empty of none provided
            AvailableActions = availableActions ?? new List<CombatActionType>();
        }

        public Unit(string id, string displayName, UnitRole role, UnitStats stats, UnitVisual visual, AIBehavior aiBehavior = AIBehavior.Aggressive, List<CombatActionType> availableActions = null)
        {
            Id = id;
            DisplayName = displayName;
            Role = role;
            Stats = stats;
            Visual = visual;
            AIBehavior = aiBehavior;
            // Assign a unit's valid actions or fallback to empty of none provided
            AvailableActions = availableActions ?? new List<CombatActionType>();
        }

        // Update the unit's position. Called by HexGrid.
        internal void SetPosition(HexCoordinates coords, HexCell cell)
        {
            CurrentCell = cell;
        }

        // Apply damage to this unit.
        public void TakeDamage(int damage)
        {
            Stats.TakeDamage(damage);
            if (Visual != null) Visual.Flash();
            Debug.Log($"{DisplayName} took {damage} damage. Remaining HP: {Stats.currentHealth}/{Stats.maxHealth}");
        }

        public void Heal(int amount)
        {
            Stats.Heal(amount);
            if (Visual != null) Visual.HealEffect();
        }

        public override string ToString()
        {
            return $"{DisplayName} [{Role}] HP:{Stats.currentHealth}/{Stats.maxHealth} @ {Coordinates}";
        }
    }
}

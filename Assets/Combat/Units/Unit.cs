using Game.Core.Party;
using Game.Combat.Grid;
using System.Collections.Generic;
using UnityEngine;

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
        public AIBehavior? AIBehavior { get; }
        public UnitStats Stats { get; }

        public HexCoordinates Coordinates { get; private set; }
        public HexCell CurrentCell { get; private set; }

        public bool IsAlive => Stats.IsAlive;
        public bool IsPlayerControlled => Role == UnitRole.Player || Role == UnitRole.Companion;

        public Unit(string id, string displayName, UnitRole role, UnitStats stats, AIBehavior? aiBehavior = null)
        {
            Id = id;
            DisplayName = displayName;
            Role = role;
            Stats = stats;
            AIBehavior = aiBehavior;
        }

        // Update the unit's position. Called by HexGrid.
        internal void SetPosition(HexCoordinates coords, HexCell cell)
        {
            Coordinates = coords;
            CurrentCell = cell;
        }

        // Apply damage to this unit.
        public void TakeDamage(int damage)
        {
            Stats.TakeDamage(damage);
            Debug.Log($"{DisplayName} took {damage} damage. Remaining HP: {Stats.currentHealth}/{Stats.maxHealth}");
        }

        public override string ToString()
        {
            return $"{DisplayName} [{Role}] HP:{Stats.currentHealth}/{Stats.maxHealth} @ {Coordinates}";
        }
    }
}

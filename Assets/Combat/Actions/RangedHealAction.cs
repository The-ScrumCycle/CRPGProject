using Game.Combat.Units;
using Game.Combat.Grid;
using System.Collections.Generic;

namespace Game.Combat.Actions
{
    /// <summary>
    /// Action for healing a unit at range
    /// If valid, heals exactly by healAmount
    /// </summary>
    public class RangedHealAction : ICombatAction
    {
        public Unit Actor { get; }
        public Unit Target { get; }
        public int healAmount => Actor.Stats.healPower;

        public RangedHealAction(Unit actor, Unit target)
        {
            Actor = actor;
            Target = target;
        }

        public IEnumerable<HexCoordinates> GetTargetCells()
        {
            if (Target != null)
            {
                yield return Target.Coordinates;
            }
        }

        public bool IsValid(HexGrid grid)
        {
            // Can't attack self
            if (Actor == Target)
            {
                return false;
            }

            // Target must be alive
            if (Target == null || !Target.IsAlive)
            {
                return false;
            }

            // Must be adjacent (distance = 1)
            int distance = grid.GetDistance(Actor.Coordinates, Target.Coordinates);
            return distance >= 1 && distance <= Actor.Stats.attackRange;
        }

	    // Execute attack
        public void Execute(HexGrid grid)
        {
            Target.Heal(healAmount);
        }
    }
}

using System.Collections.Generic;
using Game.Combat.Units;
using Game.Combat.Grid;

namespace Game.Combat.Actions
{
    /// <summary>
    /// Action for melee attacking an adjacent unit.
    /// If valid, attack hits and deals exact damage.
    /// </summary>
    public class MeleeAttackAction : ICombatAction
    {
        public Unit Actor { get; }
        public Unit Target { get; }
        public int Damage => Actor.Stats.attackPower;

        public MeleeAttackAction(Unit actor, Unit target)
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
            return distance == 1;
        }

	// Execute attack
        public void Execute(HexGrid grid)
        {
            Target.TakeDamage(Damage);
        }
    }
}

using System.Collections.Generic;
using Game.Combat.Units;
using Game.Combat.Grid;

namespace Game.Combat.Actions
{
    /// <summary>
    /// Action for grappling an adjacent unit
    /// If valid, grappled unit is unable to move
    /// </summary>
    public class GrappleAction : ICombatAction
    {
        public Unit Actor { get; }
        public Unit Target { get; }

        public GrappleAction(Unit actor, Unit target)
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

            // Check distance is within attack range but not adjacent (ranged = distance > 1)
            int distance = grid.GetDistance(Actor.Coordinates, Target.Coordinates);
            return distance == 1;
        }

	    // Execute attack
        public void Execute(HexGrid grid, UnitVisual visual)
        {
            Target.grappler = Actor;
        }
    }
}
using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.Grid;
using System.Collections.Generic;

namespace Game.Combat.AI
{
    public class HydraGrapplerBrain : IEnemyBrain
    {
        public IEnumerable<ICombatAction> GenerateCandidateActions(Unit enemyUnit, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver)
        {
            if (BrainHelpers.ShouldRetreatToHealer(enemyUnit, allUnits, out Unit healer))
            {
                var retreat = BrainHelpers.MoveToward(enemyUnit, healer, grid, resolver);
                if (retreat != null)
                {
                    yield return retreat;
                }
            }

            foreach (var unit in allUnits)
            {
                if (!unit.IsPlayerControlled || !unit.IsAlive || unit.IsGrappled)
                {
                    continue;
                }

                int distance = grid.GetDistance(enemyUnit.Coordinates, unit.Coordinates);
                if (distance == 1)
                {
                    var grapple = resolver.CreateGrappleAction(enemyUnit, grid.GetCell(unit.Coordinates));
                    if (resolver.Validate(grapple)) yield return grapple;
                }

                var chaseMove = BrainHelpers.MoveToward(enemyUnit, unit, grid, resolver);
                if (chaseMove != null)
                {
                    yield return chaseMove;
                }
            }
        }
    }
}
using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.Grid;
using System.Collections.Generic;

namespace Game.Combat.AI
{
    public class MalakorBrain : IEnemyBrain
    {
        public IEnumerable<ICombatAction> GenerateCandidateActions(Unit enemyUnit, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver)
        {
            // 1. Find the absolute nearest player to hunt
            Unit target = BrainHelpers.FindNearestPlayer(enemyUnit, allUnits, grid);
            if (target == null) yield break;

            int dist = grid.GetDistance(enemyUnit.Coordinates, target.Coordinates);

            // 2. If adjacent attack
            if (dist == 1)
            {
                var attack = resolver.CreateSweepAttack(enemyUnit, grid.GetCell(target.Coordinates));
                if (resolver.Validate(attack))
                {
                    yield return attack;
                }
            }

            // 3. Otherwise chase them down
            var chase = BrainHelpers.MoveToward(enemyUnit, target, grid, resolver);
            if (chase != null)
            {
                yield return chase;
            }
        }
    }
}
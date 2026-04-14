using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.Grid;
using System.Collections.Generic;

namespace Game.Combat.AI
{
    public class SkeletonMeleeBrain : IEnemyBrain
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
                if (!unit.IsAlive || !unit.IsPlayerControlled) continue;
                if (grid.GetDistance(enemyUnit.Coordinates, unit.Coordinates) == 1)
                {
                    if (enemyUnit.AvailableActions.Contains(CombatActionType.SweepAttack))
                    {
                        var sweep = resolver.CreateSweepAttack(enemyUnit, grid.GetCell(unit.Coordinates));
                        if (resolver.Validate(sweep))
                        {
                            yield return sweep;
                        }
                    }

                    yield return resolver.CreateMeleeAttack(enemyUnit, grid.GetCell(unit.Coordinates));
                }
            }

            Unit huntTarget = FindLowestHpPlayer(allUnits);
            if (huntTarget != null)
            {
                var huntMove = BrainHelpers.MoveToward(enemyUnit, huntTarget, grid, resolver);
                if (huntMove != null)
                {
                    yield return huntMove;
                }
            }

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || !unit.IsPlayerControlled || unit == huntTarget)
                {
                    continue;
                }

                var chaseMove = BrainHelpers.MoveToward(enemyUnit, unit, grid, resolver);
                if (chaseMove != null)
                {
                    yield return chaseMove;
                }
            }
        }

        private Unit FindLowestHpPlayer(IReadOnlyList<Unit> allUnits)
        {
            Unit lowest = null;
            int lowestHp = int.MaxValue;

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || !unit.IsPlayerControlled) continue;
                if (unit.Stats.currentHealth < lowestHp)
                {
                    lowestHp = unit.Stats.currentHealth;
                    lowest = unit;
                }
            }
            return lowest;
        }
    }
}
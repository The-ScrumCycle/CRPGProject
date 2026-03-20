using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.Grid;
using System.Collections.Generic;

namespace Game.Combat.AI
{
    public class HydraGrapplerBrain : IEnemyBrain
    {
        public ICombatAction DecideAction(Unit enemyUnit, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver)
        {
            Unit bestTarget = null;
            int closestDist = int.MaxValue;

            // Find closest player
            foreach (var unit in allUnits)
            {
                if (unit.IsPlayerControlled && unit.IsAlive && !unit.IsGrappled) // Don't grapple already grappled units
                {
                    int dist = grid.GetDistance(enemyUnit.Coordinates, unit.Coordinates);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestTarget = unit;
                    }
                }
            }

            if (bestTarget != null)
            {
                if (closestDist == 1) 
                {
                    // In range use the Grapple Action
                    return new GrappleAction(enemyUnit, bestTarget);
                }
                else
                {
                    // Move toward target — pick cell that minimizes distance
                    var validMoves = resolver.GetValidMoveDestinations(enemyUnit);
                    if (validMoves.Count > 0)
                    {
                        HexCoordinates bestCell = validMoves[0];
                        int bestMoveDist = grid.GetDistance(validMoves[0], bestTarget.Coordinates);

                        for (int i = 1; i < validMoves.Count; i++)
                        {
                            int dist = grid.GetDistance(validMoves[i], bestTarget.Coordinates);
                            if (dist < bestMoveDist)
                            {
                                bestMoveDist = dist;
                                bestCell = validMoves[i];
                            }
                        }

                        if (bestMoveDist < closestDist)
                            return resolver.CreateMoveAction(enemyUnit, bestCell);
                    }
                }
            }
            return null;
        }
    }
}

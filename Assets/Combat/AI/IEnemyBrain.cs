using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.Grid;
using System.Collections.Generic;

namespace Game.Combat.AI
{
    /// <summary>
    /// Strategy interface for all enemy decision making
    /// Brains are stateless and emit candidate actions for the utility planner to score.
    /// </summary>
    public interface IEnemyBrain
    {
        IEnumerable<ICombatAction> GenerateCandidateActions(Unit enemyUnit, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver actionResolver);
    }
}

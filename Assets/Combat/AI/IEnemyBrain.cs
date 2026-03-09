using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.Grid;
using System.Collections.Generic;

namespace Game.Combat.AI
{
    /// <summary>
    /// Strategy interface for all enemy decision making
    /// Brains are stateless and only return an intended action (action intent)
    /// </summary>
    public interface IEnemyBrain
    {
        ICombatAction DecideAction(Unit enemyUnit, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver actionResolver);
    }
}

using System.Collections.Generic;
using Game.Combat.Grid;
using Game.Combat.Units;
using Game.Combat.Actions;

namespace Game.Combat
{
    ///<summary>
    /// Class that controls all AI decision making logic e.g where we implement AI heuristics for decision making
    /// Responsible for generating enemy AI action intents and creating enemy AI turn plans.
    ///<summary>
    public class EnemyDecisionService
    {
        private readonly HexGrid _grid;
        private readonly ActionResolver _actionResolver;

        public EnemyDecisionService(HexGrid grid, ActionResolver actionResolver)
        {
            _grid = grid;
            _actionResolver = actionResolver;
        }

        public ICombatAction DecideAction(Unit enemyUnit, IReadOnlyList<Unit> allUnits)
        {
            if (enemyUnit == null || !enemyUnit.IsAlive)
            {
                return null;
            }

            AI.IEnemyBrain brain = AI.BrainFactory.GetBrain(enemyUnit.AIBehavior);
            if (brain != null)
            {
                return brain.DecideAction(enemyUnit, allUnits, _grid, _actionResolver);
            }

            return null;
        }

        public void GenerateAllIntents(CombatRuntimeState state)
        {
            state.ClearIntents();

            foreach (var unit in state.AllUnits)
            {
                if (!unit.IsAlive || unit.IsPlayerControlled) continue;

                var intent = GenerateIntentForEnemy(unit, state.AllUnits);
                if (intent != null)
                {
                    state.AddIntent(intent);
                }
            }
        }

        public ActionIntent GenerateIntentForEnemy(Unit enemy, IReadOnlyList<Unit> allUnits)
        {
            var action = DecideAction(enemy, allUnits);
            if (action == null)
            {
                return null;
            }
            return _actionResolver.Preview(action);
        }

    }
}


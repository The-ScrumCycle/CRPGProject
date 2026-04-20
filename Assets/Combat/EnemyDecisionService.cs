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
        private readonly AI.EnemyActionScorer _actionScorer;

        public EnemyDecisionService(HexGrid grid, ActionResolver actionResolver)
        {
            _grid = grid;
            _actionResolver = actionResolver;
            _actionScorer = new AI.EnemyActionScorer(grid);
        }

        public ICombatAction DecideAction(Unit enemyUnit, IReadOnlyList<Unit> allUnits)
        {
            return GenerateIntentForEnemy(enemyUnit, allUnits)?.Action;
        }

        public void GenerateAllIntents(CombatRuntimeState state)
        {
            state.ClearIntents();
            var planningContext = new AI.EnemyPlanningContext();

            foreach (var unit in state.AllUnits)
            {
                if (!unit.IsAlive || unit.IsPlayerControlled) continue;

                var intent = GenerateIntentForEnemy(unit, state.AllUnits, planningContext);
                if (intent != null)
                {
                    state.AddIntent(intent);
                    RegisterIntentOutcome(intent, state.AllUnits, planningContext);
                }
            }
        }

        public ActionIntent GenerateIntentForEnemy(Unit enemy, IReadOnlyList<Unit> allUnits, AI.EnemyPlanningContext planningContext = null)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                return null;
            }

            AI.IEnemyBrain brain = AI.BrainFactory.GetBrain(enemy.AIBehavior);
            if (brain == null)
            {
                return null;
            }

            ActionIntent bestIntent = null;
            float bestScore = float.NegativeInfinity;
            var seenSignatures = new HashSet<string>();

            foreach (var action in brain.GenerateCandidateActions(enemy, allUnits, _grid, _actionResolver))
            {
                var intent = _actionResolver.Preview(action);
                if (intent == null)
                {
                    continue;
                }

                string signature = BuildIntentSignature(intent);
                if (!seenSignatures.Add(signature))
                {
                    continue;
                }

                float score = _actionScorer.Score(enemy, intent, allUnits, planningContext);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIntent = intent;
                }
            }

            return bestIntent;
        }

        private void RegisterIntentOutcome(ActionIntent intent, IReadOnlyList<Unit> allUnits, AI.EnemyPlanningContext planningContext)
        {
            if (intent == null || planningContext == null)
            {
                return;
            }

            if (intent.Action is MoveAction move)
            {
                planningContext.ReserveMoveDestination(move.Destination);
                planningContext.SetPlannedPosition(intent.Actor, move.Destination);

                if (IsFrontlineMove(intent.Actor, move.Destination, allUnits))
                {
                    planningContext.MarkFrontlineAlly(intent.Actor);
                }
            }

            foreach (var kvp in AI.EnemyActionScorer.GetPredictedDamageToPlayers(intent, allUnits))
            {
                planningContext.AddPlannedDamage(kvp.Key, kvp.Value);
            }

            if (CreatesFriendlyFireZone(intent.Action))
            {
                for (int i = 0; i < intent.TargetCells.Count; i++)
                {
                    planningContext.ReserveAttackCell(intent.TargetCells[i]);
                }
            }

            if (intent.Action is MeleeAttackAction ||
                intent.Action is SweepAttackAction ||
                intent.Action is GrappleAction ||
                intent.Actor.AIBehavior == AIBehavior.SkeletonMelee ||
                intent.Actor.AIBehavior == AIBehavior.Aggressive ||
                intent.Actor.AIBehavior == AIBehavior.HydraGrappler)
            {
                planningContext.MarkFrontlineAlly(intent.Actor);
            }
        }

        private string BuildIntentSignature(ActionIntent intent)
        {
            string signature = intent.Action.GetType().Name;

            if (intent.Action is MoveAction move)
            {
                return $"{signature}|move:{move.Destination.q},{move.Destination.r}";
            }

            for (int i = 0; i < intent.TargetCells.Count; i++)
            {
                HexCoordinates cell = intent.TargetCells[i];
                signature += $"|{cell.q},{cell.r}";
            }

            if (intent.TargetUnit != null)
            {
                signature += $"|target:{intent.TargetUnit.Id}";
            }

            return signature;
        }

        private bool IsFrontlineMove(Unit actor, HexCoordinates destination, IReadOnlyList<Unit> allUnits)
        {
            if (actor == null ||
                (actor.AIBehavior != AIBehavior.SkeletonMelee &&
                 actor.AIBehavior != AIBehavior.Aggressive &&
                 actor.AIBehavior != AIBehavior.HydraGrappler))
            {
                return false;
            }

            foreach (var unit in allUnits)
            {
                if (unit.IsAlive && unit.IsPlayerControlled && _grid.GetDistance(unit.Coordinates, destination) <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CreatesFriendlyFireZone(ICombatAction action)
        {
            return action is SplashAttackAction || action is SweepAttackAction;
        }
    }
}
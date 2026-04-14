using System.Collections.Generic;
using UnityEngine;
using Game.Combat.Actions;
using Game.Combat.Grid;
using Game.Combat.Units;

namespace Game.Combat.AI
{
    /// <summary>
    /// Assigns utility weights to AI candidate intents. Brains propose options; the scorer picks the best one.
    /// </summary>
    public class EnemyActionScorer
    {
        private const float DamageWeight = 1.5f;
        private const float KillBonus = 40.0f;
        private const float FocusFireBonus = 10.0f;
        private const float OverkillPenalty = 18.0f;
        private const float GrappleBonus = 26.0f;
        private const float HealedHpWeight = 1.75f;
        private const float HealCriticalBonus = 18.0f;
        private const float RetreatBonus = 16.0f;
        private const float FrontlineProtectBonus = 16.0f;
        private const float FriendlyHitPenalty = 30.0f;
        private const float SelfHitPenalty = 45.0f;
        private const float ReservedMovePenalty = 1000.0f;
        private const float ReservedAttackCellPenalty = 1000.0f;

        private readonly HexGrid _grid;

        public EnemyActionScorer(HexGrid grid)
        {
            _grid = grid;
        }

        public float Score(Unit enemyUnit, ActionIntent intent, IReadOnlyList<Unit> allUnits, EnemyPlanningContext planningContext)
        {
            if (enemyUnit == null || intent == null || !intent.IsValid || intent.Action == null)
            {
                return float.NegativeInfinity;
            }

            float score = 0.0f;

            if (intent.Action is MoveAction move)
            {
                score += ScoreMove(enemyUnit, move, allUnits, planningContext);
            }
            else if (intent.Action is RangedHealAction heal)
            {
                score += ScoreHeal(heal.Target, intent, allUnits, planningContext);
            }
            else
            {
                score += ScoreDamageIntent(intent, allUnits, planningContext);
            }

            if (intent.Action is GrappleAction grapple)
            {
                score += ScoreGrapple(grapple.Target);
            }

            score += ScoreRetreatPreference(enemyUnit, intent, allUnits);

            return score;
        }

        public static Dictionary<Unit, int> GetPredictedDamageToPlayers(ActionIntent intent, IReadOnlyList<Unit> allUnits)
        {
            var predictedDamage = new Dictionary<Unit, int>();

            if (intent == null || intent.Action == null || intent.PredictedDamage <= 0)
            {
                return predictedDamage;
            }

            var targetedCells = new HashSet<HexCoordinates>(intent.TargetCells);

            foreach (var unit in allUnits)
            {
                if (unit == null || !unit.IsAlive || !unit.IsPlayerControlled)
                {
                    continue;
                }

                if (targetedCells.Contains(unit.Coordinates))
                {
                    predictedDamage[unit] = intent.PredictedDamage;
                }
            }

            if (intent.SecondaryBumpTarget != null && intent.SecondaryBumpTarget.IsPlayerControlled)
            {
                predictedDamage[intent.SecondaryBumpTarget] =
                    (predictedDamage.TryGetValue(intent.SecondaryBumpTarget, out int existingDamage) ? existingDamage : 0) + 10;
            }

            return predictedDamage;
        }

        private float ScoreDamageIntent(ActionIntent intent, IReadOnlyList<Unit> allUnits, EnemyPlanningContext planningContext)
        {
            float score = 0.0f;

            foreach (var kvp in GetPredictedDamageToPlayers(intent, allUnits))
            {
                Unit target = kvp.Key;
                int predictedDamage = kvp.Value;
                int plannedDamage = planningContext != null ? planningContext.GetPlannedDamage(target) : 0;
                int remainingHealth = target.Stats.currentHealth - plannedDamage;

                score += predictedDamage * DamageWeight;

                if (remainingHealth <= 0)
                {
                    score -= OverkillPenalty;
                    continue;
                }

                if (plannedDamage > 0)
                {
                    score += FocusFireBonus;
                }

                if (predictedDamage >= remainingHealth)
                {
                    score += KillBonus;
                }
            }

            score += ScoreFriendlyFirePenalty(intent, allUnits, planningContext);

            return score;
        }

        private float ScoreFriendlyFirePenalty(ActionIntent intent, IReadOnlyList<Unit> allUnits, EnemyPlanningContext planningContext)
        {
            if (intent?.Actor == null || allUnits == null || intent.PredictedDamage <= 0)
            {
                return 0.0f;
            }

            float penalty = 0.0f;
            var targetedCells = new HashSet<HexCoordinates>(intent.TargetCells);

            foreach (var unit in allUnits)
            {
                if (unit == null || !unit.IsAlive || unit.IsPlayerControlled != intent.Actor.IsPlayerControlled)
                {
                    continue;
                }

                HexCoordinates threatenedPosition = planningContext != null
                    ? planningContext.GetProjectedPosition(unit)
                    : unit.Coordinates;

                if (targetedCells.Contains(threatenedPosition))
                {
                    penalty -= unit == intent.Actor ? SelfHitPenalty : FriendlyHitPenalty;
                }
            }

            if (intent.SecondaryBumpTarget != null &&
                intent.SecondaryBumpTarget.IsAlive &&
                intent.SecondaryBumpTarget.IsPlayerControlled == intent.Actor.IsPlayerControlled)
            {
                penalty -= intent.SecondaryBumpTarget == intent.Actor ? SelfHitPenalty : FriendlyHitPenalty;
            }

            return penalty;
        }

        private float ScoreHeal(Unit target, ActionIntent intent, IReadOnlyList<Unit> allUnits, EnemyPlanningContext planningContext)
        {
            if (target == null || !target.IsAlive)
            {
                return 0.0f;
            }

            int missingHealth = target.Stats.maxHealth - target.Stats.currentHealth;
            int healAmount = Mathf.Min(-intent.PredictedDamage, missingHealth);

            if (healAmount <= 0)
            {
                return 0.0f;
            }

            float score = healAmount * HealedHpWeight;
            float healthRatio = (float)target.Stats.currentHealth / target.Stats.maxHealth;

            score += (1.0f - healthRatio) * HealCriticalBonus;
            if (IsFrontlineProtectionTarget(target, allUnits, planningContext))
            {
                score += FrontlineProtectBonus;
            }

            return score;
        }

        private float ScoreGrapple(Unit target)
        {
            if (target == null || !target.IsAlive)
            {
                return 0.0f;
            }

            float threatScore = target.Stats.attackPower * 0.35f + target.Stats.movementRange * 1.5f;
            return GrappleBonus + threatScore;
        }

        private float ScoreMove(Unit enemyUnit, MoveAction move, IReadOnlyList<Unit> allUnits, EnemyPlanningContext planningContext)
        {
            float score = 0.0f;

            if (planningContext != null && planningContext.IsMoveDestinationReserved(move.Destination))
            {
                score -= ReservedMovePenalty;
            }

            if (planningContext != null && planningContext.IsAttackCellReserved(move.Destination))
            {
                score -= ReservedAttackCellPenalty;
            }

            switch (enemyUnit.AIBehavior)
            {
                case AIBehavior.SkeletonRanged:
                case AIBehavior.Defensive:
                    score += ScoreRangedPositioning(enemyUnit, move.Destination, allUnits);
                    break;
                case AIBehavior.Healer:
                    score += ScoreHealerPositioning(enemyUnit, move.Destination, allUnits);
                    break;
                case AIBehavior.HydraGrappler:
                    score += ScoreAdvanceToTarget(enemyUnit, move.Destination, allUnits, BrainHelpers.FindNearestPlayer(enemyUnit, allUnits, _grid));
                    break;
                case AIBehavior.SkeletonMelee:
                case AIBehavior.Aggressive:
                default:
                    score += ScoreAdvanceToTarget(enemyUnit, move.Destination, allUnits, FindLowestHpPlayer(allUnits));
                    break;
            }

            return score;
        }

        private float ScoreAdvanceToTarget(Unit enemyUnit, HexCoordinates destination, IReadOnlyList<Unit> allUnits, Unit target)
        {
            target ??= BrainHelpers.FindNearestPlayer(enemyUnit, allUnits, _grid);
            if (target == null)
            {
                return 0.0f;
            }

            int before = _grid.GetDistance(enemyUnit.Coordinates, target.Coordinates);
            int after = _grid.GetDistance(destination, target.Coordinates);
            float score = (before - after) * 12.0f;

            if (after == 1)
            {
                score += 10.0f;
            }

            return score;
        }

        private float ScoreRangedPositioning(Unit enemyUnit, HexCoordinates destination, IReadOnlyList<Unit> allUnits)
        {
            Unit nearestPlayer = BrainHelpers.FindNearestPlayer(enemyUnit, allUnits, _grid);
            if (nearestPlayer == null)
            {
                return 0.0f;
            }

            int desiredRange = enemyUnit.Stats.attackRange;
            int before = _grid.GetDistance(enemyUnit.Coordinates, nearestPlayer.Coordinates);
            int after = _grid.GetDistance(destination, nearestPlayer.Coordinates);
            float score = (Mathf.Abs(before - desiredRange) - Mathf.Abs(after - desiredRange)) * 9.0f;

            if (after > 1 && after <= desiredRange)
            {
                score += 12.0f;
            }
            else if (after == 1)
            {
                score -= 10.0f;
            }

            return score;
        }

        private float ScoreHealerPositioning(Unit enemyUnit, HexCoordinates destination, IReadOnlyList<Unit> allUnits)
        {
            float score = 0.0f;
            Unit nearestPlayer = BrainHelpers.FindNearestPlayer(enemyUnit, allUnits, _grid);

            if (nearestPlayer != null)
            {
                int beforeThreat = _grid.GetDistance(enemyUnit.Coordinates, nearestPlayer.Coordinates);
                int afterThreat = _grid.GetDistance(destination, nearestPlayer.Coordinates);
                score += (afterThreat - beforeThreat) * 10.0f;
            }

            Unit damagedAlly = BrainHelpers.FindMostDamagedAlly(enemyUnit, allUnits);
            if (damagedAlly != null)
            {
                int beforeSupport = _grid.GetDistance(enemyUnit.Coordinates, damagedAlly.Coordinates);
                int afterSupport = _grid.GetDistance(destination, damagedAlly.Coordinates);

                if (beforeSupport > enemyUnit.Stats.attackRange && afterSupport <= enemyUnit.Stats.attackRange)
                {
                    score += 16.0f;
                }

                score += (beforeSupport - afterSupport) * 4.0f;
            }

            return score;
        }

        private float ScoreRetreatPreference(Unit enemyUnit, ActionIntent intent, IReadOnlyList<Unit> allUnits)
        {
            if (!BrainHelpers.ShouldRetreatToHealer(enemyUnit, allUnits, out Unit healer))
            {
                return 0.0f;
            }

            if (intent.Action is MoveAction move)
            {
                int before = _grid.GetDistance(enemyUnit.Coordinates, healer.Coordinates);
                int after = _grid.GetDistance(move.Destination, healer.Coordinates);

                if (after < before)
                {
                    return RetreatBonus;
                }
            }

            return -8.0f;
        }

        private bool IsFrontlineProtectionTarget(Unit target, IReadOnlyList<Unit> allUnits, EnemyPlanningContext planningContext)
        {
            if (planningContext != null && planningContext.IsFrontlineAlly(target))
            {
                return true;
            }

            if (target.AIBehavior == AIBehavior.SkeletonMelee ||
                target.AIBehavior == AIBehavior.Aggressive ||
                target.AIBehavior == AIBehavior.HydraGrappler)
            {
                return true;
            }

            foreach (var unit in allUnits)
            {
                if (unit.IsAlive && unit.IsPlayerControlled && _grid.GetDistance(unit.Coordinates, target.Coordinates) == 1)
                {
                    return true;
                }
            }

            return false;
        }

        private Unit FindLowestHpPlayer(IReadOnlyList<Unit> allUnits)
        {
            Unit lowest = null;
            int lowestHp = int.MaxValue;

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || !unit.IsPlayerControlled)
                {
                    continue;
                }

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

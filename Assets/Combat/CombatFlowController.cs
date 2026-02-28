using System.Collections.Generic;
using UnityEngine;
using Game.Combat.Grid;
using Game.Combat.Turn;
using Game.Combat.Units;
using Game.Combat.Actions;

namespace Game.Combat
{
    ///<summary>
    /// Class that orchestrates the turn flow of the combat engine, makes it easier to keep track of combat sequence.
    /// Responsible for starting the player and enemy turns, ending turns, checking for victory/defeat conditions,
    /// does this through the CombatRuntimeState that keeps track of all units.
    ///<summary>
    public class CombatFlowController
    {
        private readonly CombatRuntimeState _state;
        private readonly TurnSystem _turnSystem;
        private readonly ActionResolver _actionResolver;
        private readonly HexGrid _grid;
        private readonly EnemyDecisionService _enemyAI;

        public CombatFlowController(
            CombatRuntimeState state,
            TurnSystem turnSystem,
            ActionResolver actionResolver,
            HexGrid grid,
            EnemyDecisionService enemyAI)
        {
            _state = state;
            _turnSystem = turnSystem;
            _actionResolver = actionResolver;
            _grid = grid;
            _enemyAI = enemyAI;
        }

        public void StartPlayerTurn()
        {
            _state.CurrentState = CombatState.PlayerTurn;
            _state.PlayerHasActed = false;

            var currentUnit = _turnSystem.GetCurrentUnit();
            Debug.Log($"[CombatManager] Player turn started: {currentUnit?.DisplayName}");
        }

        public void StartEnemyTurn()
        {
            _state.CurrentState = CombatState.EnemyTurn;

            var currentUnit = _turnSystem.GetCurrentUnit();
            Debug.Log($"[CombatManager] Enemy turn started: {currentUnit?.DisplayName}");
        }

        public bool CheckVictory()
        {
            return !_turnSystem.HasAliveEnemyUnits();
        }

        public bool CheckDefeat()
        {
            return !_turnSystem.HasAlivePlayerUnits();
        }

        public void AdvanceTurn()
        {
            _turnSystem.AdvanceTurn();
            _enemyAI.GenerateAllIntents(_state);
        }

        public Unit GetCurrentUnit()
        {
            return _turnSystem.GetCurrentUnit();
        }

        public bool IsCurrentUnitPlayerControlled()
        {
            var currentUnit = GetCurrentUnit();
            return currentUnit != null && currentUnit.IsPlayerControlled;
        }

        public void InitializeTurnOrder(IEnumerable<Unit> units)
        {
            _turnSystem.Initialize(units);
        }

        public void RemoveUnitFromTurnOrder(Unit unit)
        {
            _turnSystem.RemoveUnit(unit);
        }

        public void GenerateInitialIntents()
        {
            _enemyAI.GenerateAllIntents(_state);
        }

        public void SetPlayerActed()
        {
            _state.PlayerHasActed = true;
        }

        public bool HasPlayerActed()
        {
            return _state.PlayerHasActed;
        }

        public void SetCombatState(CombatState newState)
        {
            _state.CurrentState = newState;
        }

        // Get all valid move destinations for a unit
        public List<HexCoordinates> GetValidMoves(Unit unit)
        {
            return _actionResolver.GetValidMoveDestinations(unit);
        }

        // Get all valid attack target coordinates for a unit (melee + ranged combined)
        public List<HexCoordinates> GetValidAttackTargets(Unit unit)
        {
            var targets = new List<HexCoordinates>();
            var seen = new HashSet<HexCoordinates>();

            var meleeTargets = _actionResolver.GetValidMeleeTargets(unit);
            foreach (var target in meleeTargets)
            {
                if (seen.Add(target.Coordinates))
                {
                    targets.Add(target.Coordinates);
                }
            }

            var rangedTargets = _actionResolver.GetValidRangedTargets(unit);
            foreach (var target in rangedTargets)
            {
                if (seen.Add(target.Coordinates))
                {
                    targets.Add(target.Coordinates);
                }
            }

            return targets;
        }
    }
} 
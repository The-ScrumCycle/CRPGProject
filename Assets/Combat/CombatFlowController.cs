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
            _state.ClearActedUnits(); // new pool of units for new turn

            var firstUnit = GetNextAvailablePlayerUnit();
            if (firstUnit != null) SelectPlayerUnit(firstUnit);
            
            Debug.Log($"[CombatManager] Player phase started.");
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
            int oldRound = _turnSystem.RoundNumber;
            _turnSystem.AdvanceTurn();

            if (_turnSystem.RoundNumber > oldRound)
            {
                _state.ClearActedUnits(); // Clear enemies/players on new round
                _enemyAI.GenerateAllIntents(_state);
            }

            // Fast-forward past any remaining players if the shared player phase is complete
            while (_turnSystem.GetCurrentUnit() != null &&
                   _turnSystem.GetCurrentUnit().IsPlayerControlled &&
                   HaveAllPlayerUnitsActed())
            {
                int currentRound = _turnSystem.RoundNumber;
                _turnSystem.AdvanceTurn();
                
                // Catch round end inside the fast-forward
                if (_turnSystem.RoundNumber > currentRound)
                {
                    _state.ClearActedUnits();
                    _enemyAI.GenerateAllIntents(_state);
                }
            }
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

        public void MarkUnitActed(Unit unit) 
        { 
            _state.ActedUnits.Add(unit); 
            _state.MovedUnits.Add(unit); // Acting also exhausts movement e.g ends unit's turn
        }
        
        public void MarkUnitMoved(Unit unit) 
        { 
            _state.MovedUnits.Add(unit); 
        }

        public bool HasUnitActed(Unit unit) { return _state.ActedUnits.Contains(unit); }
        public bool HasUnitMoved(Unit unit) { return _state.MovedUnits.Contains(unit); }

        public bool HaveAllPlayerUnitsActed()
        {
            foreach (var unit in _state.AllUnits)
            {
                if (unit.IsPlayerControlled && unit.IsAlive && !HasUnitActed(unit))
                    return false;
            }
            return true;
        }

        public Unit GetNextAvailablePlayerUnit()
        {
            foreach (var unit in _state.AllUnits)
            {
                if (unit.IsPlayerControlled && unit.IsAlive && !HasUnitActed(unit))
                    return unit;
            }
            return null;
        }

        public void SelectPlayerUnit(Unit unit)
        {
            _turnSystem.SetCurrentUnit(unit);
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
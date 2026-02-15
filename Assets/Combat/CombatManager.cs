using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Core.Transitions;
using Game.Combat.Grid;
using Game.Combat.Turn;
using Game.Combat.Units;
using Game.Combat.Actions;

namespace Game.Combat
{
    /// <summary>
    /// Main orchestrator for the combat system.
    /// Owns all combat subsystems and manages combat flow.
    /// Currently also executes the AI, ideally we do a SOC later to decouple AI from the manager.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("Grid Configuration")]
        [SerializeField] private HexGridRenderer gridRenderer;
        [SerializeField] private int gridWidth = 8;
        [SerializeField] private int gridHeight = 8;

        [Header("Unit Spawning")]
        [SerializeField] private UnitFactory unitFactory;
        [SerializeField] private HexCoordinates playerStartPosition = new HexCoordinates(1, 1);
        [SerializeField] private HexCoordinates enemyStartPosition = new HexCoordinates(6, 6);

        [Header("Combat Settings")]
        [SerializeField] private int victoryExperience = 100;

        // Combat State
        public CombatState CurrentState { get; private set; } = CombatState.Initializing;

        // Owned Subsystems
        private HexGrid _grid;
        private TurnSystem _turnSystem;
        private ActionResolver _actionResolver;

        // Unit Tracking
        private List<Unit> _allUnits = new List<Unit>();
        private Dictionary<Unit, UnitVisual> _unitVisuals = new Dictionary<Unit, UnitVisual>();

        // Current turn state
        private bool _playerHasActed = false;

        // Enemy intents for preview
        private List<ActionIntent> _enemyIntents = new List<ActionIntent>();

        #region Unity Lifecycle

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            InitializeCombat();
        }

        void Update()
        {
            if (CurrentState == CombatState.PlayerTurn)
            {
                HandlePlayerInput();
            }
        }

        #endregion

        #region Initialization

        // Initialize the combat system when scene loads.
        private void InitializeCombat()
        {
            Debug.Log("[CombatManager] Initializing combat...");

            // Create logical grid
            _grid = new HexGrid(gridWidth, gridHeight);

            // Initialize renderer with logical grid
            if (gridRenderer != null)
            {
                gridRenderer.Initialize(_grid);
            }

            // Create action resolver
            _actionResolver = new ActionResolver(_grid);

            // Create turn system
            _turnSystem = new TurnSystem();

            // Spawn units
            SpawnUnits();

            // Initialize turn order
            _turnSystem.Initialize(_allUnits);

            // Generate initial enemy intents
            GenerateEnemyIntents();

            // Start combat
            StartPlayerTurn();

            Debug.Log($"[CombatManager] Combat initialized with {_allUnits.Count} units");
        }

        // Spawn player and enemy units based on transition data.
        private void SpawnUnits()
        {
            // Spawn player unit
            var (playerUnit, playerVisual) = unitFactory.CreatePlayerUnit();
            RegisterUnit(playerUnit, playerVisual, playerStartPosition);

            // Spawn enemy unit from transition data
            var transitionData = CombatTransitionData.Instance;
            var (enemyUnit, enemyVisual) = unitFactory.CreateEnemyUnit(transitionData);
            RegisterUnit(enemyUnit, enemyVisual, enemyStartPosition);
        }

        // Register a unit in the combat system.
        private void RegisterUnit(Unit unit, UnitVisual visual, HexCoordinates startPosition)
        {
            // Place unit on grid
            if (!_grid.PlaceUnit(unit, startPosition))
            {
                Debug.LogError($"[CombatManager] Failed to place unit {unit.DisplayName} at {startPosition}");
                return;
            }

            // Initialize visual
            visual.Initialize(unit, gridRenderer);
            visual.SnapToPosition();

            // Track unit
            _allUnits.Add(unit);
            _unitVisuals[unit] = visual;

            Debug.Log($"[CombatManager] Registered unit: {unit}");
        }

        #endregion

        #region Turn Management

        // Start the player's turn.
        private void StartPlayerTurn()
        {
            CurrentState = CombatState.PlayerTurn;
            _playerHasActed = false;

            var currentUnit = _turnSystem.GetCurrentUnit();
            Debug.Log($"[CombatManager] Player turn started: {currentUnit?.DisplayName}");

            // Highlight valid move destinations
            if (currentUnit != null)
            {
                var validMoves = _actionResolver.GetValidMoveDestinations(currentUnit);
                gridRenderer.SetHighlightedCells(validMoves);
            }
        }

        // Start the enemy's turn.
        private void StartEnemyTurn()
        {
            CurrentState = CombatState.EnemyTurn;
            gridRenderer.ClearHighlight();

            var currentUnit = _turnSystem.GetCurrentUnit();
            Debug.Log($"[CombatManager] Enemy turn started: {currentUnit?.DisplayName}");

            // Execute enemy AI
            ExecuteEnemyTurn(currentUnit);
        }

        // End the current turn and advance to the next.
        private void EndTurn()
        {
            // Check victory/defeat conditions
            if (CheckVictory())
            {
                EndCombat(true);
                return;
            }

            if (CheckDefeat())
            {
                EndCombat(false);
                return;
            }

            // Advance turn
            _turnSystem.AdvanceTurn();

            // Regenerate enemy intents
            GenerateEnemyIntents();

            // Start appropriate turn
            var currentUnit = _turnSystem.GetCurrentUnit();
            if (currentUnit != null && currentUnit.IsPlayerControlled)
            {
                StartPlayerTurn();
            }
            else
            {
                StartEnemyTurn();
            }
        }

        #endregion

        #region Player Input

        // Handle player input during their turn.
        private void HandlePlayerInput()
        {
            if (_playerHasActed) return;

            var currentUnit = _turnSystem.GetCurrentUnit();
            if (currentUnit == null || !currentUnit.IsPlayerControlled) return;

            // Left click = Move or Attack
            if (Input.GetMouseButtonDown(0))
            {
                var clickedHex = gridRenderer.GetHoveredHex();
                var clickedCell = _grid.GetCell(clickedHex);

                if (clickedCell == null) return;

                // Check if clicking on enemy - attack
                if (clickedCell.Occupant != null && clickedCell.Occupant.Role == UnitRole.Enemy)
                {
                    TryPlayerAttack(currentUnit, clickedCell.Occupant);
                }
                // Otherwise try to move
                else if (clickedCell.CanEnter())
                {
                    TryPlayerMove(currentUnit, clickedHex);
                }
            }

            // Space = End turn without acting
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[CombatManager] Player ended turn early");
                _playerHasActed = true;
                EndTurn();
            }
        }

        // Attempt to move the player unit.
        private void TryPlayerMove(Unit unit, HexCoordinates destination)
        {
            var moveAction = _actionResolver.CreateMoveAction(unit, destination);

            if (_actionResolver.Execute(moveAction))
            {
                // Update visual
                if (_unitVisuals.TryGetValue(unit, out var visual))
                {
                    visual.RefreshPosition();
                }

                _playerHasActed = true;
                gridRenderer.ClearHighlight();

                // Small delay then end turn
                Invoke(nameof(EndTurn), 0.3f);
            }
        }

        // Attempt to attack with the player unit.
        private void TryPlayerAttack(Unit attacker, Unit target)
        {
            ICombatAction attackAction;

            // Determine attack type based on distance
            int distance = _grid.GetDistance(attacker.Coordinates, target.Coordinates);

            if (distance == 1)
            {
                attackAction = _actionResolver.CreateMeleeAttack(attacker, target);
            }
            else if (distance <= attacker.Stats.attackRange)
            {
                attackAction = _actionResolver.CreateRangedAttack(attacker, target);
            }
            else
            {
                Debug.Log("[CombatManager] Target out of range");
                return;
            }

            if (_actionResolver.Execute(attackAction))
            {
                Debug.Log($"[CombatManager] {attacker.DisplayName} attacked {target.DisplayName} for {attacker.Stats.attackPower} damage");

                // Check if target died
                if (!target.IsAlive)
                {
                    HandleUnitDeath(target);
                }

                _playerHasActed = true;
                gridRenderer.ClearHighlight();

                // Small delay then end turn
                Invoke(nameof(EndTurn), 0.3f);
            }
        }

        #endregion

        #region Enemy AI

        // Execute the enemy unit's turn using a simplified AI
        private void ExecuteEnemyTurn(Unit enemyUnit)
        {
            if (enemyUnit == null || !enemyUnit.IsAlive)
            {
                EndTurn();
                return;
            }

            // Find nearest player unit
            Unit targetUnit = FindNearestPlayerUnit(enemyUnit);

            if (targetUnit == null)
            {
                EndTurn();
                return;
            }

            int distanceToTarget = _grid.GetDistance(enemyUnit.Coordinates, targetUnit.Coordinates);

            // Determine behavior based on AI type
            bool shouldAttack = false;
            ICombatAction action = null;

            switch (enemyUnit.AIBehavior)
            {
                case AIBehavior.Aggressive:
                    // Try to get close and melee
                    if (distanceToTarget == 1)
                    {
                        action = _actionResolver.CreateMeleeAttack(enemyUnit, targetUnit);
                        shouldAttack = true;
                    }
                    else
                    {
                        action = CreateMoveTowardTarget(enemyUnit, targetUnit);
                    }
                    break;

                case AIBehavior.Defensive:
                    // Try to stay at range and shoot
                    if (distanceToTarget <= enemyUnit.Stats.attackRange && distanceToTarget > 1)
                    {
                        action = _actionResolver.CreateRangedAttack(enemyUnit, targetUnit);
                        shouldAttack = true;
                    }
                    else if (distanceToTarget == 1)
                    {
                        // Too close, back away or melee
                        action = CreateMoveAwayFromTarget(enemyUnit, targetUnit);
                        if (action == null || !_actionResolver.Validate(action))
                        {
                            action = _actionResolver.CreateMeleeAttack(enemyUnit, targetUnit);
                            shouldAttack = true;
                        }
                    }
                    else
                    {
                        action = CreateMoveTowardTarget(enemyUnit, targetUnit);
                    }
                    break;

                default:
                    action = CreateMoveTowardTarget(enemyUnit, targetUnit);
                    break;
            }

            // Execute action
            if (action != null && _actionResolver.Execute(action))
            {
                if (shouldAttack && action is MeleeAttackAction || action is RangedAttackAction)
                {
                    Debug.Log($"[CombatManager] {enemyUnit.DisplayName} attacked {targetUnit.DisplayName}");

                    if (!targetUnit.IsAlive)
                    {
                        HandleUnitDeath(targetUnit);
                    }
                }
                else
                {
                    // Update visual for move
                    if (_unitVisuals.TryGetValue(enemyUnit, out var visual))
                    {
                        visual.RefreshPosition();
                    }
                }
            }

            // End turn after delay
            Invoke(nameof(EndTurn), 0.5f);
        }

        // Create a move action toward the target.
        private MoveAction CreateMoveTowardTarget(Unit mover, Unit target)
        {
            var reachableCells = _grid.GetReachableCells(mover.Coordinates, mover.Stats.movementRange);
            HexCoordinates bestDestination = mover.Coordinates;
            int bestDistance = _grid.GetDistance(mover.Coordinates, target.Coordinates);

            foreach (var cell in reachableCells)
            {
                if (!cell.CanEnter()) continue;

                int distance = _grid.GetDistance(cell.Coordinates, target.Coordinates);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestDestination = cell.Coordinates;
                }
            }

            if (bestDestination != mover.Coordinates)
            {
                return _actionResolver.CreateMoveAction(mover, bestDestination);
            }

            return null;
        }

        // Create a move action away from the target.
        private MoveAction CreateMoveAwayFromTarget(Unit mover, Unit target)
        {
            var reachableCells = _grid.GetReachableCells(mover.Coordinates, mover.Stats.movementRange);
            HexCoordinates bestDestination = mover.Coordinates;
            int bestDistance = _grid.GetDistance(mover.Coordinates, target.Coordinates);

            foreach (var cell in reachableCells)
            {
                if (!cell.CanEnter()) continue;

                int distance = _grid.GetDistance(cell.Coordinates, target.Coordinates);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    bestDestination = cell.Coordinates;
                }
            }

            if (bestDestination != mover.Coordinates)
            {
                return _actionResolver.CreateMoveAction(mover, bestDestination);
            }

            return null;
        }

        /// Find the nearest player-controlled unit.
        private Unit FindNearestPlayerUnit(Unit fromUnit)
        {
            Unit nearest = null;
            int nearestDistance = int.MaxValue;

            foreach (var unit in _allUnits)
            {
                if (!unit.IsAlive || !unit.IsPlayerControlled) continue;

                int distance = _grid.GetDistance(fromUnit.Coordinates, unit.Coordinates);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = unit;
                }
            }

            return nearest;
        }

        // Generate intents for all enemy units (for preview)
        private void GenerateEnemyIntents()
        {
            _enemyIntents.Clear();

            foreach (var unit in _allUnits)
            {
                if (!unit.IsAlive || unit.IsPlayerControlled) continue;

                var intent = GenerateIntentForEnemy(unit);
                if (intent != null)
                {
                    _enemyIntents.Add(intent);
                }
            }
        }

        // Generate an intent for a single enemy.
        private ActionIntent GenerateIntentForEnemy(Unit enemy)
        {
            var target = FindNearestPlayerUnit(enemy);
            if (target == null) return null;

            int distance = _grid.GetDistance(enemy.Coordinates, target.Coordinates);

            ICombatAction plannedAction = null;

            switch (enemy.AIBehavior)
            {
                case AIBehavior.Aggressive:
                    if (distance == 1)
                    {
                        plannedAction = _actionResolver.CreateMeleeAttack(enemy, target);
                    }
                    else
                    {
                        plannedAction = CreateMoveTowardTarget(enemy, target);
                    }
                    break;

                case AIBehavior.Defensive:
                    if (distance <= enemy.Stats.attackRange && distance > 1)
                    {
                        plannedAction = _actionResolver.CreateRangedAttack(enemy, target);
                    }
                    else
                    {
                        plannedAction = CreateMoveTowardTarget(enemy, target);
                    }
                    break;
            }

            if (plannedAction != null)
            {
                return _actionResolver.Preview(plannedAction);
            }

            return null;
        }

        // Get current enemy intents for UI display.
        public IReadOnlyList<ActionIntent> GetEnemyIntents()
        {
            return _enemyIntents.AsReadOnly();
        }

        #endregion

        #region Combat Resolution

        // Handle a unit's death.
        private void HandleUnitDeath(Unit unit)
        {
            Debug.Log($"[CombatManager] Unit died: {unit.DisplayName}");

            // Remove from grid
            unit.CurrentCell?.ClearOccupant();

            // Remove from turn order
            _turnSystem.RemoveUnit(unit);

            // Destroy visual
            if (_unitVisuals.TryGetValue(unit, out var visual))
            {
                Destroy(visual.gameObject);
                _unitVisuals.Remove(unit);
            }
        }

        // Check if player has won.
        private bool CheckVictory()
        {
            return !_turnSystem.HasAliveEnemyUnits();
        }

        // Check if player has been defeated
        private bool CheckDefeat()
        {
            return !_turnSystem.HasAlivePlayerUnits();
        }

        // End combat and transition back to exploration.
        private void EndCombat(bool victory)
        {
            CurrentState = victory ? CombatState.Victory : CombatState.Defeat;
            Debug.Log($"[CombatManager] Combat ended: {(victory ? "VICTORY" : "DEFEAT")}");

            // Create result data
            var transitionData = CombatTransitionData.Instance;
            var result = new CombatResultData(
                victory: victory,
                experience: victory ? victoryExperience : 0,
                enemyName: transitionData?.enemyName ?? "Unknown"
            );

            // Transition back to exploration
            GameStateManager.Instance.TransitionToExploration(result);
        }

        #endregion

        #region Public API

        // Get the current unit whose turn it is.
        public Unit GetCurrentUnit()
        {
            return _turnSystem?.GetCurrentUnit();
        }

        // Get the logical hex grid.
        public HexGrid GetGrid()
        {
            return _grid;
        }

        // Get the action resolver.
        public ActionResolver GetActionResolver()
        {
            return _actionResolver;
        }

        // Get all units in combat.
        public IReadOnlyList<Unit> GetAllUnits()
        {
            return _allUnits.AsReadOnly();
        }

        #endregion
    }
}

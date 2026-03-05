using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Game.Core;
using Game.Core.Transitions;
using Game.Combat.Grid;
using Game.Combat.Turn;
using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.UI;

namespace Game.Combat
{
    // Player action mode - tracks player action selection
    public enum PlayerActionMode
    {
        Move,
        Attack
    }
    /// <summary>
    /// Main orchestrator for the combat system.
    /// Handles the combat engine's lifecycle, input, unity visuals and transitions between states.
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

        public CombatState CurrentState => _state.CurrentState;
        public PlayerActionMode CurrentActionMode => _currentActionMode;
        private PlayerActionMode _currentActionMode = PlayerActionMode.Move;

        private HexGrid _grid;
        private TurnSystem _turnSystem;
        private ActionResolver _actionResolver;

        private CombatRuntimeState _state;
        private CombatFlowController _flowController;
        private EnemyDecisionService _enemyAI;
        private CombatIntentRenderer _intentRenderer;

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
            if (_state.CurrentState == CombatState.PlayerTurn)
            {
                HandlePlayerInput();
            }
        }

        #endregion

        #region Initialization
	    // Initalize our combat, handles visual layers and starts all systems necessary to manage combat
        private void InitializeCombat()
        {
            Debug.Log("[CombatManager] Initializing combat...");

            _grid = new HexGrid(gridWidth, gridHeight);

            if (gridRenderer != null)
            {
                gridRenderer.Initialize(_grid);
            }

            _actionResolver = new ActionResolver(_grid);
            _turnSystem = new TurnSystem();
            _state = new CombatRuntimeState();
            _enemyAI = new EnemyDecisionService(_grid, _actionResolver);
            _flowController = new CombatFlowController(_state, _turnSystem, _actionResolver, _grid, _enemyAI);
            _intentRenderer = new CombatIntentRenderer();

            SpawnUnits();

            _flowController.InitializeTurnOrder(_state.AllUnits);
            _flowController.GenerateInitialIntents();

            BeginPlayerTurn();

            Debug.Log($"[CombatManager] Combat initialized with {_state.AllUnits.Count} units");
        }

        private void SpawnUnits()
        {
            var (playerUnit, playerVisual) = unitFactory.CreatePlayerUnit();
            RegisterUnit(playerUnit, playerVisual, playerStartPosition);

            var transitionData = CombatTransitionData.Instance;
            var (enemyUnit, enemyVisual) = unitFactory.CreateEnemyUnit(transitionData);
            RegisterUnit(enemyUnit, enemyVisual, enemyStartPosition);
        }

        private void RegisterUnit(Unit unit, UnitVisual visual, HexCoordinates startPosition)
        {
            if (!_grid.PlaceUnit(unit, startPosition))
            {
                Debug.LogError($"[CombatManager] Failed to place unit {unit.DisplayName} at {startPosition}");
                return;
            }

            visual.Initialize(unit, gridRenderer);
            visual.SnapToPosition();

            _state.RegisterUnit(unit, visual);

            Debug.Log($"[CombatManager] Registered unit: {unit}");
        }

        #endregion

        #region Highlight Management
        // Set the player action mode and refresh highlights accordingly
        public void SetActionMode(PlayerActionMode mode)
        {
            _currentActionMode = mode;
            Debug.Log($"[CombatManager] Action mode set to: {_currentActionMode}");
            RefreshPlayerHighlights();
        }

        // Build and push all active highlights to the renderer.
        // Player move highlights first, then AI intents layered on top via priority of the action intents.
        private void RefreshPlayerHighlights()
        {
            gridRenderer.ClearHighlights();
            var currentUnit = _flowController.GetCurrentUnit();

            // Layer 1: Player highlights
            if (currentUnit != null && currentUnit.IsPlayerControlled)
            {
                if (_currentActionMode == PlayerActionMode.Move)
                {
                    var validMoves = _flowController.GetValidMoves(currentUnit);
                    foreach (var coord in validMoves)
                        gridRenderer.AddHighlight(coord, HighlightType.PlayerMove);
                }
                else if (_currentActionMode == PlayerActionMode.Attack)
                {
                    var validTargets = _flowController.GetValidAttackTargets(currentUnit);
                    foreach (var coord in validTargets)
                        gridRenderer.AddHighlight(coord, HighlightType.PlayerAttack);
                }
            }

            // Layer 2: AI intents (Always draw these)
            _intentRenderer.Clear();
            _intentRenderer.RenderAll(_state.GetIntents());
            foreach (var kvp in _intentRenderer.GetHighlights())
            {
                gridRenderer.AddHighlight(kvp.Key, kvp.Value);
            }
        } 

        #endregion

        #region Turn Management
	    // Our turn management through the flow controller, manages the turn transitions
            private void BeginPlayerTurn()
        {
            _flowController.StartPlayerTurn();

            _currentActionMode = PlayerActionMode.Move;
            RefreshPlayerHighlights();
        }
        

        private void BeginEnemyTurn()
        {
            gridRenderer.ClearHighlights();
            _flowController.StartEnemyTurn();

            var currentUnit = _flowController.GetCurrentUnit();
            ExecuteEnemyTurn(currentUnit);
        }

        private void EndTurn()
        {
            if (_flowController.CheckVictory())
            {
                EndCombat(true);
                return;
            }

            if (_flowController.CheckDefeat())
            {
                EndCombat(false);
                return;
            }

            _flowController.AdvanceTurn();

            if (_flowController.IsCurrentUnitPlayerControlled())
            {
                BeginPlayerTurn();
            }
            else
            {
                BeginEnemyTurn();
            }
        }

        #endregion

        #region Player Input
	    // Handle all player input in the combat engine
	    // NOTE: considering refactoring this to a seperate PlayerController
        private void HandlePlayerInput()
        {
            if (_flowController.HasPlayerActed()) return;

            var currentUnit = _flowController.GetCurrentUnit();
            if (currentUnit == null || !currentUnit.IsPlayerControlled) return;

            // Mode switching
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetActionMode(PlayerActionMode.Move);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetActionMode(PlayerActionMode.Attack);
            }

            // Action execution based on current mode
            if (Input.GetMouseButtonDown(0))
            {
                // Check if player mouse is over a UI canvas, if so it's not a grid click so ignore.
                if (UnityEngine.EventSystems.EventSystem.current != null && 
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return; 
                }

                var clickedHex = gridRenderer.GetHoveredHex();
                var clickedCell = _grid.GetCell(clickedHex);

                if (clickedCell == null) return;

                if (_currentActionMode == PlayerActionMode.Move)
                {
                    if (clickedCell.CanEnter())
                    {
                        TryPlayerMove(currentUnit, clickedHex);
                    }
                }
                else if (_currentActionMode == PlayerActionMode.Attack)
                {
                    if (clickedCell.Occupant != null && clickedCell.Occupant.Role == UnitRole.Enemy)
                    {
                        TryPlayerAttack(currentUnit, clickedCell.Occupant);
                    }
                }
            }

            // End turn early
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[CombatManager] Player ended turn early");
                _flowController.SetPlayerActed();
                EndTurn();
            }
        }

        private void TryPlayerMove(Unit unit, HexCoordinates destination)
        {
            var moveAction = _actionResolver.CreateMoveAction(unit, destination);

            if (_actionResolver.Execute(moveAction))
            {
                var visual = _state.GetVisual(unit);
                visual?.RefreshPosition();

                _flowController.SetPlayerActed();
                gridRenderer.ClearHighlights();

                Invoke(nameof(EndTurn), 0.3f);
            }
        }

        private void TryPlayerAttack(Unit attacker, Unit target)
        {
            ICombatAction attackAction;

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
                // target name and health
                Debug.Log($"[CombatManager] {target.DisplayName} HP: {target.Stats.currentHealth}/{target.Stats.maxHealth}");


                if (!target.IsAlive)
                {
                    HandleUnitDeath(target);
                }

                _flowController.SetPlayerActed();
                gridRenderer.ClearHighlights();

                Invoke(nameof(EndTurn), 0.3f);
            }
        }

        #endregion

        #region Enemy AI

        private void ExecuteEnemyTurn(Unit enemyUnit)
        {
            if (enemyUnit == null || !enemyUnit.IsAlive)
            {
                EndTurn();
                return;
            }

            var action = _enemyAI.DecideAction(enemyUnit, _state.AllUnits);

            if (action == null)
            {
                Invoke(nameof(EndTurn), 0.5f);
                return;
            }

            if (_actionResolver.Execute(action))
            {
                Unit targetUnit = null;
                bool isAttack = false;

                if (action is MeleeAttackAction melee)
                {
                    targetUnit = melee.Target;
                    isAttack = true;
                }
                else if (action is RangedAttackAction ranged)
                {
                    targetUnit = ranged.Target;
                    isAttack = true;
                }

                if (isAttack && targetUnit != null)
                {
                    Debug.Log($"[CombatManager] {enemyUnit.DisplayName} attacked {targetUnit.DisplayName}");

                    if (!targetUnit.IsAlive)
                    {
                        HandleUnitDeath(targetUnit);
                    }
                }
                else
                {
                    var visual = _state.GetVisual(enemyUnit);
                    visual?.RefreshPosition();
                }
            }

            Invoke(nameof(EndTurn), 0.5f);
        }

        #endregion

        #region Combat Resolution

        private void HandleUnitDeath(Unit unit)
        {
            Debug.Log($"[CombatManager] Unit died: {unit.DisplayName}");

            unit.CurrentCell?.ClearOccupant();

            _flowController.RemoveUnitFromTurnOrder(unit);

            var visual = _state.GetVisual(unit);
            if (visual != null)
            {
                Destroy(visual.gameObject);
                _state.RemoveUnitVisual(unit);
            }
        }

        private void EndCombat(bool victory)
        {
            _flowController.SetCombatState(victory ? CombatState.Victory : CombatState.Defeat);
            Debug.Log($"[CombatManager] Combat ended: {(victory ? "VICTORY" : "DEFEAT")}");

            var transitionData = CombatTransitionData.Instance;
            var result = new CombatResultData(
                victory: victory,
                experience: victory ? victoryExperience : 0,
                enemyName: transitionData?.enemyName ?? "Unknown"
            );

            GameStateManager.Instance.TransitionToExploration(result);
        }

        #endregion

        #region Public API

        public Unit GetCurrentUnit()
        {
            return _flowController?.GetCurrentUnit();
        }

        public HexGrid GetGrid()
        {
            return _grid;
        }

        public ActionResolver GetActionResolver()
        {
            return _actionResolver;
        }

        public IReadOnlyList<Unit> GetAllUnits()
        {
            return _state.AllUnits.AsReadOnly();
        }

        public IReadOnlyList<ActionIntent> GetEnemyIntents()
        {
            return _state.GetIntents();
        }

        // Check if the player has acted this turn
        public bool HasPlayerActed => _flowController != null && _flowController.HasPlayerActed();

        public bool IsPlayerTurn => _state != null && _state.CurrentState == CombatState.PlayerTurn;

        #endregion
    }
}

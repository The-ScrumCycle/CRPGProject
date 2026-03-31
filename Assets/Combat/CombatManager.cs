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

        [Header("Deployment Zones")]
        [SerializeField] private System.Collections.Generic.List<HexCoordinates> playerDeploymentHexes;
        [SerializeField] private System.Collections.Generic.List<HexCoordinates> enemyDeploymentHexes;

        [Header("Sandbox Testing (Direct Scene Boot)")] // allows us to test combat scenarios inside the combat scene
        [SerializeField] private System.Collections.Generic.List<SandboxUnitID> debugPlayerRoster = new System.Collections.Generic.List<SandboxUnitID> { SandboxUnitID.Captain, SandboxUnitID.Warrior, SandboxUnitID.Cleric };
        [SerializeField] private System.Collections.Generic.List<SandboxUnitID> debugEnemyRoster = new System.Collections.Generic.List<SandboxUnitID> { SandboxUnitID.skeleton_ranged, SandboxUnitID.skeleton_melee, SandboxUnitID.Hydra };

        [Header("Prefabs")]
        [SerializeField] private GameObject shoveArrowPrefab;

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
        private HexCoordinates? _lastHoveredHex = null;
        private ActionIntent _currentHoverIntent = null;

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
                // The only two current player "input types" we can handle e.g an actual input and a hover
                HandlePlayerInput();
                HandlePlayerHoverPreview();
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
            _intentRenderer = new CombatIntentRenderer(shoveArrowPrefab);

            SpawnUnits();

            _flowController.InitializeTurnOrder(_state.AllUnits);
            _flowController.GenerateInitialIntents();

            BeginPlayerTurn();

            Debug.Log($"[CombatManager] Combat initialized with {_state.AllUnits.Count} units");
        }

        private void SpawnUnits()
        {
            var transitionData = Game.Core.Transitions.CombatTransitionData.Instance;

            // --- SAFETY FALLBACKS FOR DEPLOYMENT ZONES ---
            if (playerDeploymentHexes == null || playerDeploymentHexes.Count == 0)
            {
                playerDeploymentHexes = new System.Collections.Generic.List<HexCoordinates> 
                { new HexCoordinates(1, 1), new HexCoordinates(1, 2), new HexCoordinates(2, 1) };
            }
            if (enemyDeploymentHexes == null || enemyDeploymentHexes.Count == 0)
            {
                enemyDeploymentHexes = new System.Collections.Generic.List<HexCoordinates> 
                { new HexCoordinates(6, 6), new HexCoordinates(6, 5), new HexCoordinates(5, 6), new HexCoordinates(5, 5) };
            }

            // --- SPAWN PLAYERS (we use sandbox config if no transition data avail) ---
            List<string> playersToSpawn = new List<string>();
            if (transitionData != null && transitionData.ActiveCompanions.Count > 0)
            {
                playersToSpawn = transitionData.ActiveCompanions;
            }
            else
            {
                // Convert Enum to String for the Sandbox
                foreach (var id in debugPlayerRoster) playersToSpawn.Add(id.ToString());
            } 

            for (int i = 0; i < playersToSpawn.Count; i++)
            {
                if (i >= playerDeploymentHexes.Count) break;

                var (playerUnit, playerVisual) = unitFactory.CreatePlayerUnit(playersToSpawn[i]);
                RegisterUnit(playerUnit, playerVisual, playerDeploymentHexes[i]);
            }

            // --- SPAWN ENEMIES (we use sandbox config if no transition data avail) ---
            List<string> enemiesToSpawn = new List<string>();
            if (transitionData != null && transitionData.EncounterEnemies.Count > 0)
            {
                enemiesToSpawn = transitionData.EncounterEnemies;
            }
            else
            {
                // Convert Enum to String for the Sandbox!
                foreach (var id in debugEnemyRoster) enemiesToSpawn.Add(id.ToString());
            } 

            int enemyLevel = transitionData != null ? transitionData.ennemyLevel : 1;

            for (int i = 0; i < enemiesToSpawn.Count; i++)
            {
                if (i >= enemyDeploymentHexes.Count) break;

                string tag = enemiesToSpawn[i];
                string fallbackName = tag.Replace("_", " ").ToUpper();

                var (enemyUnit, enemyVisual) = unitFactory.CreateEnemyUnit(tag, enemyLevel, fallbackName);
                RegisterUnit(enemyUnit, enemyVisual, enemyDeploymentHexes[i]);
            }
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
        private void UpdateUnitWorldUIs(ActionIntent hoverIntent = null)
        {
            var intents = new List<ActionIntent>();
            // get intents from the combat runtime state
            if (_state != null) intents.AddRange(_state.GetIntents());
            if (hoverIntent != null) intents.Add(hoverIntent);

            var hoveredHex = gridRenderer.GetHoveredHex();
            var hoveredCell = _grid.GetCell(hoveredHex);
            Unit hoveredUnit = hoveredCell?.Occupant;

            foreach (var unit in _state.AllUnits)
            {
                if (unit == null || !unit.IsAlive) continue;

                var visual = _state.GetVisual(unit);
                if (visual == null) continue; 

                var worldUI = visual.GetComponentInChildren<UI.UnitWorldUI>();
                if (worldUI == null) continue;

                int incomingDamage = 0;
                bool isSecondaryTarget = false; 

                foreach (var intent in intents)
                {
                    // --- Damage Preview Layer for AOE Attacks ---
                    bool isTargetedByAction = false;
                    
                    if (intent.Action is SplashAttackAction || intent.Action is SweepAttackAction)
                    {
                        // If it's an AoE, check if this unit is standing in the attack zone
                        isTargetedByAction = intent.TargetCells.Contains(unit.Coordinates);
                    }
                    else
                    {
                        // Standard single-target check
                        isTargetedByAction = intent.TargetUnit == unit;
                    }

                    if (isTargetedByAction)
                    {
                        incomingDamage += intent.PredictedDamage;
                    }

                    // Check for Bump Damage
                    if (intent.SecondaryBumpTarget == unit) 
                    {
                        incomingDamage += 10; // BUMP_DAMAGE
                        isSecondaryTarget = true;
                    }
                }

                bool isHovered = (unit == hoveredUnit) || isSecondaryTarget;
                worldUI.UpdateState(unit.Stats.currentHealth, unit.Stats.maxHealth, incomingDamage, isHovered, unit.IsPlayerControlled);
            }
        } 

        // Set the player action mode and refresh highlights accordingly
        public void SetActionMode(PlayerActionMode mode)
        {
            if (_currentActionMode == mode) return; // Ignore if pressing the same key

            _currentActionMode = mode;
            _lastHoveredHex = null; // Force a fresh hover calculation (edge case bug fix)
            _currentHoverIntent = null; // WIPE the old intent from the previous action from UI

            Debug.Log($"[CombatManager] Action mode set to: {_currentActionMode}");
            RefreshPlayerHighlights(); // Pass no intent, clears the screen until mouse updates
        }

        // Build and push all active highlights to the renderer.
        // Player move highlights first, then AI intents layered on top via priority of the action intents.
        private void RefreshPlayerHighlights(ActionIntent hoverIntent = null)
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

                    // Also show attackable enemies so the player knows they're already in range
                    var attackableNow = _flowController.GetValidAttackTargets(currentUnit);
                    foreach (var coord in attackableNow)
                        gridRenderer.AddHighlight(coord, HighlightType.PlayerAttack);
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
            var allIntentsToRender = new List<ActionIntent>(_state.GetIntents());
            if (hoverIntent != null) allIntentsToRender.Add(hoverIntent); // Generate "Phantom" action intent for showing enemy health bar

            _intentRenderer.RenderAll(allIntentsToRender);
            foreach (var kvp in _intentRenderer.GetHighlights())
            {
                gridRenderer.AddHighlight(kvp.Key, kvp.Value);
            }

            // Layer 3: Damage indicators and Shove arrow indicator
            UpdateUnitWorldUIs(hoverIntent);
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
            _currentHoverIntent = null;
            _intentRenderer.Clear();
            gridRenderer.ClearHighlights();
            UpdateUnitWorldUIs();

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
            var currentUnit = _flowController.GetCurrentUnit();
            // If the currently selected unit has already acted or is dead, do not process input
            if (currentUnit == null || !currentUnit.IsPlayerControlled || _flowController.HasUnitActed(currentUnit)) return;

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
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return; 

                var clickedHex = gridRenderer.GetHoveredHex();
                var clickedCell = _grid.GetCell(clickedHex);
                if (clickedCell == null) return;

                // Swap unit when player clicks on a unit on grid
                if (clickedCell.Occupant != null && clickedCell.Occupant.IsPlayerControlled && clickedCell.Occupant.IsAlive)
                {
                    if (!_flowController.HasUnitActed(clickedCell.Occupant))
                    {
                        _flowController.SelectPlayerUnit(clickedCell.Occupant);
                        SetActionMode(PlayerActionMode.Move);
                        return; // Successfully swapped, abort action execution
                    }
                }

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
                // Mark all alive players as acted to force turn end
                foreach(var u in _state.AllUnits) if (u.IsPlayerControlled) _flowController.MarkUnitActed(u);
                EndTurn();
            }
        }

        // Handles a player hovering over something with his mouse causing information to be displayed
        // Currently this is only for displaying health bar but we can add future hover info
        private void HandlePlayerHoverPreview()
        {
            var currentHex = gridRenderer.GetHoveredHex();
            
            // Only rebuild if the mouse moved to a new hex
            if (_lastHoveredHex == currentHex) return;
            _lastHoveredHex = currentHex;

            _currentHoverIntent = null;
            var currentUnit = _flowController.GetCurrentUnit();

            // If mouse is not on UI, it's our turn, we have a unit, AND that specific unit hasn't acted
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() && 
                currentUnit != null && currentUnit.IsPlayerControlled && !_flowController.HasUnitActed(currentUnit))
            {
                var cell = _grid.GetCell(currentHex);
                
                // Create a "Phantom" Intent for the UI to read
                if (cell != null && _currentActionMode == PlayerActionMode.Attack && cell.Occupant != null && cell.Occupant.Role == UnitRole.Enemy)
                {
                    int distance = _grid.GetDistance(currentUnit.Coordinates, currentHex);
                    ICombatAction mockAction = null;
                    
                    if (distance == 1) mockAction = _actionResolver.CreateMeleeAttack(currentUnit, cell.Occupant);
                    else if (distance <= currentUnit.Stats.attackRange) mockAction = _actionResolver.CreateRangedAttack(currentUnit, cell.Occupant);

                    if (mockAction != null && _actionResolver.Validate(mockAction))
                    {
                        _currentHoverIntent = _actionResolver.Preview(mockAction);
                    }
                }
            }

            // Refresh highlights passing in the new phantom intent
            RefreshPlayerHighlights(_currentHoverIntent);
        } 

        private void TryPlayerMove(Unit unit, HexCoordinates destination)
        {
            var moveAction = _actionResolver.CreateMoveAction(unit, destination);
            if (_actionResolver.Execute(moveAction))
            {
                var currentUnit = _flowController.GetCurrentUnit();
                _flowController.MarkUnitActed(currentUnit);
                
                // Clear UI and Highlights immediately as player action is done
                _currentHoverIntent = null;
                _intentRenderer.Clear();
                gridRenderer.ClearHighlights();
                UpdateUnitWorldUIs();
                RefreshAllUnitVisuals();
                
                // End turn if player played all unit turns OR go to next unit
                if (_flowController.HaveAllPlayerUnitsActed())
                {
                    Invoke(nameof(EndTurn), 0.3f);
                }
                else
                {
                    var nextUnit = _flowController.GetNextAvailablePlayerUnit();
                    if (nextUnit != null) _flowController.SelectPlayerUnit(nextUnit);
                    
                    // Delay the UI refresh slightly so visual slide finishes before drawing new grids
                    Invoke(nameof(ResetModeToMove), 0.3f); 
                }
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
                Debug.Log($"[CombatManager] {target.DisplayName} HP: {target.Stats.currentHealth}/{target.Stats.maxHealth}");
                
                SweepForDeaths();

                var currentUnit = _flowController.GetCurrentUnit();
                _flowController.MarkUnitActed(currentUnit);
                
                // Clear UI and Highlights immediately
                _currentHoverIntent = null;
                _intentRenderer.Clear();
                gridRenderer.ClearHighlights();
                UpdateUnitWorldUIs();

                // updating enemy position (shove)
                RefreshAllUnitVisuals();

                // End turn if player played all unit turns OR go to next unit
                if (_flowController.HaveAllPlayerUnitsActed())
                {
                    Invoke(nameof(EndTurn), 0.3f);
                }
                else
                {
                    var nextUnit = _flowController.GetNextAvailablePlayerUnit();
                    if (nextUnit != null) _flowController.SelectPlayerUnit(nextUnit);
                    
                    // Delay the UI refresh slightly so visual slide finishes before drawing new grids
                    Invoke(nameof(ResetModeToMove), 0.3f); 
                }
            }
        }

        private void ResetModeToMove()
        {
            SetActionMode(PlayerActionMode.Move);
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

            // 1. Find the EXACT intent the AI locked in during the planning phase
            ActionIntent lockedIntent = null;
            foreach (var intent in _state.GetIntents())
            {
                if (intent.Actor == enemyUnit)
                {
                    lockedIntent = intent;
                    break;
                }
            }

            // 2. Execute the locked intent IF it is still valid, e.g don't move into a hexagon that contains a unit now (through shoving or player move)
            bool targetStillInHex = true;
            if (lockedIntent != null && lockedIntent.TargetUnit != null)
            {
                if (lockedIntent != null && lockedIntent.TargetUnit != null && lockedIntent.TargetCells.Count > 0)
                {
                    // We check against TargetCells[0] because single-target attacks yield exactly one hex, in the future if we add AOE we need to totally refactor
                    if (lockedIntent.TargetUnit.Coordinates != lockedIntent.TargetCells[0])
                    {
                        targetStillInHex = false; // Player dodged attack
                    }
                }
            }

            // Execute the locked intent IF it is valid AND the player didn't doge the attack e.g move away
            if (lockedIntent != null && targetStillInHex && _actionResolver.Validate(lockedIntent.Action))
            {
                if (_actionResolver.Execute(lockedIntent.Action))
                {
                    bool isAttack = lockedIntent.Action is MeleeAttackAction || lockedIntent.Action is RangedAttackAction;
                    if (isAttack)
                    {
                        Debug.Log($"[CombatManager] {enemyUnit.DisplayName} executed telegraphed attack!");
                        SweepForDeaths(); 
                    }
                }
            }
            else
            {
                if (lockedIntent == null)
                    Debug.Log($"[AI Skip] {enemyUnit.DisplayName} ({enemyUnit.AIBehavior}) has nothing to do — no action planned");
                else if (!targetStillInHex)
                    Debug.Log($"[AI Dodged] {enemyUnit.DisplayName}'s {lockedIntent.Action.GetType().Name} on {lockedIntent.TargetUnit?.DisplayName} failed — target moved away from expected hex");
                else
                    Debug.Log($"[AI Blocked] {enemyUnit.DisplayName}'s {lockedIntent.Action.GetType().Name} failed validation — destination may be occupied or target out of range");
            }

            // 3. Force visual refresh so any shoved units physically slide to their new hex
            foreach (var unit in _state.AllUnits)
            {
                _state.GetVisual(unit)?.RefreshPosition();
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

        // Ensure position tracking is up to date
        private void RefreshAllUnitVisuals()
        {
            foreach (var unit in _state.AllUnits)
            {
                var visual = _state.GetVisual(unit);
                visual?.RefreshPosition();
            }
        }

        private void SweepForDeaths()
        {
            // Copy unit list to safely remove items while iterating
            var unitsToCheck = new List<Unit>(_state.AllUnits);
            foreach (var u in unitsToCheck)
            {
                if (!u.IsAlive)
                {
                    HandleUnitDeath(u);
                }
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

        public void TrySelectPlayerUnit(Unit unit)
        {
            if (unit == null || !unit.IsPlayerControlled || !unit.IsAlive) return;
            if (_flowController.HasUnitActed(unit)) return; // can't select exhausted units

            _flowController.SelectPlayerUnit(unit);
            SetActionMode(PlayerActionMode.Move);
            
            // Force visuals to update immediately for new selected unit
            RefreshPlayerHighlights();
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
        public bool HasUnitActed(Unit unit) => _flowController != null && _flowController.HasUnitActed(unit);

        public bool IsPlayerTurn => _state != null && _state.CurrentState == CombatState.PlayerTurn;
        #endregion
    }
}

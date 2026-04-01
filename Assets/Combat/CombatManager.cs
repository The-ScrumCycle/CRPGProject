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
        Attack,
        SecondaryAction
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
                    
                    // Check if player is in AoE damage zone or targetted by an attack
                    if (intent.TargetCells != null && intent.TargetCells.Count > 0)
                    {
                        foreach (var cellCoord in intent.TargetCells)
                        {
                            if (_grid.GetDistance(cellCoord, unit.Coordinates) == 0)
                            {
                                isTargetedByAction = true;
                                break;
                            }
                        }
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

                // show health bar constantly while taking damage
                bool isHovered = (unit == hoveredUnit) || isSecondaryTarget || (incomingDamage > 0);
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

            // Optional 3rd action player units can have
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetActionMode(PlayerActionMode.SecondaryAction);

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
                    // Only allow swap if player has move action selected, so they don't swap while attempting to perform an action on their units
                    if (!_flowController.HasUnitActed(clickedCell.Occupant) && _currentActionMode == PlayerActionMode.Move)
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
                else if (_currentActionMode == PlayerActionMode.Attack || _currentActionMode == PlayerActionMode.SecondaryAction)
                {
                    TryPlayerAbility(currentUnit, clickedCell);
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
                
                // Create a dynamic Intent based on the active loadout slot
                if (cell != null && (_currentActionMode == PlayerActionMode.Attack || _currentActionMode == PlayerActionMode.SecondaryAction))
                {
                    int actionIndex = _currentActionMode == PlayerActionMode.Attack ? 0 : 1;
                    
                    if (actionIndex < currentUnit.AvailableActions.Count)
                    {
                        CombatActionType abilityType = currentUnit.AvailableActions[actionIndex];
                        ICombatAction mockAction = CreateActionFromType(abilityType, currentUnit, cell);

                        if (mockAction != null && _actionResolver.Validate(mockAction))
                        {
                            _currentHoverIntent = _actionResolver.Preview(mockAction);
                        }
                    }
                }
            }

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

        
        private void TryPlayerAbility(Unit caster, HexCell targetCell)
        {
            int actionIndex = _currentActionMode == PlayerActionMode.Attack ? 0 : 1;
            if (actionIndex >= caster.AvailableActions.Count) return;

            CombatActionType abilityType = caster.AvailableActions[actionIndex];
            ICombatAction action = CreateActionFromType(abilityType, caster, targetCell);

            // If the factory failed to build it, or the action's specific IsValid checks fail, abort
            if (action == null || !_actionResolver.Validate(action)) 
            {
                Debug.Log($"[CombatManager] Invalid target for {abilityType}");
                return;
            }

            if (_actionResolver.Execute(action))
            {
                Debug.Log($"[CombatManager] {caster.DisplayName} used {abilityType} on {targetCell.Occupant?.DisplayName ?? "Empty Hex"}");
                
                SweepForDeaths();

                var currentUnit = _flowController.GetCurrentUnit();
                _flowController.MarkUnitActed(currentUnit);
                
                _currentHoverIntent = null;
                _intentRenderer.Clear();
                gridRenderer.ClearHighlights();
                UpdateUnitWorldUIs();
                RefreshAllUnitVisuals();

                if (_flowController.HaveAllPlayerUnitsActed())
                {
                    Invoke(nameof(EndTurn), 0.3f);
                }
                else
                {
                    var nextUnit = _flowController.GetNextAvailablePlayerUnit();
                    if (nextUnit != null) _flowController.SelectPlayerUnit(nextUnit);
                    
                    Invoke(nameof(ResetModeToMove), 0.3f); 
                }
            }
        }

        private ICombatAction CreateActionFromType(CombatActionType type, Unit actor, HexCell targetCell)
        {
            // Most actions currently require an occupant to target, except specific AoEs
            if (targetCell.Occupant == null && type != CombatActionType.SplashAttack && type != CombatActionType.SweepAttack) 
                return null; 

            switch (type)
            {
                case CombatActionType.MeleeAttack:
                    return _actionResolver.CreateMeleeAttack(actor, targetCell.Occupant);
                case CombatActionType.RangedAttack:
                    return _actionResolver.CreateRangedAttack(actor, targetCell.Occupant);
                case CombatActionType.HeavyMeleeAttack:
                    return _actionResolver.CreateHeavyMeleeAttack(actor, targetCell);
                case CombatActionType.PullAlly:
                    return _actionResolver.CreatePull(actor, targetCell);
                case CombatActionType.RangedHeal:
                    return _actionResolver.CreateRangedHeal(actor, targetCell.Occupant);
                default:
                    return null;
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

            // 2. Execute the locked intent. (The old 'targetStillInHex' abort has been eradicated).
            // The action will always hit the locked hex if the player dodged, it hits empty space.
            if (lockedIntent != null && _actionResolver.Validate(lockedIntent.Action))
            {
                if (_actionResolver.Execute(lockedIntent.Action))
                {
                    Debug.Log($"[CombatManager] {enemyUnit.DisplayName} executed telegraphed action: {lockedIntent.Action.GetType().Name}");
                    SweepForDeaths(); 
                }
            }
            else
            {
                if (lockedIntent == null)
                    Debug.Log($"[AI Skip] {enemyUnit.DisplayName} ({enemyUnit.AIBehavior}) has nothing to do — no action planned");
                else
                    Debug.Log($"[AI Blocked] {enemyUnit.DisplayName}'s {lockedIntent.Action.GetType().Name} failed validation — target out of range");
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

            if(victory)
            {
                GameStateManager.Instance.TransitionToExploration(result);
            }


            else
            {
                GameStateManager.Instance.TransitionToGameOver();
            }
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

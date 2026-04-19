using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Game.Core;
using Game.Core.Transitions;
using Game.Combat.Grid;
using Game.Combat.Turn;
using Game.Combat.Units;
using Game.Combat.Actions;
using Game.Combat.UI;
using System;
using UnityEngine.Rendering;

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
        [SerializeField] private int gridWidth = 9;
        [SerializeField] private int gridHeight = 9;

        [Header("Environments")]
        [SerializeField] private CombatEnvironment[] environments;
        [SerializeField] private BoardRenderer boardRenderer;
        [SerializeField] private Material backgroundMat;
        [SerializeField] private Volume globalVolume;

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

        private List<Unit> outlinedUnits;

        int currentEnv = 0;

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
            if (Input.GetKeyDown(KeyCode.E))
            {
                currentEnv = (currentEnv+1)%environments.Length;
                SetEnvironment(environments[currentEnv]);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                _intentRenderer._renderArrows = false;
            }

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

            outlinedUnits = new List<Unit>();

            _grid = new HexGrid(gridWidth, gridHeight);

            if (gridRenderer != null)
            {
                gridRenderer.Initialize(_grid);
            }

            InitializeEnvironment();

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

        private void InitializeEnvironment()
        {
            try
            {
                int envIndex = 0;
                if (CombatTransitionData.Instance != null)
                {
                    envIndex = (int)CombatTransitionData.Instance.EnvironmentType;
                }
                if (environments != null && environments.Length > envIndex)
                {
                    currentEnv = envIndex;
                    SetEnvironment(environments[currentEnv]);
                }
                else if (environments != null && environments.Length > 0)
                {
                    currentEnv = 0;
                    SetEnvironment(environments[currentEnv]);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Failed to load environment: " + e.Message);
            }
        }

        private void SpawnUnits()
        {
            var transitionData = Game.Core.Transitions.CombatTransitionData.Instance;

            // --- SPAWN PLAYERS ---
            List<string> playersToSpawn = new List<string>();
            if (transitionData != null && transitionData.ActiveCompanions.Count > 0)
            {
                playersToSpawn = transitionData.ActiveCompanions;
            }
            else
            {
                foreach (var id in debugPlayerRoster) playersToSpawn.Add(id.ToString());
            } 

            foreach (var playerId in playersToSpawn)
            {
                var result = unitFactory.CreatePlayerUnit(playerId);
                if (result.unit != null)
                {
                    // VISUAL BOTTOM-LEFT
                    HexCoordinates coords = GetEmptyCellInBounds(0, (gridWidth / 2) - 1, 0, (gridHeight / 2) - 1);
                    RegisterUnit(result.unit, result.visual, coords);
                }
            }

            // --- SPAWN ENEMIES & CRYSTALS ---
            List<string> enemiesToSpawn = new List<string>();
            if (transitionData != null && transitionData.EncounterEnemies.Count > 0)
            {
                enemiesToSpawn = transitionData.EncounterEnemies;
            }
            else
            {
                foreach (var id in debugEnemyRoster) enemiesToSpawn.Add(id.ToString());
            } 

            int enemyLevel = transitionData != null ? transitionData.ennemyLevel : 1;
            int crystalCount = 0;

            foreach (var enemyTag in enemiesToSpawn)
            {
                string fallbackName = enemyTag.Replace("_", " ").ToUpper();
                var result = unitFactory.CreateEnemyUnit(enemyTag, enemyLevel, fallbackName);
                
                if (result.unit != null)
                {
                    HexCoordinates coords;

                    if (result.unit.AIBehavior == AIBehavior.Crystal)
                    {
                        // Spontaneous Crystal Spawning exactly mapped to your visual camera
                        coords = crystalCount switch
                        {
                            // 0. Visual Top-Left
                            0 => GetEmptyCellInBounds(0, (gridWidth / 3) - 1, gridHeight * 2 / 3, gridHeight - 1), 
                            
                            // 1. Visual Bottom-Right
                            1 => GetEmptyCellInBounds(gridWidth * 2 / 3, gridWidth - 1, 0, (gridHeight / 3) - 1), 
                            
                            // 2. Visual Bottom-Left
                            2 => GetEmptyCellInBounds(0, (gridWidth / 3) - 1, 0, (gridHeight / 3) - 1), 
                            
                            // 3. Visual Top-Right / Center-Right
                            3 => GetEmptyCellInBounds(gridWidth / 3, gridWidth - 1, gridHeight / 3, gridHeight - 1), 
                            
                            // Fallback
                            _ => GetAnyEmptyCell() 
                        };
                        crystalCount++;
                    }
                    else
                    {
                        // Enemies -> VISUAL TOP-RIGHT
                        coords = GetEmptyCellInBounds(gridWidth / 2, gridWidth - 1, gridHeight / 2, gridHeight - 1);
                    }

                    RegisterUnit(result.unit, result.visual, coords);
                }
            }
        } 

        // Finds a random empty cell within a specific coordinate boundary (Quadrant)
        private HexCoordinates GetEmptyCellInBounds(int minQ, int maxQ, int minR, int maxR)
        {
            List<HexCoordinates> validCells = new List<HexCoordinates>();
            
            for (int q = Mathf.Max(0, minQ); q <= Mathf.Min(gridWidth - 1, maxQ); q++)
            {
                for (int r = Mathf.Max(0, minR); r <= Mathf.Min(gridHeight - 1, maxR); r++)
                {
                    var hex = new HexCoordinates(q, r);
                    var cell = _grid.GetCell(hex);
                    
                    // Crucial check: ensures units NEVER spawn on top of each other
                    if (cell != null && cell.Occupant == null)
                    {
                        validCells.Add(hex);
                    }
                }
            }

            if (validCells.Count > 0)
                return validCells[UnityEngine.Random.Range(0, validCells.Count)];

            // Fallback if the quadrant is completely full
            return GetAnyEmptyCell();
        }

        // Absolute fallback to guarantee the unit gets placed somewhere on the board
        private HexCoordinates GetAnyEmptyCell()
        {
            foreach (var cell in _grid.GetAllCells())
            {
                if (cell.Occupant == null) return cell.Coordinates;
            }
            return new HexCoordinates(0, 0); 
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

        #region Environment Management

        private void SetEnvironment(CombatEnvironment env)
        {
            if (boardRenderer != null)
            {
                boardRenderer.SetBoard(env._boardMat);
                boardRenderer.SetBorder(env._borderMat);
            }

            if (gridRenderer != null)
            {
                
            }

            if (backgroundMat != null) RenderSettings.skybox = env._backgroundMat;

            if (globalVolume != null) globalVolume.profile = env._postProcessingProfile;
        }

        #endregion

        #region Highlight Management
        private void UpdateUnitWorldUIs(ActionIntent hoverIntent = null)
        {
            var intents = new List<ActionIntent>();
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
                    
                    if (intent.TargetCells != null && intent.TargetCells.Count > 0)
                    {
                        // Safely loop through the target cells instead of relying on missing LINQ extensions
                        foreach (var cellCoord in intent.TargetCells)
                        {
                            if (cellCoord == unit.Coordinates)
                            {
                                isTargetedByAction = true;
                                break;
                            }
                        }
                    } 
                    else
                    {
                        isTargetedByAction = intent.TargetUnit == unit;
                    }

                    if (isTargetedByAction)
                    {
                        incomingDamage += intent.PredictedDamage;
                    }

                    if (intent.SecondaryBumpTarget == unit) 
                    {
                        incomingDamage += 10; // BUMP_DAMAGE
                        isSecondaryTarget = true;
                    }
                }

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

        private void RefreshEnemyIntents()
        {
            if (_state == null) return;
            var currentIntents = new List<ActionIntent>(_state.GetIntents());
            
            _state.ClearIntents(); 
            foreach (var intent in currentIntents)
            {
                if (intent.Actor != null && intent.Actor.IsAlive)
                {
                    var freshIntent = _actionResolver.Preview(intent.Action);
                    _state.AddIntent(freshIntent);
                }
            }
        }

        // Build and push all active highlights to the renderer.
        // Player move highlights first, then AI intents layered on top via priority of the action intents.
        private void RefreshPlayerHighlights(ActionIntent hoverIntent = null)
        {
            gridRenderer.ClearHighlights();
            var currentUnit = _flowController.GetCurrentUnit();

            ClearOutlines();

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
                    var validTargets = _flowController.GetCellsInAttackRange(currentUnit);
                    foreach (var coord in validTargets)
                        gridRenderer.AddHighlight(coord, HighlightType.PlayerAttack);
                }

                _state.GetVisual(currentUnit).SetHighlight(true);
                outlinedUnits.Add(currentUnit);
            }

            // Layer 2: AI intents (Always draw these)
            _intentRenderer.Clear();
            var allIntentsToRender = new List<ActionIntent>(_state.GetIntents());
            if (hoverIntent != null) allIntentsToRender.Add(hoverIntent); // Generate "Phantom" action intent for showing enemy health bar

            Dictionary<ActionIntent, UnitVisual> intentsWithVisuals = new Dictionary<ActionIntent, UnitVisual>();
            foreach (ActionIntent intent in allIntentsToRender)
            {
                intentsWithVisuals.Add(intent, _state.GetVisual(intent.Actor));
            }

            _intentRenderer.RenderAll(intentsWithVisuals);
            foreach (var kvp in _intentRenderer.GetHighlights())
            {
                gridRenderer.AddHighlight(kvp.Key, kvp.Value);
            }

            // Layer 3: Damage indicators and Shove arrow indicator
            UpdateUnitWorldUIs(hoverIntent);
        }

        private void ClearOutlines()
        {
            foreach (Unit unit in outlinedUnits)
            {
                _state.GetVisual(unit).SetHighlight(false);
            }
            outlinedUnits.Clear();
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
            if (Input.GetKeyDown(KeyCode.Alpha1) && !_flowController.HasUnitMoved(currentUnit)) SetActionMode(PlayerActionMode.Move); 
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetActionMode(PlayerActionMode.Attack);
            // Optional 3rd action player units can have
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetActionMode(PlayerActionMode.SecondaryAction);
            // Button to end unit's own turn manually
            if (Input.GetKeyDown(KeyCode.X)) SkipCurrentUnitTurn();

            // Action execution based on current mode
            if (Input.GetMouseButtonDown(0))
            {
                // Check if player mouse is over a UI canvas, if so it's not a grid click so ignore.
                if (UnityEngine.EventSystems.EventSystem.current != null && 
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return; 

                var clickedHex = gridRenderer.GetHoveredHex();
                var clickedCell = _grid.GetCell(clickedHex);
                if (clickedCell == null) return;

                // Prevent Heal/Friendly Spells from swapping characters
                bool isExecutingFriendlyAction = false;
                if (_currentActionMode == PlayerActionMode.SecondaryAction && currentUnit.AvailableActions.Count > 1)
                {
                    var action = CreateActionFromType(currentUnit.AvailableActions[1], currentUnit, clickedCell);
                    if (action != null && action.IsValid(_grid)) isExecutingFriendlyAction = true;
                }
                else if (_currentActionMode == PlayerActionMode.Attack && currentUnit.AvailableActions.Count > 0)
                {
                    var action = CreateActionFromType(currentUnit.AvailableActions[0], currentUnit, clickedCell);
                    if (action != null && action.IsValid(_grid)) isExecutingFriendlyAction = true;
                }

                // Swap unit when player clicks on a unit on grid (Bypassed if aiming a valid heal)
                if (!isExecutingFriendlyAction && clickedCell.Occupant != null && clickedCell.Occupant.IsPlayerControlled && clickedCell.Occupant.IsAlive)
                {
                    if (!_flowController.HasUnitActed(clickedCell.Occupant))
                    {
                        _flowController.SelectPlayerUnit(clickedCell.Occupant);
                        RefreshActionStateForCurrentUnit(); // Set them to Move or Attack appropriately
                        return; 
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

        /* Unit Pinned State -
        //  1) If player is in a targeted hex his "move" action becomes "dodge" as they're in pinned state which exhausts the unit's turn
        //  2) If player is not in a targeted hex the player gets to "move" and then play a secondary/thirdary action e.g attack.
        */
        public bool IsUnitPinned(Unit unit)
        {
            if (unit == null) return false;

            foreach (var intent in _state.GetIntents())
            {
                // Check direct targeting
                if (intent.TargetUnit == unit) return true;
                
                // Check AoE Danger Zones
                if (intent.TargetCells != null)
                {
                    foreach (var cell in intent.TargetCells)
                    {
                        if (cell == unit.Coordinates) return true;
                    }
                }

                // Check physical collision bump paths
                if (intent.SecondaryBumpTarget == unit) return true;
            }
            return false;
        }

        private void TryPlayerMove(Unit unit, HexCoordinates destination)
        {
            // CHECK PINNED STATUS BEFORE THEY MOVE (refer to function IsUnitPinned)
            bool wasPinned = IsUnitPinned(unit); 

            var moveAction = _actionResolver.CreateMoveAction(unit, destination);
            if (_actionResolver.Execute(moveAction))
            {
                SweepForDeaths();
                RefreshEnemyIntents();

                if (wasPinned)
                {
                    // DODGE: Consumes the Unit's entire turn to move
                    _flowController.MarkUnitActed(unit);
                    Debug.Log($"[CombatManager] {unit.DisplayName} Dodged! Turn ended.");
                }
                else
                {
                    // FREE REPOSITION: Consumes move, leaves attack actions intact
                    _flowController.MarkUnitMoved(unit);
                    SetActionMode(PlayerActionMode.Attack);
                    Debug.Log($"[CombatManager] {unit.DisplayName} repositioned. Can still act.");
                }
                
                // Handle visual and intent layer
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
                    
                    Invoke(nameof(RefreshActionStateForCurrentUnit), 0.3f);
                }
            }
        } 

        // Allow player's to deliberately skip their unit's turn
        public void SkipCurrentUnitTurn()
        {
            var unit = _flowController.GetCurrentUnit();
            if (unit == null || !unit.IsPlayerControlled || _flowController.HasUnitActed(unit)) return;

            Debug.Log($"[CombatManager] {unit.DisplayName} waits. Turn ended.");
            
            // Exhaust their turn
            _flowController.MarkUnitActed(unit);
            
            // Clean up the UI intent layer
            _currentHoverIntent = null;
            _intentRenderer.Clear();
            gridRenderer.ClearHighlights();
            UpdateUnitWorldUIs();
            RefreshAllUnitVisuals();

            // Advance the combat flow
            if (_flowController.HaveAllPlayerUnitsActed())
            {
                Invoke(nameof(EndTurn), 0.3f);
            }
            else
            {
                var nextUnit = _flowController.GetNextAvailablePlayerUnit();
                if (nextUnit != null) _flowController.SelectPlayerUnit(nextUnit);
                
                Invoke(nameof(RefreshActionStateForCurrentUnit), 0.3f); 
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
                RefreshEnemyIntents();

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
                    
                    Invoke(nameof(RefreshActionStateForCurrentUnit), 0.3f);
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
                    return _actionResolver.CreateMeleeAttack(actor, targetCell);
                case CombatActionType.RangedAttack:
                    return _actionResolver.CreateRangedAttack(actor, targetCell);
                case CombatActionType.HeavyMeleeAttack:
                    return _actionResolver.CreateHeavyMeleeAttack(actor, targetCell);
                case CombatActionType.PullAlly:
                    return _actionResolver.CreatePull(actor, targetCell);
                case CombatActionType.RangedHeal:
                    return _actionResolver.CreateRangedHeal(actor, targetCell);
                default:
                    return null;
            }
        }

        private void RefreshActionStateForCurrentUnit()
        {
            var unit = _flowController.GetCurrentUnit();
            if (unit != null && !_flowController.HasUnitMoved(unit))
                SetActionMode(PlayerActionMode.Move);
            else if (unit != null)
                SetActionMode(PlayerActionMode.Attack);
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

            // 3. Handle update of unit visual based on action


            // 4. Force visual refresh so any shoved units physically slide to their new hex
            foreach (var unit in _state.AllUnits)
            {
                _state.GetVisual(unit)?.RefreshPosition();
            }

            Invoke(nameof(EndTurn), 0.5f);
        } 

        // Checks the runtime state to see if any Crystals are still alive
        public bool AreCrystalsAlive()
        {
            if (_state == null) return false;
            foreach (var unit in _state.AllUnits)
            {
                if (unit.IsAlive && unit.AIBehavior == AIBehavior.Crystal)
                {
                    return true;
                }
            }
            return false;
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
                visual.Die();
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

        // Shift enemy AI's attack intent per unit offset 
        public void ShiftUnitIntent(Unit unit, HexCoordinates offset)
        {
            if (unit == null || (offset.q == 0 && offset.r == 0)) return;

            foreach (var intent in _state.GetIntents())
            {
                // If the unit that was pushed has a locked-in attack, shift it.
                if (intent.Actor == unit)
                {
                    intent.Shift(offset, _grid);
                    Debug.Log($"[Physics] Shifted {unit.DisplayName}'s telegraphed aim by {offset.q}, {offset.r}");
                    break;
                }
            }
        }

        // Check if the player has acted / moved this turn
        public bool HasUnitActed(Unit unit) => _flowController != null && _flowController.HasUnitActed(unit);
        public bool HasUnitMoved(Unit unit) => _flowController != null && _flowController.HasUnitMoved(unit);

        public bool IsPlayerTurn => _state != null && _state.CurrentState == CombatState.PlayerTurn;
        #endregion
    }
}

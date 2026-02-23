using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core.Transitions;
using Game.Combat;

namespace Game.Core
{
    /// <summary>
    /// Singleton manager controlling game state transition from Exploration to Combat (as of 15 feb.)
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private CombatTransitionData combatTransitionData;

        [Header("Scene Names")]
        [SerializeField] private string explorationSceneName = "Exploration";
        [SerializeField] private string combatSceneName = "CombatScene";

        public GameState CurrentState { get; private set; } = GameState.Exploration;

        // Stores the result of the last combat
        public CombatResultData LastCombatResult { get; private set; }
        private bool _isReturningFromCombat = false;

        private PlayerController _playerController;
        private GameObject _player;
        private GameObject _mainCamera;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Start()
        {
            CacheExplorationReferences();
        }

        private void CacheExplorationReferences()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            _playerController = _player?.GetComponent<PlayerController>();
        }

        private void SetState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            Debug.Log($"[GameStateManager] State changed to: {CurrentState}");
        }

        public GameState GetCurrentState() => CurrentState;

        // Transition from Exploration to Combat.
        // Called by MonsterAI when enemy contacts player.
        public void TransitionToCombat(GameObject enemy)
        {
            if (CurrentState != GameState.Exploration)
            {
                Debug.LogWarning("[GameStateManager] Cannot transition to combat - not in exploration state");
                return;
            }

            SetState(GameState.Combat);

            // Save exploration state
            combatTransitionData.SaveTransitionData(enemy);

            // Disable player agent before scene transition
            if (_playerController != null && _playerController.agent != null)
            {
                _playerController.agent.enabled = false;
            }

            // Mark enemy for destruction on return
            EnnemiesState.Instance?.SetDeadEnnemy(enemy);

            // Load combat scene
            SceneManager.LoadScene(combatSceneName);
        }

        // Transitions from Combat back to Exploration.
        // Called by CombatManager when combat ends.
        public void TransitionToExploration(CombatResultData result)
        {
            if (CurrentState != GameState.Combat)
            {
                Debug.LogWarning("[GameStateManager] Cannot transition to exploration -> not in combat state");
                return;
            }

            LastCombatResult = result;
            _isReturningFromCombat = true; // set combat return flag for state management

            SetState(GameState.Exploration);
            SceneManager.LoadScene(explorationSceneName);
        }

        // Legacy overload for backward compatibility with previous version of combat transitioning
	    // Feel free to remove this and refactor legacy code in Exploration to use new systems 
        public void TransitionToExploration()
        {
            TransitionToExploration(new CombatResultData(true, 0, combatTransitionData.enemyName));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // only restore scene if we're exiting from combat, not initial game load.
            if (scene.name == explorationSceneName && _isReturningFromCombat)
            {
                RestoreExplorationState();
                _isReturningFromCombat = false; // reset combat flag
            }
        }

        private void RestoreExplorationState()
        {
            // Re-cache references after scene load
            CacheExplorationReferences();

            if (_playerController != null && _playerController.agent != null)
            {
                _playerController.agent.enabled = true;
                _playerController.agent.Warp(combatTransitionData.playerPosition);
            }

            if (_player != null)
            {
                _player.transform.rotation = combatTransitionData.playerRotation;
            }

            if (_mainCamera != null)
            {
                _mainCamera.transform.position = combatTransitionData.cameraPosition;
                _mainCamera.transform.rotation = combatTransitionData.cameraRotation;
            }

            combatTransitionData.ClearCache();
    }
}
}

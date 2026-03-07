using UnityEngine;

namespace Game.Core.Transitions
{
    public enum EnvironmentType { Default, Desert, Forest }

    /// <summary>
    /// Stores data needed to transition between Exploration and Combat.
    /// Persists across scene loads.
    /// </summary>
    public class CombatTransitionData : MonoBehaviour
    {
        public static CombatTransitionData Instance { get; private set; }

        [Header("Exploration Return Data")]
        public Vector3 playerPosition;
        public Quaternion playerRotation;
        public Vector3 cameraPosition;
        public Quaternion cameraRotation;

        [Header("Enemy Data")]
        public string enemyTag;
        public string enemyName;

        [Header("Environment Data")]
        public EnvironmentType EnvironmentType;

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
        }

        void Start()
        {
            CacheReferences();
        }

        private void CacheReferences()
        {
            if (_player == null)
                _player = GameObject.FindGameObjectWithTag("Player");
            if (_mainCamera == null)
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        // Called before transitioning to combat to save exploration state.
        public void SaveTransitionData(GameObject enemy)
        {
            CacheReferences();

            if (_player != null)
            {
                playerPosition = _player.transform.position;
                playerRotation = _player.transform.rotation;
            }

            if (_mainCamera != null)
            {
                cameraPosition = _mainCamera.transform.position;
                cameraRotation = _mainCamera.transform.rotation;
            }

            if (enemy != null)
            {
                enemyTag = enemy.tag;
                enemyName = enemy.name;
            }

            EnvironmentType = EnvironmentType.Default;
        }

        // Clears cached references when returning to exploration.
        public void ClearCache()
        {
            _player = null;
            _mainCamera = null;
        }
    }
}

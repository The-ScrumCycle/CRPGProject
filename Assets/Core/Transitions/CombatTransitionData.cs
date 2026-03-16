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
        public int ennemyLevel;
        public int XPGiven;

        [Header("Encounter Data")]
        public System.Collections.Generic.List<string> ActiveCompanions = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> EncounterEnemies = new System.Collections.Generic.List<string>();

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
                MonsterController monster = enemy.GetComponent<MonsterController>();
                enemyTag    = enemy.tag;
                enemyName   = enemy.name;
                ennemyLevel = monster.GetEnemyLevel();
                XPGiven     = monster.GetXPGiven();
            } 

            EnvironmentType = EnvironmentType.Default;

            // Manage active party
            ActiveCompanions.Clear();
            ActiveCompanions.Add("Captain"); // player maincharacter
            // Safely poll the PartyManager for actual companions
            if (PartyManager.Instance != null)
            {
                foreach (var follower in PartyManager.Instance.GetActiveFollowers())
                {
                    ActiveCompanions.Add(follower.ToString()); // add follower in our party to combat scene
                }
            }

            EncounterEnemies.Clear();
            if (!string.IsNullOrEmpty(enemyTag)) EncounterEnemies.Add(enemyTag); // The monster you physically touched

            // TODO: add dynamic encounter enemy spawning that we can control
            EncounterEnemies.Add("skeleton_ranged"); // Force-spawn a second enemy so we can test multi-AI
        }

        // Clears cached references when returning to exploration.
        public void ClearCache()
        {
            _player = null;
            _mainCamera = null;
        }
    }
}

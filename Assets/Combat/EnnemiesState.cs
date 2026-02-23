using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Game.Combat
{
    /// <summary>
    /// Tracks enemy state across scene transitions.
    /// Marks enemies as dead so they are destroyed when returning to exploration.
    /// </summary>
    public class EnnemiesState : MonoBehaviour
    {
        public static EnnemiesState Instance { get; private set; }

        [SerializeField] private List<GameObject> ennemies = new List<GameObject>();
        [SerializeField] private List<string> deadEnnemiesNames = new List<string>();

        public void AddEnnemy(GameObject ennemy)
        {
            if (!ennemies.Contains(ennemy))
            {
                ennemies.Add(ennemy);
            }
        }

        public void SetDeadEnnemy(GameObject ennemy)
        {
            if (!deadEnnemiesNames.Contains(ennemy.name))
            {
                deadEnnemiesNames.Add(ennemy.name);
            }
        }

        public bool IsEnnemyDead(GameObject ennemy)
        {
            return deadEnnemiesNames.Contains(ennemy.name);
        }

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

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Exploration") return;

            foreach (string ennemyName in deadEnnemiesNames)
            {
                GameObject foundEnnemy = GameObject.Find(ennemyName);
                if (foundEnnemy != null)
                {
                    Destroy(foundEnnemy);
                }
            }
        }
    }
}

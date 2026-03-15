using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Core.Save;

namespace Game.Combat
{
    /// <summary>
    /// Tracks enemy state across scene transitions.
    /// Marks enemies as dead so they are destroyed when returning to exploration.
    /// </summary>
    public class EnnemiesState : MonoBehaviour, ISaveable
    {
        public static EnnemiesState Instance { get; private set; }
        [SerializeField] private List<string> deadEnnemiesNames = new List<string>();

        void Start()
        {
            SaveManager.Instance.Register(this);
        }

        public void SetDeadEnnemy(string enemyID)
        {
            if (!deadEnnemiesNames.Contains(enemyID))
            {
                deadEnnemiesNames.Add(enemyID);
            }
        }

        public bool IsEnnemyDead(string enemyID)
        {
            return deadEnnemiesNames.Contains(enemyID);
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

        public void SetSaveData(SaveData saveData)
        {
            saveData.enemy.deadEnnemiesNames = new List<string>(deadEnnemiesNames);
        }

        public void LoadSaveData(SaveData saveData)
        {
            deadEnnemiesNames = new List<string>(saveData.enemy.deadEnnemiesNames);

            // kill all ennemies that are in the list
            EnemyID[] enemies = FindObjectsOfType<EnemyID>();
            foreach (EnemyID enemy in enemies)
            {
                if (IsEnnemyDead(enemy.getEnemyID()))
                {
                    Destroy(enemy.gameObject);
                }
            }
        }
    }

}

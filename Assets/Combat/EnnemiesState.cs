using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


// Function that has a list with all ennemies states, so alive or dead. If they are dead. They are Destroyed on load
public class EnnemiesState : MonoBehaviour
{
    public static EnnemiesState Instance { get; private set; }
    [SerializeField] List<GameObject> Ennemies = new List<GameObject>();
    [SerializeField] List<string> deadEnnemiesNames = new List<string>();


    public void AddEnnemy(GameObject ennemy)
    {
        if (!Ennemies.Contains(ennemy))
        {
            Ennemies.Add(ennemy);
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
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // standard Unity functions that handle scene loading
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
        // Should only destroy ennemies if won the battle and player is coming back to exploration 
        if (scene.name != "Exploration")
            return;


        // Destroy all dead ennemies on load
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

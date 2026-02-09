
using UnityEngine;
using System.Collections.Generic;


public class TagToPrefab : MonoBehaviour
{
    [SerializeField] private List<GameObject> prefabs;
    public static TagToPrefab Instance { get; private set; }

    // Function that returns the prefab that is associated with the tag
    public GameObject GetPrefabForTag(string tag)
    {
        foreach (GameObject prefab in prefabs)
        {
            if (prefab.CompareTag(tag))
            {
                return prefab;
            }
        }

        Debug.LogWarning($" Found no Prefab for the selected tag : {tag}");
        return null;
    }

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
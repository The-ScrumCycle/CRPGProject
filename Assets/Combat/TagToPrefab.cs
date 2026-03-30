
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
            Debug.Log(prefab.tag);
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
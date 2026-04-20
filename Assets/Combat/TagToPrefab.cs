using UnityEngine;
using System;

public class TagToPrefab : MonoBehaviour
{
    public static TagToPrefab Instance { get; private set; }

    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private GameObject fallbackPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public GameObject GetPrefabForTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) 
        {
            Debug.LogWarning("[TagToPrefab] Encounter requested an empty tag. Spawning fallback.");
            return GetFallback();
        }

        // 1. Case-Insensitive Search
        foreach (var prefab in prefabs)
        {
            if (prefab.tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
            {
                return prefab;
            }
        }

        // 2. The Hardened Fallback (Prevents the NullReferenceException crash)
        Debug.LogWarning($"[TagToPrefab] CRITICAL: No prefab found for tag '{tag}'. Spawning fallback enemy!");
        return GetFallback();
    }

    private GameObject GetFallback()
    {
        if (fallbackPrefab != null) return fallbackPrefab;
        if (prefabs != null && prefabs.Length > 0) return prefabs[0]; // fallback
        
        Debug.LogError("[TagToPrefab] No fallback available! Game will likely crash.");
        return null;
    }
}

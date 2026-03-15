using UnityEngine;
using System;

public class EnemyID : MonoBehaviour
{
    [SerializeField] private string enemyID;
    
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(enemyID))
        {
            enemyID = Guid.NewGuid().ToString();
        }
    }

    public string getEnemyID()
    {
        return enemyID;
    }

}

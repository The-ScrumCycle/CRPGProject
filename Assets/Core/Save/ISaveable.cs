using UnityEngine;
using Core.Save;

public interface ISaveable
{   
    void SetSaveData(SaveData data);
    void LoadSaveData(SaveData data);
}


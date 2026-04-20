using UnityEngine;
using System;

namespace Core.Save
{

    [Serializable]
    public class CaptainData
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool active;
    }
}

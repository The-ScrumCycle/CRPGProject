using UnityEngine;
using System;

namespace Core.Save
{

    [Serializable]
    public class ShipData
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool controllable;
    }
}

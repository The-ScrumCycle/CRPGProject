using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Combat
{
    [CreateAssetMenu(fileName = "NewCombatEnvironment", menuName = "Combat Environment")]
    public class CombatEnvironment : ScriptableObject
    {
        public Material _boardMat;
        public Material _borderMat;
        public Texture2D _hexTex;
        public Color _hexColour;
        public Material _backgroundMat;
        public VolumeProfile _postProcessingProfile;
    }
}

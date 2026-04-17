using UnityEngine;

namespace Game.Combat.UI
{
    // Base class of implementations for arrow renderer
    public abstract class ArrowRenderer : MonoBehaviour
    {
        public abstract void Render(Vector3 startPos, Vector3 endPos, Color color, Color outlineColor, float offset, float bodyWidth, float headWidth, float headHeight);
        public abstract void Render(Vector3 startPos, Vector3 endPos, Color color, Color outlineColor, float offset);
        public abstract void SetColor(Color pColor);
        public abstract void SetOutlineColor(Color pColor);
    }
}
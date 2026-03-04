using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Combat.UI
{
    /// <summary>
    /// Class responsible for representing the predicted damage an action does to a unit
    /// <summary>
    public class UnitWorldUI : MonoBehaviour
    {
        [SerializeField] private Image healthFill;
        [SerializeField] private Image damagePreviewFill;
        [SerializeField] private TextMeshProUGUI healthText;
        private Canvas _canvas;

        public void UpdateState(int current, int max, int incomingDamage, bool isHovered, bool isPlayer)
        {
            float currentRatio = (float)current / max;
            float predictedRatio = Mathf.Max(0, (float)(current - incomingDamage) / max);

            if (healthFill != null) healthFill.fillAmount = predictedRatio;
            if (damagePreviewFill != null) damagePreviewFill.fillAmount = currentRatio;
            if (healthText != null) healthText.text = $"{Mathf.Max(0, current - incomingDamage)}/{max}";

            // For showing health bars we follow the rules -
            // Rule 1: Always show if taking damage
            // Rule 2: Show if hovered AND it is an enemy
            bool shouldShow = (incomingDamage > 0) || (isHovered && !isPlayer);
            
            if (_canvas != null) _canvas.enabled = shouldShow;
        } 
    }
}

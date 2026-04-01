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
        [SerializeField] private float rotateSpeed = 400.0f;

        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            // Force hidden on spawn so they don't clutter the screen before turn 1
            if (_canvas != null) _canvas.enabled = false; 
        }

        void Update()
        {
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up),
                rotateSpeed * Time.deltaTime);
        }

        public void UpdateState(int current, int max, int incomingDamage, bool isHovered, bool isPlayer)
        {
            float currentRatio = (float)current / max;
            float predictedRatio = Mathf.Max(0, (float)(current - incomingDamage) / max);

            if (healthFill != null) healthFill.fillAmount = predictedRatio;
            if (damagePreviewFill != null) damagePreviewFill.fillAmount = currentRatio;
            if (healthText != null) healthText.text = $"{Mathf.Max(0, current - incomingDamage)}/{max}";

            // Apply strict visibility rules -
            bool shouldShow = false;
            
            if (isPlayer)
            {
                // Rule 1: Player health bar ONLY shows if they are about to take damage
                shouldShow = (incomingDamage > 0);
            }
            else
            {
                // Rule 2: Enemy health bar shows if player hovers them, OR if they are taking damage
                shouldShow = isHovered || (incomingDamage > 0);
            }
            
            if (_canvas != null) _canvas.enabled = shouldShow;
        } 
    }
}

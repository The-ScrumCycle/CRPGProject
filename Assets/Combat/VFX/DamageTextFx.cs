using UnityEngine;
using TMPro;

namespace Game.Combat.VFX
{
    public class DamageTextFx : MonoBehaviour
    {
        private TextMeshPro _textMesh;
        private float _lifetime = 1.5f;
        private float _timer = 0f;

        // Static helper to spawn the text instantly from anywhere in code
        public static void Create(Vector3 position, string text, Color color)
        {
            GameObject go = new GameObject("DamageText");
            go.transform.position = position + Vector3.up * 2.5f; // Spawn above the unit
            var fx = go.AddComponent<DamageTextFx>();
            fx.Setup(text, color);
        }

        private void Setup(string text, Color color)
        {
            _textMesh = gameObject.AddComponent<TextMeshPro>();
            _textMesh.text = text;
            _textMesh.color = color;
            _textMesh.fontSize = 6;
            _textMesh.alignment = TextAlignmentOptions.Center;
            _textMesh.fontStyle = FontStyles.Bold;
        }

        void Update()
        {
            _timer += Time.deltaTime;
            transform.position += Vector3.up * Time.deltaTime * 1.5f; // Float upwards
            
            Color c = _textMesh.color;
            c.a = 1f - (_timer / _lifetime); // Fade out over time
            _textMesh.color = c;

            // text always face the camera
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }

            if (_timer > _lifetime) Destroy(gameObject);
        }
    }
}

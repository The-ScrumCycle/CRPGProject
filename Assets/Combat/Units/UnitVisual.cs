using UnityEngine;
using Game.Combat.Grid;
using System.Collections;

namespace Game.Combat.Units
{
    /// <summary>
    /// MonoBehaviour responsible for representing a Unit visually in the scene.
    /// Syncs the visual layer with the logical Unit (Unit.cs)
    /// </summary>
    public class UnitVisual : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotateSpeed = 400f;
        [SerializeField] private Color flashColor = Color.red;
        [SerializeField] private int numFlashes = 4;
        [SerializeField] private float flashDuration = 0.1f;
        [SerializeField] private float deathTime = 1.0f;

        private Unit _unit;
        private Renderer[] _renderers;
        private Color[] _originalColors;
        private HexGridRenderer _gridRenderer;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private int _standardLayer;
        private bool _isMoving;
        private bool _isRotating;
        private float positionOffset = 0.0f;

        public Unit Unit => _unit;

        public void Awake()
        {
            // Store exact colors for all mesh parts
            _renderers = GetComponentsInChildren<Renderer>();
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i].material.HasProperty("_BaseColor"))
                    _originalColors[i] = _renderers[i].material.GetColor("_BaseColor");
                else
                    _originalColors[i] = _renderers[i].material.color;
            }
        }

        // Initialize this visual with a logical unit.
        public void Initialize(Unit unit, HexGridRenderer gridRenderer)
        {
            _unit = unit;
            _gridRenderer = gridRenderer;
            _standardLayer = gameObject.layer;

            // Get Position Offset Safely
            GameObject offsetObj = gameObject;

            foreach (Transform child in transform)
            {
                if (child.CompareTag("Pedestal"))
                {
                    offsetObj = child.gameObject;
                    break;
                }
            }

            // Safely check for the MeshFilter before trying to read its bounds
            MeshFilter meshFilter = offsetObj.GetComponent<MeshFilter>();
            
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Note: Using sharedMesh avoids leaking memory by accidentally instantiating a new mesh
                positionOffset = Mathf.Abs(meshFilter.sharedMesh.bounds.min.y * transform.localScale.y + offsetObj.transform.position.y);
            }
            else
            {
                // Graceful fallback for animated characters or units without pedestals
                positionOffset = 0.0f; 
            }

            // Set initial position
            _targetPosition = _gridRenderer.HexToWorld(_unit.Coordinates) + Vector3.up*positionOffset;
            _targetRotation = Quaternion.Euler(0.0f, Quaternion.LookRotation(Vector3.zero - _targetPosition).eulerAngles.y, 0.0f) 
            * Quaternion.Euler(0.0f, Random.Range(0.0f, 80.0f), 0.0f);
            transform.position = _targetPosition;
            transform.rotation = _targetRotation;
        } 

        void Update()
        {
            if (_unit == null || _gridRenderer == null) return;

            // Sync position with logical unit
            _targetPosition = _gridRenderer.HexToWorld(_unit.Coordinates) + Vector3.up*positionOffset;

            if (_isMoving)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    _targetPosition,
                    moveSpeed * Time.deltaTime
                );

                if (Vector3.Distance(transform.position, _targetPosition) < 0.01f)
                {
                    transform.position = _targetPosition;
                    _isMoving = false;
                }
            }

            if (_isRotating)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    _targetRotation,
                    rotateSpeed * Time.deltaTime
                );

                if (Quaternion.Angle(transform.rotation, _targetRotation) < 0.01f)
                {
                    transform.rotation = _targetRotation;
                    _isRotating = false;
                }
            }
        }

        // Trigger visual movement to unit's current logical position.
        public void RefreshPosition()
        {
            _isMoving = true;
        }

        public void LookAtCell(HexCoordinates cell)
        {
            _targetRotation = Quaternion.Euler(0.0f, Quaternion.LookRotation(_gridRenderer.HexToWorld(cell) - transform.position).eulerAngles.y, 0.0f);
            _isRotating = true;
        }

        // Move to unit's current logical position.
        public void SnapToPosition()
        {
            if (_unit != null && _gridRenderer != null)
            {
                _targetPosition = _gridRenderer.HexToWorld(_unit.Coordinates) + Vector3.up*positionOffset;
                transform.position = _targetPosition;
                _isMoving = false;
            }
        }

        public void SetHighlight(bool highlight)
        {
            int intendedLayer = _standardLayer;
            if (highlight) intendedLayer = LayerMask.NameToLayer("ReceiveOutline");

            gameObject.layer = intendedLayer;
            foreach (Transform child in transform)
            {
                child.gameObject.layer = intendedLayer;
            }
        }

        public void Flash()
        {
            StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i].material.HasProperty("_BaseColor")) _renderers[i].material.SetColor("_BaseColor", flashColor);
                else _renderers[i].material.color = flashColor;
            }
            
            yield return new WaitForSeconds(flashDuration*0.8f);
            
            // Returns unit back to its native colors
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i].material.HasProperty("_BaseColor")) _renderers[i].material.SetColor("_BaseColor", _originalColors[i]);
                else _renderers[i].material.color = _originalColors[i];
            }

            for (int i = 0; i < numFlashes; i++)
            {
                SetVisibility(false);
                yield return new WaitForSeconds(flashDuration);
                SetVisibility(true);
                yield return new WaitForSeconds(flashDuration);
            }
        }

        public void Die()
        {
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            float timeElapsed = 0.0f;
            while (timeElapsed < deathTime)
            {
                timeElapsed += Time.deltaTime;

                for (int i = 0; i < _renderers.Length; i++)
                {
                    _renderers[i].material.color = Color.Lerp(_renderers[i].material.color, Color.black, Time.deltaTime);
                }
                
                transform.position -= new Vector3(0.0f, Time.deltaTime, 0.0f);
                yield return null;
            }
            Destroy(gameObject);
        }

        public void HealEffect()
        {
            StartCoroutine(HealRoutine());
        }

        private IEnumerator HealRoutine()
        {
           yield return null; 
        }

        public void SetVisibility(bool visible)
        {
            foreach(MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
            {
                renderer.enabled = visible;
            }
            foreach(SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.enabled = visible;
            }
        }
    }
}
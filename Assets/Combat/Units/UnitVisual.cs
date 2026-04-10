using UnityEngine;
using Game.Combat.Grid;

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

        private Unit _unit;
        private HexGridRenderer _gridRenderer;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private bool _isMoving;
        private bool _isRotating;
        private float positionOffset = 0.0f;

        public Unit Unit => _unit;

        // Initialize this visual with a logical unit.
        public void Initialize(Unit unit, HexGridRenderer gridRenderer)
        {
            _unit = unit;
            _gridRenderer = gridRenderer;

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
    }
}

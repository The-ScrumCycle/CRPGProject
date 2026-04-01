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

        private Unit _unit;
        private HexGridRenderer _gridRenderer;
        private Vector3 _targetPosition;
        private bool _isMoving;
        private float positionOffset = 0.0f;

        public Unit Unit => _unit;

        // Initialize this visual with a logical unit.
        public void Initialize(Unit unit, HexGridRenderer gridRenderer)
        {
            _unit = unit;
            _gridRenderer = gridRenderer;

            // Get Position Offset
            GameObject offsetObj = gameObject;
            foreach (Transform child in transform){
                if (child.tag == "Pedestal")
                {
                    Debug.Log("Pedestal Found");
                    offsetObj = child.gameObject;
                }
            }
            positionOffset = Mathf.Abs(offsetObj.GetComponent<MeshFilter>().mesh.bounds.min.y*transform.localScale.y + offsetObj.transform.position.y);

            // Set initial position
            _targetPosition = _gridRenderer.HexToWorld(_unit.Coordinates) + Vector3.up*positionOffset;
            transform.position = _targetPosition;
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
        }

        // Trigger visual movement to unit's current logical position.
        public void RefreshPosition()
        {
            _isMoving = true;
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

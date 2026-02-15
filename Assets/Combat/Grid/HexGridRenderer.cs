using UnityEngine;
using Game.Combat.Units;
using System.Collections.Generic;

namespace Game.Combat.Grid
{
    /// <summary>
    /// MonoBehaviour responsible for rendering the hex grid, this allows us to add fancy visuals for higlighting
    /// attacks, movements, telegraphed AI plans, and all sorts of visualizations of our hexgrid.
    /// </summary>
    public class HexGridRenderer : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private int gridWidth = 8;
        [SerializeField] private int gridHeight = 8;

        [Header("Hex Rendering")]
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color hexColor = Color.black;
        [SerializeField] private Color highlightColor = Color.cyan;
        [SerializeField] private Color reachableColor = Color.green;
        [SerializeField][Range(0.0f, 1.0f)] private float lineThickness = 0.1f;
        [SerializeField][Min(1)] private int hexScale = 5;
        [SerializeField] private bool clipEdges = true;

        private Material _hexMaterial;
        private Camera _camera;
        private HexGrid _logicalGrid;

        private HexCoordinates _hoveredHex;
        private HashSet<HexCoordinates> _highlightedCells = new HashSet<HexCoordinates>();

        private const float CANONICAL_SIZE = 10.0f;
        private const float SQRT3 = 1.7320508f;

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;

        public Vector3 HexSize { get; private set; }
        public Vector3 GridOrigin { get; private set; }

        void Awake()
        {
            _camera = Camera.main;
            _hexMaterial = GetComponent<MeshRenderer>().material;
        }

        // Initialize the renderer with a logical grid.
        public void Initialize(HexGrid logicalGrid)
        {
            _logicalGrid = logicalGrid;
            CalculateGridMetrics();
            UpdateShaderParams();
        }

        void Update()
        {
            if (_logicalGrid == null) return;

            UpdateHoveredHex();
            UpdateShaderParams();
        }

        private void CalculateGridMetrics()
        {
            HexSize = new Vector3(
                CANONICAL_SIZE / hexScale,
                0.0f,
                CANONICAL_SIZE / hexScale * SQRT3 / 2.0f
            );

            var colliderBounds = GetComponent<Collider>().bounds;
            GridOrigin = colliderBounds.max - new Vector3(HexSize.x / 2.0f, 0.0f, HexSize.z);

            transform.localScale = new Vector3(
                (float)gridWidth / hexScale + (HexSize.x / CANONICAL_SIZE / 2.0f),
                transform.localScale.y,
                (float)((HexSize.z * gridHeight + HexSize.z) / CANONICAL_SIZE)
            );
        }

        private void UpdateHoveredHex()
        {
            Vector3 mouseWorldPos = _camera.ScreenToWorldPoint(
                new Vector3(
                    Input.mousePosition.x,
                    Input.mousePosition.y,
                    _camera.nearClipPlane + _camera.transform.position.y
                )
            );

            _hoveredHex = WorldToHex(mouseWorldPos);
        }

        private void UpdateShaderParams()
        {
            _hexMaterial.SetFloat("_HexScale", hexScale);
            _hexMaterial.SetVector("_GridScale", new Vector4(transform.localScale.x, transform.localScale.z, 0, 0));
            _hexMaterial.SetVector("_GridDim", new Vector4(gridWidth, gridHeight, 0, 0));
            _hexMaterial.SetFloat("_LineWeight", lineThickness);
            _hexMaterial.SetColor("_BaseColor", baseColor);
            _hexMaterial.SetColor("_HexColor", hexColor);
            _hexMaterial.SetInt("_ClipEdges", clipEdges ? 1 : 0);
            _hexMaterial.SetVector("_ActiveHex", new Vector4(_hoveredHex.q, _hoveredHex.r, 0, 0));
        }

        #region Coordinate Conversion

        // Convert world position to hex coordinates.
        public HexCoordinates WorldToHex(Vector3 worldPos)
        {
            int r = (int)((GridOrigin.z + (HexSize.z / 2.0f) - worldPos.z) / HexSize.z);
            int q = (int)((GridOrigin.x + (HexSize.x / 2.0f)
                        - (r % 2 == 0 ? 0.0f : HexSize.x / 2.0f)
                        - worldPos.x) / HexSize.x);

            return new HexCoordinates(q, r);
        }

        // Convert hex coordinates to world position.
        public Vector3 HexToWorld(HexCoordinates coords)
        {
            return GridOrigin - new Vector3(
                coords.q * HexSize.x + (coords.r % 2 == 0 ? 0.0f : HexSize.x / 2.0f),
                0.0f,
                coords.r * HexSize.z
            );
        }

        #endregion

        #region Highlighting

        // Get the currently hovered hex.
        public HexCoordinates GetHoveredHex()
        {
            return _hoveredHex;
        }

        // Set cells to highlight (e.g movement range).
        public void SetHighlightedCells(IEnumerable<HexCoordinates> cells)
        {
            _highlightedCells.Clear();
            foreach (var cell in cells)
            {
                _highlightedCells.Add(cell);
            }
        }

        // Clear all highlighted cells.
        public void ClearHighlight()
        {
            _highlightedCells.Clear();
        }

        // Check if a cell is highlighted.
        public bool IsCellHighlighted(HexCoordinates coords)
        {
            return _highlightedCells.Contains(coords);
        }

        #endregion
    }
}

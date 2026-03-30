using UnityEngine;
using System.Collections.Generic;
using Game.Combat.Actions;
using Game.Core.Transitions;
using System;

namespace Game.Combat.Grid
{
    public class HexGridRenderer : MonoBehaviour
    {
	/// <summary>
        /// MonoBehaviour responsible for rendering the hex grid, this allows us to add fancy visuals for higlighting
        /// attacks, movements, telegraphed AI plans, and all sorts of visualizations of our hexgrid.
        /// </summary>
        [Header("Grid Configuration")]
        [SerializeField] private int gridWidth = 13;
        [SerializeField] private int gridHeight = 13;
        [SerializeField][Min(1)] private int hexScale = 13;

	// All the movement, attack and highlighting coloring for the visual telegraphing
        [Header("Hex Rendering")]
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color hexColor = Color.black;
        [SerializeField] private Color playerMoveColor = new Color(0.0f, 0.8f, 0.2f, 1.0f);
        [SerializeField] private Color playerAttackColor = new Color(0.9f, 0.1f, 0.1f, 1.0f);
        [SerializeField] private Color aiMoveColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);
        [SerializeField] private Color aiAttackBaseColor = new Color(1.0f, 0.4f, 0.0f, 1.0f);
        [SerializeField] private Color aiAttackBrightColor = new Color(1.0f, 0.7f, 0.2f, 1.0f);
        [SerializeField] private Color hoverColor = new Color(0.0f, 0.8f, 0.8f, 1.0f);
        [SerializeField][Range(1.0f, 10.0f)] private float hoverBrightnessMultiplier = 1.3f;
        [SerializeField][Range(1.0f, 10.0f)] private float pulseSpeed = 4.0f;
        [SerializeField][Range(0.0f, 1.0f)] private float lineThickness = 0.1f;
        [SerializeField][Range(0.0f, 20.0f)] private float textureScale = 1.0f;
        [SerializeField] private bool clipEdges = true;
        [SerializeField] private bool showGridMetrics = true; // Debug, see gizmos

        private Material _hexMaterial;
        private Camera _camera;
        private HexGrid _logicalGrid;

        private HexCoordinates _hoveredHex;
        private Dictionary<HexCoordinates, HighlightType> _highlightedCells = new Dictionary<HexCoordinates, HighlightType>();

        private const int MAX_HIGHLIGHTS = 256; //temp maximum for how many cells we even need to consider in a level
        private Vector4[] _highlightArray = new Vector4[MAX_HIGHLIGHTS];

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

            for (int i = 0; i < MAX_HIGHLIGHTS; i++)
            {
                _highlightArray[i] = new Vector4(-9999, -9999, 0, 0);
            }
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showGridMetrics) return;

            Gizmos.color = Color.green;

            // Draw point at every hex center
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Gizmos.DrawSphere(HexToWorld(new HexCoordinates(x, y)), 1.0f/hexScale);
                }
            }

            // Draw point at all neighbours of active hex
            Gizmos.color = Color.red;
            foreach (HexCell hex in _logicalGrid.GetNeighbors(_hoveredHex))
            {
                Gizmos.DrawSphere(HexToWorld(hex.Coordinates), 1.0f/hexScale);
            }

            // Log distance from hovered hex to origin
            //Debug.Log(HexCoordinates.Distance(_hoveredHex, WorldToHex(GridOrigin)));

            // Draw point at grid origin
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(GridOrigin, 1.0f/hexScale);

            Gizmos.color = Color.blue;
            Ray mouseRay = _camera.ScreenPointToRay(Input.mousePosition);
            float distance;
            new Plane(Vector3.up, 0.0f).Raycast(mouseRay, out distance);
            Vector3 mouseWorldPos = mouseRay.GetPoint(distance);
            Gizmos.DrawSphere(mouseWorldPos, 1.0f/hexScale);
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

            transform.localScale = new Vector3(
                (float)gridWidth / hexScale + (HexSize.x / CANONICAL_SIZE / 2.0f),
                transform.localScale.y,
                (float)((HexSize.z * gridHeight + HexSize.z) / CANONICAL_SIZE)
            );
            Physics.SyncTransforms(); // Force collider to update

            GridOrigin = GetComponent<Collider>().bounds.max - new Vector3(HexSize.x / 2.0f, 0.0f, HexSize.z);
        }

        private void UpdateHoveredHex()
        {
            Ray mouseRay = _camera.ScreenPointToRay(Input.mousePosition);
            float distance;
            new Plane(Vector3.up, 0.0f).Raycast(mouseRay, out distance); // Assumes that hex grid is positioned at origin
            Vector3 mouseWorldPos = mouseRay.GetPoint(distance);

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
            _hexMaterial.SetColor("_PlayerMoveColor", playerMoveColor);
            _hexMaterial.SetColor("_PlayerAttackColor", playerAttackColor);
            _hexMaterial.SetColor("_AIMoveColor", aiMoveColor);
            _hexMaterial.SetColor("_AIAttackBaseColor", aiAttackBaseColor);
            _hexMaterial.SetColor("_AIAttackBrightColor", aiAttackBrightColor);
            _hexMaterial.SetColor("_HoverColor", hoverColor);
            _hexMaterial.SetFloat("_HoverBrightness", hoverBrightnessMultiplier);
            _hexMaterial.SetFloat("_PulseSpeed", pulseSpeed);
            _hexMaterial.SetFloat("_BaseMapScale", textureScale);
            _hexMaterial.SetInt("_ClipEdges", clipEdges ? 1 : 0);
            _hexMaterial.SetVector("_ActiveHex", new Vector4(_hoveredHex.q, _hoveredHex.r, 0, 0));

            int highlightCount = 0;
            foreach (var kvp in _highlightedCells)
            {
                if (highlightCount >= MAX_HIGHLIGHTS) break;

                _highlightArray[highlightCount] = new Vector4(
                    kvp.Key.q,
                    kvp.Key.r,
                    (int)kvp.Value,
                    0
                );
                highlightCount++;
            }

            for (int i = highlightCount; i < MAX_HIGHLIGHTS; i++)
            {
                _highlightArray[i] = new Vector4(-9999, -9999, 0, 0);
            }

            _hexMaterial.SetInt("_HighlightCount", highlightCount);
            _hexMaterial.SetVectorArray("_Highlights", _highlightArray);
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

        public HexCoordinates GetHoveredHex()
        {
            return _hoveredHex;
        }

	/// Set cells to highlight (e.g movement range)
        /// Check highlight priorities to replace highlights. Highlight type is responsible for content.
        public void SetHighlights(Dictionary<HexCoordinates, HighlightType> highlights)
        {
            _highlightedCells.Clear();
            foreach (var kvp in highlights)
            {
                if (kvp.Value != HighlightType.None)
                {
                    _highlightedCells[kvp.Key] = kvp.Value;
                }
            }
        }

        /// Add a single highlight. Only overwrites if new type has higher priority.
        public void AddHighlight(HexCoordinates coords, HighlightType type)
        {
            if (type == HighlightType.None) return;

            if (_highlightedCells.TryGetValue(coords, out var existing))
            {
                if ((int)type > (int)existing)
                {
                    _highlightedCells[coords] = type;
                }
            }
            else
            {
                _highlightedCells[coords] = type;
            }
        }

        /// highlight cells as PlayerMove
        public void SetHighlightedCells(IEnumerable<HexCoordinates> cells)
        {
            _highlightedCells.Clear();
            foreach (var cell in cells)
            {
                _highlightedCells[cell] = HighlightType.PlayerMove;
            }
        }

        /// Highlight cells with a specific type, replacing all existing highlights.
        public void SetHighlightedCells(IEnumerable<HexCoordinates> cells, HighlightType type)
        {
            _highlightedCells.Clear();
            foreach (var cell in cells)
            {
                _highlightedCells[cell] = type;
            }
        }

        public void RemoveHighlight(HexCoordinates coords)
        {
            _highlightedCells.Remove(coords);
        }

        public void ClearHighlights()
        {
            _highlightedCells.Clear();
        }

        public void ClearHighlight()
        {
            ClearHighlights();
        }

        public bool IsCellHighlighted(HexCoordinates coords)
        {
            return _highlightedCells.ContainsKey(coords);
        }

        public HighlightType GetHighlightType(HexCoordinates coords)
        {
            if (_highlightedCells.TryGetValue(coords, out var type))
            {
                return type;
            }
            return HighlightType.None;
        }

        #endregion
    }
}

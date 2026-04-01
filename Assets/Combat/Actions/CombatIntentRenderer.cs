using UnityEngine;
using System.Collections.Generic;
using Game.Combat.Grid;

namespace Game.Combat.Actions
{
    /// <summary>
    /// Converts ActionIntents into highlight data for HexGridRenderer.
    /// Does not touch the renderer directly the caller reads GetHighlights() and feeds the renderer with the data from here.
    /// </summary>
    public class CombatIntentRenderer
    {
        private GameObject _arrowPrefab;
        private List<GameObject> _activeArrows;
        private HexGridRenderer _gridRenderer;
        private readonly Dictionary<HexCoordinates, HighlightType> _highlights;

        public CombatIntentRenderer(GameObject arrowPrefab)
        {
            _highlights = new Dictionary<HexCoordinates, HighlightType>();
            _arrowPrefab = arrowPrefab;
            _activeArrows = new List<GameObject>();
            _gridRenderer = UnityEngine.Object.FindObjectOfType<HexGridRenderer>();
        }

        // Render a single intent into highlight data.
        // Our higlights currently are movement cells, AoE and regular attack cells, push direction arrow.
        public void Render(ActionIntent intent)
        {
            if (intent == null || !intent.IsValid) return;

            HighlightType type = MapVisualType(intent.VisualType);
            if (type == HighlightType.None) return;

            // 1. HIGHLIGHT MOVEMENT PATH
            if (intent.MovementPath != null && intent.MovementPath.Count > 0)
            {
                foreach (var coords in intent.MovementPath)
                {
                    if (coords != intent.Actor.Coordinates) 
                    {
                        AddWithPriority(coords, HighlightType.AI_Move);
                    }
                }
            }

            // 2. HIGHLIGHT ALL AOE TARGET CELLS
            var targetCells = intent.Action.GetTargetCells();
            if (targetCells != null)
            {
                foreach (var cellCoords in targetCells)
                {
                    AddWithPriority(cellCoords, type);
                }
            }

            // 3. RENDER PUSH / PULL ARROW
            if (intent.PushDestination.HasValue || intent.VisualType == ActionVisualType.Pull)
            {
                HexCoordinates startHex = intent.TargetUnit.Coordinates;
                
                HexCoordinates endHex = intent.SecondaryBumpTarget != null ? 
                                        intent.SecondaryBumpTarget.Coordinates : 
                                        (intent.PushDestination.HasValue ? intent.PushDestination.Value : startHex);

                if (startHex != endHex) // Only draw if displacement occurs
                {
                    Vector3 startWorldPos = _gridRenderer.HexToWorld(startHex);
                    Vector3 endWorldPos = _gridRenderer.HexToWorld(endHex);

                    // The trajectory from start to end natively points towards the caster.
                    GameObject arrow = UnityEngine.Object.Instantiate(_arrowPrefab);
                    
                    arrow.transform.position = (startWorldPos + endWorldPos) / 2f;
                    arrow.transform.position += Vector3.up * 0.5f; 

                    Vector3 direction = (endWorldPos - startWorldPos).normalized;
                    if (direction != Vector3.zero)
                    {
                        arrow.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                    }

                    float distance = Vector3.Distance(startWorldPos, endWorldPos);
                    arrow.transform.localScale = new Vector3(1f, 1f, distance * 0.5f);

                    _activeArrows.Add(arrow);
                }
            } 
        } 

        // Render all intents from a list into highlight data
        // NOTE: Call Clear() first if you want a fresh pass
        public void RenderAll(IReadOnlyList<ActionIntent> intents)
        {
            for (int i = 0; i < intents.Count; i++)
            {
                Render(intents[i]);
            }
        }

        // Clear all stored highlight data and attack arrows
        public void Clear()
        {
            _highlights.Clear();
            foreach (var arrow in _activeArrows)
            {
                if (arrow != null) UnityEngine.Object.Destroy(arrow);
            }
            _activeArrows.Clear();
        }

        // Get the current highlight dictionary for feeding into HexGridRenderer.
        public IReadOnlyDictionary<HexCoordinates, HighlightType> GetHighlights()
        {
            return _highlights;
        }

        /// Map ActionVisualType to the corresponding AI HighlightType
        private HighlightType MapVisualType(ActionVisualType visualType)
        {
            switch (visualType)
            {
                case ActionVisualType.Move:
                    return HighlightType.AI_Move;
                case ActionVisualType.MeleeAttack:
                case ActionVisualType.RangedAttack:
                case ActionVisualType.Grapple:
                    return HighlightType.AI_Attack;
                case ActionVisualType.Heal:
                    return HighlightType.AI_Move;
                default:
                    return HighlightType.None;
            }
        }

        // Add a highlight, only overwriting if new type has higher priority
        private void AddWithPriority(HexCoordinates coords, HighlightType type)
        {
            if (_highlights.TryGetValue(coords, out var existing))
            {
                if ((int)type > (int)existing)
                {
                    _highlights[coords] = type;
                }
            }
            else
            {
                _highlights[coords] = type;
            }
        }
    }
}

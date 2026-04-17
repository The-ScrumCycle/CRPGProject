using UnityEngine;
using System.Collections.Generic;
using Game.Combat.Grid;
using Game.Combat.Units;
using Game.Combat.UI;

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
        public bool _renderArrows {get; set;}
        private readonly Dictionary<HexCoordinates, HighlightType> _highlights;

        public CombatIntentRenderer(GameObject arrowPrefab)
        {
            _highlights = new Dictionary<HexCoordinates, HighlightType>();
            _arrowPrefab = arrowPrefab;
            _activeArrows = new List<GameObject>();
            _gridRenderer = UnityEngine.Object.FindObjectOfType<HexGridRenderer>();
            _renderArrows = true;
        }

        private void RenderArrow(Vector3 startPos, Vector3 endPos, Color color)
        {
            if (!_renderArrows) return;

            // The trajectory from start to end natively points towards the caster.
            GameObject arrow = Object.Instantiate(_arrowPrefab);
            arrow.GetComponent<ArrowRenderer>().Render(startPos, endPos, new Color(color.r, color.g, color.b, 0.5f), 0.1f + Random.Range(0.0f, 0.001f));
            _activeArrows.Add(arrow);
        }

        // Render a single intent into highlight data.
        // Our higlights currently are movement cells, AoE and regular attack cells, push direction arrow.
        public void Render(ActionIntent intent, UnitVisual visual)
        {
            if (intent == null || visual == null || !intent.IsValid) return;

            HighlightType type = MapVisualType(intent.VisualType);
            if (type == HighlightType.None) return;

            // We randomly offset the height of the arrows to prevent z-fighting
            // We want this to look consistent frame-to-frame so we set the random seed by the unit's id
            Random.InitState(intent.Actor.Id);

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

                RenderArrow(
                    _gridRenderer.HexToWorld(intent.Actor.Coordinates), 
                    _gridRenderer.HexToWorld(intent.MovementPath[^1]),
                    _gridRenderer.GetGridColor(type)
                );
                visual.LookAtCell(intent.MovementPath[^1]);
            }

            // 2. HIGHLIGHT ALL AOE TARGET CELLS
            var targetCells = intent.Action.GetTargetCells();
            if (targetCells != null && !(intent.VisualType == ActionVisualType.MeleeAttack))
            {
                Vector3 averagePos = Vector3.zero;
                int count = 0;
                foreach (var cellCoords in targetCells)
                {
                    AddWithPriority(cellCoords, type);
                    averagePos += _gridRenderer.HexToWorld(cellCoords);
                    count++;
                }
                averagePos /= count;

                if (intent.Actor.Role == UnitRole.Enemy)
                {
                    RenderArrow(
                        _gridRenderer.HexToWorld(intent.Actor.Coordinates), 
                        _gridRenderer.HexToWorld(_gridRenderer.WorldToHex(averagePos)),
                        _gridRenderer.GetGridColor(type)
                    );
                }

                visual.LookAtCell(_gridRenderer.WorldToHex(averagePos));
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
                    RenderArrow(
                        _gridRenderer.HexToWorld(startHex), 
                        _gridRenderer.HexToWorld(endHex),
                        _gridRenderer.GetGridColor(HighlightType.PlayerAttack)
                    );
                    visual.LookAtCell(endHex);
                }
            } 
        } 

        // Render all intents from a list into highlight data
        // NOTE: Call Clear() first if you want a fresh pass
        public void RenderAll(IReadOnlyDictionary<ActionIntent, UnitVisual> intents)
        {
            foreach (var (intent, visual) in intents)
            {
                Render(intent, visual);
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
                case ActionVisualType.Push:
                    return HighlightType.PlayerAttack;
                case ActionVisualType.Pull:
                    return HighlightType.PlayerAttack;
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

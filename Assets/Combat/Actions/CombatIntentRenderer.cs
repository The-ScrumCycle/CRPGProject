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
        private readonly Dictionary<HexCoordinates, HighlightType> _highlights;

        public CombatIntentRenderer()
        {
            _highlights = new Dictionary<HexCoordinates, HighlightType>();
        }

        // Render a single intent into highlight data.
        // Only stores the destination tile (NOTE: not full movement path, so we can't add like a tank class that damages through movement)
        public void Render(ActionIntent intent)
        {
            if (intent == null || !intent.IsValid) return;

            HighlightType type = MapVisualType(intent.VisualType);
            if (type == HighlightType.None) return;

            foreach (var cell in intent.TargetCells)
            {
                AddWithPriority(cell, type);
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

        // Clear all stored highlight data.
        public void Clear()
        {
            _highlights.Clear();
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
                    return HighlightType.AI_Attack;
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

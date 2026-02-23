using Game.Combat.Units;
using Game.Combat.Grid;
using System.Collections.Generic;

namespace Game.Combat.Actions
{
    /// <summary>
    /// Interface for all combat actions.
    /// Actions are deterministic e.g if valid they succeed.
    /// </summary>
    public interface ICombatAction
    {
        // The unit performing this action.
        Unit Actor { get; }

        // Get all cells affected by this action.
        IEnumerable<HexCoordinates> GetTargetCells();

        // Check if this action is valid given the current game state.
        bool IsValid(HexGrid grid);

        // Execute the action. Only call if IsValid returns true.
        void Execute(HexGrid grid);
    }
}

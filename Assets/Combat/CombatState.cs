namespace Game.Combat
{
    /// <summary>
    /// Represents the current phase of combat.
    /// </summary>
    public enum CombatState
    {
        Initializing,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat
    }
}

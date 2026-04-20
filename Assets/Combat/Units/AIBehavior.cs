namespace Game.Combat.Units
{
    /// <summary>
    /// Defines AI behavior patterns for enemy units.
    /// This allows us to have several AI "archetypes" that act in distinct ways.
    /// We can try this system and if ever it's not ideal for decent AI gameplay we can totally refactor.
    /// </summary>
    public enum AIBehavior
    {
        // default behavior
        Aggressive,
        Defensive,

	    // behaviors for our actual enemies
        SkeletonRanged,
        SkeletonMelee,
        Healer,
        HydraGrappler,
        Malakor,
        Crystal
    }
}

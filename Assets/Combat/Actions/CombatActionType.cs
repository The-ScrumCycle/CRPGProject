namespace Game.Combat.Actions
{
    /// <summary>
    /// Identifiers for the types of actions a unit can perform
    /// Used for configuring unit loadouts in the Unity Inspector
    /// </summary>
    public enum CombatActionType
    {
        MeleeAttack,
        RangedAttack,
        RangedHeal,
        Grapple,
	    SweepAttack,
	    SplashAttack,
	    HeavyMeleeAttack, // Warrior ability
	    PullAlly // Cleric ability
    }
}

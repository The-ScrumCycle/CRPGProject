using Game.Combat.Units;

namespace Game.Combat.AI
{
    public static class BrainFactory
    {
        /// <summary>
        /// Returns the appropriate AI strategy based on the unit's assigned behavior
        /// </summary>
        public static IEnemyBrain GetBrain(AIBehavior behavior)
        {
            switch (behavior)
            {
                // Alex: As you create new classes (e.g., SkeletonMeleeBrain, HealerBrain), 
                // wire them up here and add the enums to AIBehavior.cs so we keep following the "Factory Key" pattern that Unity devs tend to use for this type of thing
                case AIBehavior.HydraGrappler: return new HydraGrapplerBrain(); 
		        // case AIBehavior.SkeletonRanged: return new SkeletonRangedBrain();
                // case AIBehavior.Healer: return new HealerBrain();
                
                default: return null; 
            }
        }
    }
}

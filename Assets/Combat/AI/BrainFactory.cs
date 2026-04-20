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
                case AIBehavior.Malakor:          return new MalakorBrain();
                case AIBehavior.SkeletonMelee:    return new SkeletonMeleeBrain();
                case AIBehavior.SkeletonRanged:   return new SkeletonRangedBrain();
                case AIBehavior.Healer:           return new HealerBrain();
                case AIBehavior.HydraGrappler:    return new HydraGrapplerBrain();
                case AIBehavior.Crystal:          return null; // passes turn automatically
                default:                          return null;
            }
        }
    }
}

namespace Game.Core.Transitions
{
    /// <summary>
    /// Data returned from Combat to Exploration after combat ends.
    /// Created simply for tracking exp awarded, death/victory and overworld enemy data (may have to refactor later)
    /// </summary>
    public class CombatResultData
    {
        public bool wasVictory;
        public int experienceAwarded;
        public string defeatedEnemyName;

        public CombatResultData(bool victory, int experience, string enemyName)
        {
            wasVictory = victory;
            experienceAwarded = experience;
            defeatedEnemyName = enemyName;
        }
    }
}

using System;

namespace Game.Core.Party
{
    /// <summary>
    /// Data structure representing a unit's combat statistics.
    /// Used as template data in CrewMember and as instance data in combat Units.
    /// </summary>
    [Serializable]
    public class UnitStats
    {
        public int maxHealth;
        public int currentHealth;
        public int attackPower;
        public int movementRange;
        public int attackRange;

        public UnitStats() { }

        public UnitStats(int maxHealth, int attackPower, int movementRange, int attackRange)
        {
            this.maxHealth = maxHealth;
            this.currentHealth = maxHealth;
            this.attackPower = attackPower;
            this.movementRange = movementRange;
            this.attackRange = attackRange;
        }

        public bool IsAlive => currentHealth > 0;

        public void TakeDamage(int damage)
        {
            currentHealth = Math.Max(0, currentHealth - damage);
        }

        public void Heal(int amount)
        {
            currentHealth = Math.Min(maxHealth, currentHealth + amount);
        }

        public UnitStats Clone()
        {
            return new UnitStats
            {
                maxHealth = this.maxHealth,
                currentHealth = this.currentHealth,
                attackPower = this.attackPower,
                movementRange = this.movementRange,
                attackRange = this.attackRange
            };
        }
    }
}

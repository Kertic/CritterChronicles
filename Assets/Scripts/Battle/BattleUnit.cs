using AutobattlerSample.Data;

namespace AutobattlerSample.Battle
{
    public class BattleUnit
    {
        public UnitInstance Instance { get; }
        public bool IsAlly { get; }
        public int CurrentHP { get; set; }
        public int Shield { get; set; }
        public int CurrentCooldown { get; set; }
        public bool IsAlive => CurrentHP > 0;
        public bool IsReady => CurrentCooldown <= 0;

        public string DisplayName => Instance.DisplayName;
        public int MaxHP => Instance.EffectiveMaxHP;
        public int AttackDamage => Instance.EffectiveAttackDamage;
        public int Cooldown => Instance.EffectiveCooldown;
        public int Rank => Instance.Rank;
        public PassiveType Passive => Instance.Passive;

        public BattleUnit(UnitInstance instance, bool isAlly)
        {
            Instance = instance;
            IsAlly = isAlly;
            CurrentHP = instance.CurrentHP;
            Shield = instance.Shield;
            CurrentCooldown = 0;
        }

        public void TickCooldown()
        {
            if (CurrentCooldown > 0)
                CurrentCooldown--;
        }

        public void StartCooldown()
        {
            CurrentCooldown = Cooldown;
        }

        /// <summary>
        /// Deal damage, absorbing Shield first, then HP. Returns (totalDamage, shieldAbsorbed).
        /// </summary>
        public (int damage, int shieldAbsorbed) TakeDamage(int rawDamage)
        {
            int remaining = rawDamage;
            int shieldAbsorbed = 0;

            if (Shield > 0)
            {
                shieldAbsorbed = remaining > Shield ? Shield : remaining;
                Shield -= shieldAbsorbed;
                remaining -= shieldAbsorbed;
            }

            CurrentHP -= remaining;
            if (CurrentHP < 0) CurrentHP = 0;

            return (rawDamage, shieldAbsorbed);
        }

        public int Heal(int amount)
        {
            int before = CurrentHP;
            CurrentHP += amount;
            if (CurrentHP > MaxHP) CurrentHP = MaxHP;
            return CurrentHP - before;
        }

        public void WriteBackHP()
        {
            Instance.CurrentHP = CurrentHP;
            Instance.Shield = Shield;
        }
    }
}

using AutobattlerSample.Data;

namespace AutobattlerSample.Battle
{
    public class BattleUnit
    {
        public UnitInstance Instance { get; }
        public bool IsAlly { get; }
        public int CurrentHP { get; set; }
        public bool IsAlive => CurrentHP > 0;

        public string DisplayName => Instance.DisplayName;
        public int MaxHP => Instance.EffectiveMaxHP;
        public int Armor => Instance.EffectiveArmor;
        public int Attack => Instance.EffectiveAttack;

        public BattleUnit(UnitInstance instance, bool isAlly)
        {
            Instance = instance;
            IsAlly = isAlly;
            CurrentHP = instance.CurrentHP;
        }

        public int TakeDamage(int rawDamage)
        {
            int mitigated = rawDamage - Armor;
            int finalDamage = mitigated < 1 ? 1 : mitigated;
            CurrentHP -= finalDamage;
            if (CurrentHP < 0) CurrentHP = 0;
            return finalDamage;
        }

        public void WriteBackHP()
        {
            Instance.CurrentHP = CurrentHP;
        }
    }
}

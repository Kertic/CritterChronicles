using System;

namespace AutobattlerSample.Data
{
    [Serializable]
    public class UnitInstance
    {
        public UnitData BaseData;
        public int BonusHP;
        public int BonusArmor;
        public int BonusAttack;
        public int CurrentHP;

        public string DisplayName => BaseData != null ? BaseData.DisplayName : "Unit";
        public int EffectiveMaxHP => (BaseData != null ? BaseData.MaxHP : 0) + BonusHP;
        public int EffectiveArmor => (BaseData != null ? BaseData.Armor : 0) + BonusArmor;
        public int EffectiveAttack => (BaseData != null ? BaseData.AttackDamage : 0) + BonusAttack;
        public bool IsAlive => CurrentHP > 0;

        public UnitInstance() { }

        public UnitInstance(UnitData data)
        {
            BaseData = data;
            BonusHP = 0;
            BonusArmor = 0;
            BonusAttack = 0;
            CurrentHP = EffectiveMaxHP;
        }

        public void FullHeal()
        {
            CurrentHP = EffectiveMaxHP;
        }

        public UnitInstance Clone()
        {
            return new UnitInstance
            {
                BaseData = BaseData,
                BonusHP = BonusHP,
                BonusArmor = BonusArmor,
                BonusAttack = BonusAttack,
                CurrentHP = CurrentHP
            };
        }
    }
}


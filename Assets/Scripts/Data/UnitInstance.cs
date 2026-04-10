using System;

namespace AutobattlerSample.Data
{
    [Serializable]
    public class UnitInstance
    {
        public UnitData BaseData;
        public int BonusHP;
        public int BonusCooldownReduction;
        public int Shield;
        public int Rank = 1;
        public int CurrentHP;

        public string DisplayName => BaseData != null ? BaseData.DisplayName : "Unit";
        public int EffectiveMaxHP => (BaseData != null ? BaseData.MaxHP + BaseData.RankUpBonusHP * (Rank - 1) : 0) + BonusHP;

        public int EffectiveAttackDamage
        {
            get
            {
                if (BaseData == null) return 0;
                float scale = BaseData.DamageScalePerRank;
                if (scale <= 0f) scale = 1f;
                return (int)(BaseData.BaseAttackDamage * Math.Pow(scale, Rank - 1));
            }
        }

        public int EffectiveCooldown
        {
            get
            {
                if (BaseData == null) return 1;
                int cd = BaseData.AttackCooldown - BonusCooldownReduction;
                return cd < 1 ? 1 : cd;
            }
        }

        public PassiveType Passive => BaseData != null ? BaseData.Passive : PassiveType.None;
        public bool IsAlive => CurrentHP > 0;

        public UnitInstance() { }

        public UnitInstance(UnitData data)
        {
            BaseData = data;
            BonusHP = 0;
            BonusCooldownReduction = 0;
            Shield = 0;
            Rank = 1;
            CurrentHP = EffectiveMaxHP;
        }

        public void FullHeal()
        {
            CurrentHP = EffectiveMaxHP;
        }

        public void RankUp()
        {
            Rank++;
            CurrentHP = EffectiveMaxHP;
        }

        public UnitInstance Clone()
        {
            return new UnitInstance
            {
                BaseData = BaseData,
                BonusHP = BonusHP,
                BonusCooldownReduction = BonusCooldownReduction,
                Shield = Shield,
                Rank = Rank,
                CurrentHP = CurrentHP
            };
        }
    }
}

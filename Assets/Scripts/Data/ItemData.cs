using UnityEngine;

namespace AutobattlerSample.Data
{
    public enum StatType
    {
        HP,
        Armor,
        AttackDamage
    }

    [CreateAssetMenu(menuName = "Autobattler/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string Name;
        public StatType Stat;
        public int Amount;

        public void ApplyTo(UnitInstance unit)
        {
            switch (Stat)
            {
                case StatType.HP:
                    unit.BonusHP += Amount;
                    unit.CurrentHP += Amount;
                    break;
                case StatType.Armor:
                    unit.BonusArmor += Amount;
                    break;
                case StatType.AttackDamage:
                    unit.BonusAttack += Amount;
                    break;
            }
        }

        public string StatName
        {
            get
            {
                switch (Stat)
                {
                    case StatType.HP: return "HP";
                    case StatType.Armor: return "Armor";
                    case StatType.AttackDamage: return "Attack";
                    default: return "???";
                }
            }
        }

        public override string ToString()
        {
            return $"{Name}\n+{Amount} {StatName}";
        }
    }
}

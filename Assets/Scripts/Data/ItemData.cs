using UnityEngine;

namespace AutobattlerSample.Data
{
    [CreateAssetMenu(menuName = "Autobattler/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string Name;
        public ItemType Type;
        public int Amount;

        public void ApplyTo(UnitInstance unit)
        {
            switch (Type)
            {
                case ItemType.MaxHP:
                    unit.BonusHP += Amount;
                    unit.CurrentHP += Amount;
                    break;
                case ItemType.CooldownReduction:
                    unit.BonusCooldownReduction += Amount;
                    break;
                case ItemType.Shield:
                    unit.Shield += Amount;
                    break;
            }
        }

        public string TypeName
        {
            get
            {
                switch (Type)
                {
                    case ItemType.MaxHP: return "Max HP";
                    case ItemType.CooldownReduction: return "CD Reduction";
                    case ItemType.Shield: return "Shield";
                    default: return "???";
                }
            }
        }

        public override string ToString()
        {
            string sign = Type == ItemType.CooldownReduction ? "-" : "+";
            return $"{Name}\n{sign}{Amount} {TypeName}";
        }
    }
}

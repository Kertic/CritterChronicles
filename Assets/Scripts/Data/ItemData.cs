using UnityEngine;

namespace AutobattlerSample.Data
{
    [CreateAssetMenu(menuName = "Autobattler/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string Name;
        public ItemType Type;
        public int Amount;

        [Header("Action Grant (when Type = ActionGrant)")]
        public ActionType GrantedActionType;
        public int GrantedActionAmount;
        public int GrantedActionCooldown = 3;

        public void ApplyTo(UnitInstance unit)
        {
            string tag = GetInstanceID().ToString();
            switch (Type)
            {
                case ItemType.MaxHP:
                    unit.BonusHP += Amount;
                    unit.CurrentHP += Amount;
                    break;
                case ItemType.CooldownReduction:
                    unit.BonusCooldownReduction += Amount;
                    unit.RebuildActions();
                    break;
                case ItemType.Shield:
                    // Shield items now grant a ShieldSelf action instead of flat shield
                    unit.AddAction(new ActionData(Name, ActionType.ShieldSelf, Amount, 4) { SourceTag = tag });
                    break;
                case ItemType.ActionGrant:
                    unit.AddAction(new ActionData(Name, GrantedActionType, GrantedActionAmount, GrantedActionCooldown) { SourceTag = tag });
                    break;
            }
            if (!unit.EquippedItems.Contains(this))
                unit.EquippedItems.Add(this);
        }

        public void UnapplyFrom(UnitInstance unit)
        {
            switch (Type)
            {
                case ItemType.MaxHP:
                    unit.BonusHP -= Amount;
                    if (unit.CurrentHP > unit.EffectiveMaxHP)
                        unit.CurrentHP = unit.EffectiveMaxHP;
                    break;
                case ItemType.CooldownReduction:
                    unit.BonusCooldownReduction -= Amount;
                    if (unit.BonusCooldownReduction < 0) unit.BonusCooldownReduction = 0;
                    unit.RebuildActions();
                    break;
                case ItemType.Shield:
                case ItemType.ActionGrant:
                    // Remove the action granted by this item (matched by SourceTag for uniqueness)
                    string tag = GetInstanceID().ToString();
                    for (int i = unit.Actions.Count - 1; i >= 0; i--)
                    {
                        if (unit.Actions[i].Data != null && unit.Actions[i].Data.SourceTag == tag)
                        {
                            unit.Actions.RemoveAt(i);
                            break;
                        }
                    }
                    break;
            }
            unit.EquippedItems.Remove(this);
        }

        public string TypeName
        {
            get
            {
                switch (Type)
                {
                    case ItemType.MaxHP: return "Max HP";
                    case ItemType.CooldownReduction: return "CD Reduction";
                    case ItemType.Shield: return "Shield Action";
                    case ItemType.ActionGrant:
                        switch (GrantedActionType)
                        {
                            case ActionType.Attack: return "Attack Action";
                            case ActionType.ShieldSelf: return "Shield Action";
                            case ActionType.HealSelf: return "Heal Self Action";
                            case ActionType.HealFront: return "Heal Front Action";
                            case ActionType.HealAll: return "Heal All Action";
                            default: return "Action";
                        }
                    default: return "???";
                }
            }
        }

        public override string ToString()
        {
            if (Type == ItemType.ActionGrant)
                return $"{Name}\n{GrantedActionType}: {GrantedActionAmount} (CD:{GrantedActionCooldown})";
            string sign = Type == ItemType.CooldownReduction ? "-" : "+";
            return $"{Name}\n{sign}{Amount} {TypeName}";
        }
    }
}

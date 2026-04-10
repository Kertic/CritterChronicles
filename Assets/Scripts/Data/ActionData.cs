using System;

namespace AutobattlerSample.Data
{
    [Serializable]
    public class ActionData
    {
        public string DisplayName;
        public ActionType Type;
        public int Amount;   // damage, heal, or shield value
        public int Cooldown;
        /// <summary>Unique tag identifying the source that created this action (e.g. item instance ID).
        /// Used for reliable removal instead of matching by display name.</summary>
        public string SourceTag;

        public ActionData() { }

        public ActionData(string displayName, ActionType type, int amount, int cooldown)
        {
            DisplayName = displayName;
            Type = type;
            Amount = amount;
            Cooldown = cooldown;
        }

        public string ShortLabel
        {
            get
            {
                switch (Type)
                {
                    case ActionType.Attack: return $"ATK:{Amount}";
                    case ActionType.ShieldSelf: return $"SH:{Amount}";
                    case ActionType.HealSelf: return $"HS:{Amount}";
                    case ActionType.HealFront: return $"HF:{Amount}";
                    case ActionType.HealAll: return $"HA:{Amount}";
                    default: return DisplayName;
                }
            }
        }

        public ActionData Clone()
        {
            return new ActionData(DisplayName, Type, Amount, Cooldown) { SourceTag = SourceTag };
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;

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
        public int Position; // 0 = front, higher = further back
        public bool IsActive = true; // true = in party, false = at camp

        public List<ActionInstance> Actions = new();
        public List<ItemData> EquippedItems = new();

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

        public int SlotCost => BaseData != null ? BaseData.SlotCost : 1;

        public UnitInstance() { }

        public UnitInstance(UnitData data)
        {
            BaseData = data;
            BonusHP = 0;
            BonusCooldownReduction = 0;
            Shield = 0;
            Rank = 1;
            CurrentHP = EffectiveMaxHP;
            Position = 0;
            IsActive = true;
            RebuildActions();
        }

        public void RebuildActions()
        {
            if (BaseData == null) return;

            var itemActions = new List<ActionInstance>();
            var defaultNames = new HashSet<string>();
            foreach (var ad in BaseData.GetDefaultActions())
                defaultNames.Add(ad.DisplayName);

            foreach (var ai in Actions)
            {
                if (ai.Data != null && !defaultNames.Contains(ai.Data.DisplayName))
                    itemActions.Add(ai);
            }

            Actions.Clear();
            int priority = 0;
            foreach (var ad in BaseData.GetDefaultActions())
            {
                var cloned = ad.Clone();
                if (cloned.Type == ActionType.Attack && Rank > 1)
                {
                    float scale = BaseData.DamageScalePerRank;
                    if (scale <= 0f) scale = 1f;
                    cloned.Amount = (int)(cloned.Amount * Math.Pow(scale, Rank - 1));
                }
                if (cloned.Type == ActionType.Attack && BonusCooldownReduction > 0)
                {
                    cloned.Cooldown = Math.Max(1, cloned.Cooldown - BonusCooldownReduction);
                }
                Actions.Add(new ActionInstance(cloned, priority++));
            }

            foreach (var ia in itemActions)
            {
                ia.Priority = priority++;
                Actions.Add(ia);
            }
        }

        public void AddAction(ActionData actionData)
        {
            int maxPriority = Actions.Count > 0 ? Actions.Max(a => a.Priority) + 1 : 0;
            Actions.Add(new ActionInstance(actionData, maxPriority));
        }

        public void FullHeal()
        {
            CurrentHP = EffectiveMaxHP;
        }

        public void RankUp()
        {
            Rank++;
            CurrentHP = EffectiveMaxHP;
            RebuildActions();
        }

        public UnitInstance Clone()
        {
            var clone = new UnitInstance
            {
                BaseData = BaseData,
                BonusHP = BonusHP,
                BonusCooldownReduction = BonusCooldownReduction,
                Shield = Shield,
                Rank = Rank,
                CurrentHP = CurrentHP,
                Position = Position,
                IsActive = IsActive,
                Actions = new List<ActionInstance>(),
                EquippedItems = new List<ItemData>(EquippedItems)
            };
            foreach (var action in Actions)
                clone.Actions.Add(action.Clone());
            return clone;
        }
    }
}

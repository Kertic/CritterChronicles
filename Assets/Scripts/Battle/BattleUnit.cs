using System.Collections.Generic;
using AutobattlerSample.Data;

namespace AutobattlerSample.Battle
{
    public class BattleUnit
    {
        public UnitInstance Instance { get; }
        public bool IsAlly { get; }
        public int CurrentHP { get; set; }
        public int Shield { get; set; }
        public int Position { get; set; }
        public bool IsAlive => CurrentHP > 0;

        public List<ActionInstance> Actions { get; } = new();

        public string DisplayName => Instance.DisplayName;
        public int MaxHP => Instance.EffectiveMaxHP;
        public int Rank => Instance.Rank;
        public PassiveType Passive => Instance.Passive;

        // Backward compat helpers
        public int AttackDamage => Instance.EffectiveAttackDamage;

        public BattleUnit(UnitInstance instance, bool isAlly)
        {
            Instance = instance;
            IsAlly = isAlly;
            CurrentHP = instance.CurrentHP;
            Shield = instance.Shield;
            Position = instance.Position;

            // Clone actions from instance
            foreach (var ai in instance.Actions)
                Actions.Add(ai.Clone());
        }

        public void TickAllCooldowns()
        {
            foreach (var action in Actions)
                action.TickCooldown();
        }

        /// <summary>
        /// Returns the highest-priority (lowest Priority number) action that is ready.
        /// Returns null if all actions are on cooldown.
        /// </summary>
        public ActionInstance GetNextReadyAction()
        {
            ActionInstance best = null;
            foreach (var a in Actions)
            {
                if (!a.IsReady) continue;
                if (best == null || a.Priority < best.Priority)
                    best = a;
            }
            return best;
        }

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

        public void AddShield(int amount)
        {
            Shield += amount;
        }

        /// <summary>
        /// If unit has HasteOnHeal passive, haste its first Attack action by amount.
        /// Returns (triggered, actionName, cdBefore, cdAfter).
        /// </summary>
        public (bool triggered, string actionName, int cdBefore, int cdAfter) TriggerHasteOnHeal(int hasteAmount)
        {
            if (Passive != PassiveType.HasteOnHeal) return (false, null, 0, 0);
            foreach (var a in Actions)
            {
                if (a.Type == ActionType.Attack)
                {
                    int before = a.CurrentCooldown;
                    a.Haste(hasteAmount);
                    return (true, a.DisplayName, before, a.CurrentCooldown);
                }
            }
            return (false, null, 0, 0);
        }

        public void WriteBackHP()
        {
            Instance.CurrentHP = CurrentHP;
            Instance.Shield = Shield;
        }
    }
}

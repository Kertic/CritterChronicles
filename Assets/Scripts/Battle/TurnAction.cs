using System.Collections.Generic;
using AutobattlerSample.Data;

namespace AutobattlerSample.Battle
{
    public class TurnAction
    {
        public BattleUnit Attacker;
        public BattleUnit Target;
        public ActionType UsedActionType;
        public string ActionName;
        public int RawDamage;
        public int DamageDealt;
        public int ShieldAbsorbed;
        public int TargetHPBefore;
        public int TargetHPAfter;
        public int TargetShieldBefore;
        public int TargetShieldAfter;
        public bool KilledTarget;
        public bool WasOnCooldown;
        public int AttackerCooldownAfter;
        public int AttackerRank;
        public int LifestealHealed;
        public int HealAmount;
        public BattleUnit HealTarget;
        public int ShieldGained;
        public int TurnNumber;

        /// <summary>Per-unit heal results for HealAll actions.</summary>
        public List<(BattleUnit unit, int healed)> HealAllResults;

        // Passive trigger tracking
        public bool LifestealTriggered;
        public bool HasteTriggered;
        public string HasteUnitName;
        public string HasteActionName;
        public int HasteCooldownBefore;
        public int HasteCooldownAfter;

        /// <summary>All haste triggers for HealAll (when multiple allies have HasteOnHeal).</summary>
        public List<(string unitName, string actionName, int cdBefore, int cdAfter)> HasteAllTriggers;
    }
}

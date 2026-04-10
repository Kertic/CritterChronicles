namespace AutobattlerSample.Battle
{
    public class TurnAction
    {
        public BattleUnit Attacker;
        public BattleUnit Target;
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
    }
}

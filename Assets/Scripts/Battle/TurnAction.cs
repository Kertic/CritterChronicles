namespace AutobattlerSample.Battle
{
    public class TurnAction
    {
        public BattleUnit Attacker;
        public BattleUnit Target;
        public int RawDamage;
        public int TargetArmor;
        public int DamageDealt;
        public int TargetHPBefore;
        public int TargetHPAfter;
        public bool KilledTarget;
    }
}


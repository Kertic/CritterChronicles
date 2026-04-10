using UnityEngine;

namespace AutobattlerSample.Data
{
    [CreateAssetMenu(menuName = "Autobattler/Unit Data")]
    public class UnitData : ScriptableObject
    {
        public string UnitId;
        public string DisplayName;
        public int MaxHP = 20;
        public int Armor = 0;
        public int AttackDamage = 4;
    }
}

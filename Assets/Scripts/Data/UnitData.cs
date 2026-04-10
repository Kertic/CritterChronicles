using System.Collections.Generic;
using UnityEngine;

namespace AutobattlerSample.Data
{
    [CreateAssetMenu(menuName = "Autobattler/Unit Data")]
    public class UnitData : ScriptableObject
    {
        public string UnitId;
        public string DisplayName;
        public CreatureType Type;
        public CreatureSize Size;
        public int MaxHP = 20;
        public List<string> Attributes = new();

        [Header("Attack Action")]
        public int BaseAttackDamage = 5;
        public int AttackCooldown = 3;

        [Header("Passive")]
        public PassiveType Passive = PassiveType.None;

        [Header("Rank Up")]
        public int RankUpBonusHP = 10;
        public float DamageScalePerRank = 1f;
    }
}

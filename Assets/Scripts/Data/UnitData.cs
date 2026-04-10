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

        [Header("Default Actions")]
        public List<ActionData> DefaultActions = new();

        [Header("Passive")]
        public PassiveType Passive = PassiveType.None;

        [Header("Rank Up")]
        public int RankUpBonusHP = 10;
        public float DamageScalePerRank = 1f;

        /// <summary>
        /// Returns the default actions list. If empty, auto-generates one Attack action
        /// from BaseAttackDamage/AttackCooldown for backward compatibility.
        /// </summary>
        public List<ActionData> GetDefaultActions()
        {
            if (DefaultActions != null && DefaultActions.Count > 0)
                return DefaultActions;
            return new List<ActionData>
            {
                new ActionData(DisplayName + " Attack", ActionType.Attack, BaseAttackDamage, AttackCooldown)
            };
        }

        public int SlotCost => Size == CreatureSize.Large ? 2 : 1;
    }
}

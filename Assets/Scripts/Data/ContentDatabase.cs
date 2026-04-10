using System.Collections.Generic;
using UnityEngine;

namespace AutobattlerSample.Data
{
    [CreateAssetMenu(menuName = "Autobattler/Content Database")]
    public class ContentDatabase : ScriptableObject
    {
        public List<UnitData> PlayerUnits = new();
        public List<UnitData> StartingUnits = new();
        public List<UnitData> EnemyUnits = new();
        public List<UnitData> BossUnits = new();
        public List<UnitData> ShopUnits = new();
        public List<ItemData> RewardItems = new();
        public List<string> FloorEncounterNames = new() { "Scouts", "Raiders", "Warband", "Horde", "Legion" };
    }
}

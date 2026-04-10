using System.Collections.Generic;
using AutobattlerSample.Data;
using UnityEngine;

namespace AutobattlerSample.Core
{
    public class ContentGenerator
    {
        private readonly ContentDatabase _database;

        public ContentGenerator(ContentDatabase database)
        {
            _database = database;
        }

        public List<UnitInstance> GeneratePlayerTeam()
        {
            var team = new List<UnitInstance>();
            if (_database == null || _database.PlayerUnits == null)
                return team;

            foreach (var unit in _database.PlayerUnits)
            {
                if (unit != null)
                    team.Add(new UnitInstance(unit));
            }
            return team;
        }

        public EncounterData GenerateEncounter(int floor, int nodeIndex)
        {
            int enemyCount = Mathf.Clamp(2 + floor / 2, 2, 4);
            float scaleFactor = 1f + floor * 0.15f;

            var encounter = new EncounterData
            {
                DisplayName = GetFloorEncounterName(floor) + $" #{nodeIndex + 1}",
                IsBoss = false
            };

            for (int i = 0; i < enemyCount; i++)
            {
                var template = GetRandomUnit(_database != null ? _database.EnemyUnits : null);
                if (template != null)
                    encounter.Enemies.Add(CreateScaledInstance(template, scaleFactor));
            }
            return encounter;
        }

        public EncounterData GenerateEliteEncounter(int floor, int nodeIndex)
        {
            var encounter = GenerateEncounter(floor, nodeIndex);
            encounter.DisplayName = "Elite: " + encounter.DisplayName;
            foreach (var enemy in encounter.Enemies)
            {
                enemy.BonusHP += 10;
                enemy.FullHeal();
            }
            return encounter;
        }

        public EncounterData GenerateBossEncounter()
        {
            var template = GetRandomUnit(_database != null ? _database.BossUnits : null);
            var encounter = new EncounterData
            {
                DisplayName = template != null ? template.DisplayName + "'s Lair" : "Boss Lair",
                IsBoss = true
            };

            if (template != null)
                encounter.Enemies.Add(new UnitInstance(template));

            int minionCount = Random.Range(1, 3);
            for (int i = 0; i < minionCount; i++)
            {
                var minionTemplate = GetRandomUnit(_database != null ? _database.EnemyUnits : null);
                if (minionTemplate != null)
                    encounter.Enemies.Add(CreateScaledInstance(minionTemplate, 1.3f));
            }
            return encounter;
        }

        public List<ItemData> GenerateItemRewards(int count = 3)
        {
            var items = new List<ItemData>();
            if (_database == null || _database.RewardItems == null || _database.RewardItems.Count == 0)
                return items;

            var used = new HashSet<int>();
            for (int i = 0; i < count && used.Count < _database.RewardItems.Count; i++)
            {
                int idx;
                do { idx = Random.Range(0, _database.RewardItems.Count); }
                while (used.Contains(idx));

                used.Add(idx);
                var item = _database.RewardItems[idx];
                if (item != null)
                    items.Add(item);
            }
            return items;
        }

        /// <summary>
        /// Generate a mix of shop offerings: some units and some items.
        /// Returns (units, items) tuple.
        /// </summary>
        public (List<UnitData> units, List<ItemData> items) GenerateShopOfferings(int totalCount = 4)
        {
            var units = new List<UnitData>();
            var items = new List<ItemData>();

            // Offer 1-2 units from ShopUnits
            if (_database != null && _database.ShopUnits != null && _database.ShopUnits.Count > 0)
            {
                int unitCount = Mathf.Min(2, _database.ShopUnits.Count);
                var usedUnits = new HashSet<int>();
                for (int i = 0; i < unitCount; i++)
                {
                    int idx;
                    int attempts = 0;
                    do { idx = Random.Range(0, _database.ShopUnits.Count); attempts++; }
                    while (usedUnits.Contains(idx) && attempts < 20);
                    if (!usedUnits.Contains(idx))
                    {
                        usedUnits.Add(idx);
                        var u = _database.ShopUnits[idx];
                        if (u != null) units.Add(u);
                    }
                }
            }

            // Fill remaining with items
            int itemCount = totalCount - units.Count;
            var rewardItems = GenerateItemRewards(itemCount);
            items.AddRange(rewardItems);

            return (units, items);
        }

        private string GetFloorEncounterName(int floor)
        {
            if (_database == null || _database.FloorEncounterNames == null || _database.FloorEncounterNames.Count == 0)
                return "Encounter";
            return _database.FloorEncounterNames[Mathf.Clamp(floor, 0, _database.FloorEncounterNames.Count - 1)];
        }

        private static UnitData GetRandomUnit(IReadOnlyList<UnitData> units)
        {
            if (units == null || units.Count == 0)
                return null;

            for (int attempt = 0; attempt < units.Count; attempt++)
            {
                var candidate = units[Random.Range(0, units.Count)];
                if (candidate != null)
                    return candidate;
            }
            return null;
        }

        private static UnitInstance CreateScaledInstance(UnitData template, float scaleFactor)
        {
            var unit = new UnitInstance(template);
            unit.BonusHP = Mathf.Max(0, Mathf.RoundToInt(template.MaxHP * scaleFactor) - template.MaxHP);
            unit.FullHeal();
            return unit;
        }
    }
}

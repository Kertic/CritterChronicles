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

        /// <summary>
        /// Generate a set of random critters for the player to pick from at start.
        /// Uses the narrow StartingUnits pool (falls back to PlayerUnits).
        /// </summary>
        public List<UnitData> GenerateStartingPicks(int count = 5)
        {
            var picks = new List<UnitData>();
            if (_database == null) return picks;

            // Use the narrow starting pool only
            var pool = new List<UnitData>();
            if (_database.StartingUnits != null && _database.StartingUnits.Count > 0)
                pool.AddRange(_database.StartingUnits);
            else if (_database.PlayerUnits != null)
                pool.AddRange(_database.PlayerUnits);

            var used = new HashSet<int>();
            for (int i = 0; i < count && used.Count < pool.Count; i++)
            {
                int idx;
                int attempts = 0;
                do { idx = Random.Range(0, pool.Count); attempts++; }
                while (used.Contains(idx) && attempts < 30);
                if (!used.Contains(idx) && pool[idx] != null)
                {
                    used.Add(idx);
                    picks.Add(pool[idx]);
                }
            }
            return picks;
        }

        public EncounterData GenerateEncounter(int floor, int nodeIndex)
        {
            int enemyCount = Mathf.Clamp(2 + floor / 2, 2, 7);
            float scaleFactor = 1f + floor * 0.25f;

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
                enemy.BonusHP += 10 + floor * 3;
                enemy.FullHeal();
            }
            return encounter;
        }

        public EncounterData GenerateBossEncounter(int floor = 0)
        {
            var template = GetRandomUnit(_database != null ? _database.BossUnits : null);
            float bossScale = 1f + floor * 0.2f;
            var encounter = new EncounterData
            {
                DisplayName = template != null ? template.DisplayName + "'s Lair" : "Boss Lair",
                IsBoss = true
            };

            if (template != null)
                encounter.Enemies.Add(CreateScaledInstance(template, bossScale));

            int minionCount = Random.Range(2, 4);
            float minionScale = 1.3f + floor * 0.15f;
            for (int i = 0; i < minionCount; i++)
            {
                var minionTemplate = GetRandomUnit(_database != null ? _database.EnemyUnits : null);
                if (minionTemplate != null)
                    encounter.Enemies.Add(CreateScaledInstance(minionTemplate, minionScale));
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

        public (List<UnitData> units, List<ItemData> items) GenerateShopOfferings(int totalCount = 4)
        {
            var units = new List<UnitData>();
            var items = new List<ItemData>();

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

using System.Collections.Generic;
using AutobattlerSample.Data;
using UnityEngine;

namespace AutobattlerSample.Core
{
    public static class ContentGenerator
    {
        private static readonly string[][] PlayerTemplates =
        {
            new[] { "knight", "Knight", "30", "2", "6" },
            new[] { "ranger", "Ranger", "18", "0", "8" },
            new[] { "cleric", "Cleric", "24", "1", "4" },
        };

        private static readonly string[][] EnemyTemplates =
        {
            new[] { "goblin_grunt", "Goblin Grunt", "10", "0", "3" },
            new[] { "goblin_archer", "Goblin Archer", "8", "0", "5" },
            new[] { "goblin_shaman", "Goblin Shaman", "12", "1", "4" },
            new[] { "goblin_brute", "Goblin Brute", "18", "2", "5" },
            new[] { "skeleton", "Skeleton", "14", "1", "4" },
            new[] { "skeleton_archer", "Skeleton Archer", "10", "0", "6" },
        };

        private static readonly string[][] BossTemplates =
        {
            new[] { "goblin_warchief", "Goblin Warchief", "45", "3", "8" },
            new[] { "necromancer", "Necromancer", "40", "2", "10" },
            new[] { "ogre", "Ogre", "60", "4", "7" },
        };

        private static UnitData CreateTemplate(string[] data)
        {
            var ud = ScriptableObject.CreateInstance<UnitData>();
            ud.UnitId = data[0];
            ud.DisplayName = data[1];
            ud.MaxHP = int.Parse(data[2]);
            ud.Armor = int.Parse(data[3]);
            ud.AttackDamage = int.Parse(data[4]);
            ud.name = data[1];
            return ud;
        }

        public static List<UnitInstance> GeneratePlayerTeam()
        {
            var team = new List<UnitInstance>();
            foreach (var t in PlayerTemplates)
            {
                var data = CreateTemplate(t);
                team.Add(new UnitInstance(data));
            }
            return team;
        }

        public static EncounterData GenerateEncounter(int floor, int nodeIndex)
        {
            int enemyCount = Mathf.Clamp(2 + floor / 2, 2, 4);
            float scaleFactor = 1f + floor * 0.15f;

            string[] floorNames = { "Scouts", "Raiders", "Warband", "Horde", "Legion" };
            var encounter = new EncounterData
            {
                DisplayName = floorNames[Mathf.Clamp(floor, 0, floorNames.Length - 1)] + $" #{nodeIndex + 1}",
                IsBoss = false
            };

            for (int i = 0; i < enemyCount; i++)
            {
                var template = EnemyTemplates[Random.Range(0, EnemyTemplates.Length)];
                var data = CreateTemplate(template);
                data.MaxHP = Mathf.RoundToInt(data.MaxHP * scaleFactor);
                data.AttackDamage = Mathf.RoundToInt(data.AttackDamage * scaleFactor);
                encounter.Enemies.Add(new UnitInstance(data));
            }

            return encounter;
        }

        public static EncounterData GenerateEliteEncounter(int floor, int nodeIndex)
        {
            var encounter = GenerateEncounter(floor, nodeIndex);
            encounter.DisplayName = "Elite: " + encounter.DisplayName;
            foreach (var enemy in encounter.Enemies)
            {
                enemy.BonusHP += 5;
                enemy.BonusAttack += 2;
                enemy.BonusArmor += 1;
                enemy.FullHeal();
            }
            return encounter;
        }

        public static EncounterData GenerateBossEncounter()
        {
            var template = BossTemplates[Random.Range(0, BossTemplates.Length)];
            var encounter = new EncounterData
            {
                DisplayName = template[1] + "'s Lair",
                IsBoss = true
            };

            var bossData = CreateTemplate(template);
            encounter.Enemies.Add(new UnitInstance(bossData));

            int minionCount = Random.Range(1, 3);
            for (int i = 0; i < minionCount; i++)
            {
                var minionTemplate = EnemyTemplates[Random.Range(0, EnemyTemplates.Length)];
                var minionData = CreateTemplate(minionTemplate);
                minionData.MaxHP = Mathf.RoundToInt(minionData.MaxHP * 1.3f);
                encounter.Enemies.Add(new UnitInstance(minionData));
            }

            return encounter;
        }

        public static List<ItemData> GenerateItemRewards(int count = 3)
        {
            var items = new List<ItemData>();
            string[][] itemPool =
            {
                new[] { "Iron Shield", "Armor", "2" },
                new[] { "Health Potion", "HP", "8" },
                new[] { "Whetstone", "AttackDamage", "3" },
                new[] { "Chain Mail", "Armor", "3" },
                new[] { "Vitality Ring", "HP", "12" },
                new[] { "War Axe", "AttackDamage", "5" },
                new[] { "Leather Cap", "Armor", "1" },
                new[] { "Bread Loaf", "HP", "5" },
                new[] { "Sharp Dagger", "AttackDamage", "2" },
            };

            var used = new HashSet<int>();
            for (int i = 0; i < count && used.Count < itemPool.Length; i++)
            {
                int idx;
                do { idx = Random.Range(0, itemPool.Length); } while (used.Contains(idx));
                used.Add(idx);

                var data = itemPool[idx];
                StatType stat;
                switch (data[1])
                {
                    case "Armor": stat = StatType.Armor; break;
                    case "AttackDamage": stat = StatType.AttackDamage; break;
                    default: stat = StatType.HP; break;
                }

                items.Add(new ItemData
                {
                    Name = data[0],
                    Stat = stat,
                    Amount = int.Parse(data[2])
                });
            }

            return items;
        }
    }
}


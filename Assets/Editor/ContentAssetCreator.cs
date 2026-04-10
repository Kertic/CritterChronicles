using System.Collections.Generic;
using System.IO;
using AutobattlerSample.Data;
using UnityEditor;
using UnityEngine;

namespace AutobattlerSample.Editor
{
    public static class ContentAssetCreator
    {
        private const string ContentRoot = "Assets/Resources/Content";
        private const string UnitsPath = ContentRoot + "/Units";
        private const string ItemsPath = ContentRoot + "/Items";
        private const string DatabasePath = ContentRoot + "/DefaultContentDatabase.asset";

        public static void CreateDefaultContentAssets()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(ContentRoot);
            EnsureFolder(UnitsPath);
            EnsureFolder(ItemsPath);

            var database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ContentDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.PlayerUnits = new List<UnitData>
            {
                CreateOrUpdateUnit("Knight", "knight", "Knight", 30, 2, 6),
                CreateOrUpdateUnit("Ranger", "ranger", "Ranger", 18, 0, 8),
                CreateOrUpdateUnit("Cleric", "cleric", "Cleric", 24, 1, 4)
            };

            database.EnemyUnits = new List<UnitData>
            {
                CreateOrUpdateUnit("GoblinGrunt", "goblin_grunt", "Goblin Grunt", 10, 0, 3),
                CreateOrUpdateUnit("GoblinArcher", "goblin_archer", "Goblin Archer", 8, 0, 5),
                CreateOrUpdateUnit("GoblinShaman", "goblin_shaman", "Goblin Shaman", 12, 1, 4),
                CreateOrUpdateUnit("GoblinBrute", "goblin_brute", "Goblin Brute", 18, 2, 5),
                CreateOrUpdateUnit("Skeleton", "skeleton", "Skeleton", 14, 1, 4),
                CreateOrUpdateUnit("SkeletonArcher", "skeleton_archer", "Skeleton Archer", 10, 0, 6)
            };

            database.BossUnits = new List<UnitData>
            {
                CreateOrUpdateUnit("GoblinWarchief", "goblin_warchief", "Goblin Warchief", 45, 3, 8),
                CreateOrUpdateUnit("Necromancer", "necromancer", "Necromancer", 40, 2, 10),
                CreateOrUpdateUnit("Ogre", "ogre", "Ogre", 60, 4, 7)
            };

            database.RewardItems = new List<ItemData>
            {
                CreateOrUpdateItem("IronShield", "Iron Shield", StatType.Armor, 2),
                CreateOrUpdateItem("HealthPotion", "Health Potion", StatType.HP, 8),
                CreateOrUpdateItem("Whetstone", "Whetstone", StatType.AttackDamage, 3),
                CreateOrUpdateItem("ChainMail", "Chain Mail", StatType.Armor, 3),
                CreateOrUpdateItem("VitalityRing", "Vitality Ring", StatType.HP, 12),
                CreateOrUpdateItem("WarAxe", "War Axe", StatType.AttackDamage, 5),
                CreateOrUpdateItem("LeatherCap", "Leather Cap", StatType.Armor, 1),
                CreateOrUpdateItem("BreadLoaf", "Bread Loaf", StatType.HP, 5),
                CreateOrUpdateItem("SharpDagger", "Sharp Dagger", StatType.AttackDamage, 2)
            };

            database.FloorEncounterNames = new List<string> { "Scouts", "Raiders", "Warband", "Horde", "Legion" };

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = database;
            Debug.Log("Created default content assets at Assets/Resources/Content.");
        }

        public static ContentDatabase EnsureDefaultContentDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            if (database == null)
            {
                CreateDefaultContentAssets();
                database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            }

            return database;
        }

        [MenuItem("Tools/CritterChronicles Sample/Remove Generated Content")]
        public static void ResetGeneratedContent()
        {
            if (!EditorUtility.DisplayDialog(
                "Remove Generated Content",
                "This will delete all generated content assets (units, items, database). Continue?",
                "Remove", "Cancel"))
                return;

            if (AssetDatabase.IsValidFolder(ContentRoot))
            {
                AssetDatabase.DeleteAsset(ContentRoot);
                AssetDatabase.Refresh();
                Debug.Log("[ContentAssetCreator] Generated content removed.");
            }
            else
            {
                Debug.Log("[ContentAssetCreator] Nothing to remove — content folder does not exist.");
            }
        }

        private static UnitData CreateOrUpdateUnit(string assetName, string unitId, string displayName, int maxHp, int armor, int attackDamage)
        {
            string path = Path.Combine(UnitsPath, assetName + ".asset").Replace("\\", "/");
            var asset = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (asset == null)
            {
                // Set fields before CreateAsset so the initial serialization includes them.
                asset = ScriptableObject.CreateInstance<UnitData>();
                asset.UnitId = unitId;
                asset.DisplayName = displayName;
                asset.MaxHP = maxHp;
                asset.Armor = armor;
                asset.AttackDamage = attackDamage;
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                asset.UnitId = unitId;
                asset.DisplayName = displayName;
                asset.MaxHP = maxHp;
                asset.Armor = armor;
                asset.AttackDamage = attackDamage;
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        private static ItemData CreateOrUpdateItem(string assetName, string itemName, StatType stat, int amount)
        {
            string path = Path.Combine(ItemsPath, assetName + ".asset").Replace("\\", "/");
            var asset = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (asset == null)
            {
                // Set fields before CreateAsset so the initial serialization includes them.
                asset = ScriptableObject.CreateInstance<ItemData>();
                asset.Name = itemName;
                asset.Stat = stat;
                asset.Amount = amount;
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                asset.Name = itemName;
                asset.Stat = stat;
                asset.Amount = amount;
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}

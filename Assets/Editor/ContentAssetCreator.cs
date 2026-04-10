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

            // --- Player Units ---
            database.PlayerUnits = new List<UnitData>
            {
                CreateOrUpdateUnit("Bear", "bear", "Bear",
                    CreatureType.Fur, CreatureSize.Large, 100,
                    baseAttack: 15, cooldown: 5, passive: PassiveType.None,
                    rankUpHP: 50, dmgScale: 1f, attributes: null),

                CreateOrUpdateUnit("Mouse", "mouse", "Mouse",
                    CreatureType.Fur, CreatureSize.Small, 5,
                    baseAttack: 5, cooldown: 3, passive: PassiveType.None,
                    rankUpHP: 10, dmgScale: 2f, attributes: null),

                CreateOrUpdateUnit("Bat", "bat", "Bat",
                    CreatureType.Skin, CreatureSize.Small, 10,
                    baseAttack: 10, cooldown: 6, passive: PassiveType.Lifesteal,
                    rankUpHP: 5, dmgScale: 1f, attributes: new List<string> { "flying" })
            };

            // --- Enemy Units ---
            database.EnemyUnits = new List<UnitData>
            {
                CreateOrUpdateUnit("GoblinGrunt", "goblin_grunt", "Goblin Grunt",
                    CreatureType.Skin, CreatureSize.Small, 15,
                    baseAttack: 3, cooldown: 2, passive: PassiveType.None,
                    rankUpHP: 5, dmgScale: 1f, attributes: null),

                CreateOrUpdateUnit("GoblinArcher", "goblin_archer", "Goblin Archer",
                    CreatureType.Skin, CreatureSize.Small, 10,
                    baseAttack: 5, cooldown: 3, passive: PassiveType.None,
                    rankUpHP: 4, dmgScale: 1f, attributes: null),

                CreateOrUpdateUnit("GoblinShaman", "goblin_shaman", "Goblin Shaman",
                    CreatureType.Skin, CreatureSize.Medium, 18,
                    baseAttack: 4, cooldown: 4, passive: PassiveType.None,
                    rankUpHP: 6, dmgScale: 1f, attributes: null),

                CreateOrUpdateUnit("GoblinBrute", "goblin_brute", "Goblin Brute",
                    CreatureType.Skin, CreatureSize.Large, 25,
                    baseAttack: 6, cooldown: 4, passive: PassiveType.None,
                    rankUpHP: 8, dmgScale: 1f, attributes: null),

                CreateOrUpdateUnit("Skeleton", "skeleton", "Skeleton",
                    CreatureType.Skin, CreatureSize.Medium, 20,
                    baseAttack: 4, cooldown: 3, passive: PassiveType.None,
                    rankUpHP: 5, dmgScale: 1f, attributes: null),

                CreateOrUpdateUnit("SkeletonArcher", "skeleton_archer", "Skeleton Archer",
                    CreatureType.Skin, CreatureSize.Small, 12,
                    baseAttack: 6, cooldown: 3, passive: PassiveType.None,
                    rankUpHP: 4, dmgScale: 1f, attributes: null)
            };

            // --- Boss Units ---
            database.BossUnits = new List<UnitData>
            {
                CreateOrUpdateUnit("GoblinWarchief", "goblin_warchief", "Goblin Warchief",
                    CreatureType.Skin, CreatureSize.Large, 80,
                    baseAttack: 8, cooldown: 3, passive: PassiveType.None,
                    rankUpHP: 20, dmgScale: 1f, attributes: null),

                CreateOrUpdateUnit("Necromancer", "necromancer", "Necromancer",
                    CreatureType.Skin, CreatureSize.Medium, 60,
                    baseAttack: 10, cooldown: 4, passive: PassiveType.Lifesteal,
                    rankUpHP: 15, dmgScale: 1f, attributes: null),

                CreateOrUpdateUnit("Ogre", "ogre", "Ogre",
                    CreatureType.Skin, CreatureSize.Large, 120,
                    baseAttack: 12, cooldown: 5, passive: PassiveType.None,
                    rankUpHP: 30, dmgScale: 1f, attributes: null)
            };

            // --- Shop Units (units available for purchase) ---
            database.ShopUnits = new List<UnitData>
            {
                database.PlayerUnits[0], // Bear
                database.PlayerUnits[1], // Mouse
                database.PlayerUnits[2], // Bat
                CreateOrUpdateUnit("Owl", "owl", "Owl",
                    CreatureType.Feather, CreatureSize.Small, 12,
                    baseAttack: 8, cooldown: 4, passive: PassiveType.None,
                    rankUpHP: 8, dmgScale: 1.5f, attributes: new List<string> { "flying" }),

                CreateOrUpdateUnit("Turtle", "turtle", "Turtle",
                    CreatureType.Scales, CreatureSize.Medium, 40,
                    baseAttack: 3, cooldown: 2, passive: PassiveType.None,
                    rankUpHP: 20, dmgScale: 1f, attributes: null)
            };

            // --- Reward Items ---
            database.RewardItems = new List<ItemData>
            {
                CreateOrUpdateItem("VitalityAmulet", "Vitality Amulet", ItemType.MaxHP, 15),
                CreateOrUpdateItem("HealthPotion", "Health Potion", ItemType.MaxHP, 8),
                CreateOrUpdateItem("HeartCrystal", "Heart Crystal", ItemType.MaxHP, 25),
                CreateOrUpdateItem("QuickCharm", "Quick Charm", ItemType.CooldownReduction, 1),
                CreateOrUpdateItem("HasteRing", "Haste Ring", ItemType.CooldownReduction, 1),
                CreateOrUpdateItem("IronShield", "Iron Shield", ItemType.Shield, 10),
                CreateOrUpdateItem("MagicBarrier", "Magic Barrier", ItemType.Shield, 20),
                CreateOrUpdateItem("BucklerToken", "Buckler Token", ItemType.Shield, 5),
                CreateOrUpdateItem("LifeGem", "Life Gem", ItemType.MaxHP, 12)
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

        private static UnitData CreateOrUpdateUnit(string assetName, string unitId, string displayName,
            CreatureType type, CreatureSize size, int maxHp,
            int baseAttack, int cooldown, PassiveType passive,
            int rankUpHP, float dmgScale, List<string> attributes)
        {
            string path = Path.Combine(UnitsPath, assetName + ".asset").Replace("\\", "/");
            var asset = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<UnitData>();
                asset.UnitId = unitId;
                asset.DisplayName = displayName;
                asset.Type = type;
                asset.Size = size;
                asset.MaxHP = maxHp;
                asset.BaseAttackDamage = baseAttack;
                asset.AttackCooldown = cooldown;
                asset.Passive = passive;
                asset.RankUpBonusHP = rankUpHP;
                asset.DamageScalePerRank = dmgScale;
                asset.Attributes = attributes ?? new List<string>();
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                asset.UnitId = unitId;
                asset.DisplayName = displayName;
                asset.Type = type;
                asset.Size = size;
                asset.MaxHP = maxHp;
                asset.BaseAttackDamage = baseAttack;
                asset.AttackCooldown = cooldown;
                asset.Passive = passive;
                asset.RankUpBonusHP = rankUpHP;
                asset.DamageScalePerRank = dmgScale;
                asset.Attributes = attributes ?? new List<string>();
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        private static ItemData CreateOrUpdateItem(string assetName, string itemName, ItemType type, int amount)
        {
            string path = Path.Combine(ItemsPath, assetName + ".asset").Replace("\\", "/");
            var asset = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ItemData>();
                asset.Name = itemName;
                asset.Type = type;
                asset.Amount = amount;
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                asset.Name = itemName;
                asset.Type = type;
                asset.Amount = amount;
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

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

using System.Collections.Generic;
using AutobattlerSample.Battle;
using AutobattlerSample.Data;
using AutobattlerSample.Map;
using AutobattlerSample.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AutobattlerSample.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Map Settings")]
        public int Floors = 15;
        public int Width = 5;
        public int Seed = 0;

        [Header("Content")]
        [SerializeField] private ContentDatabase contentDatabase;

        private RunState _runState;
        private MapScreen _mapScreen;
        private BattleScreen _battleScreen;
        private RewardScreen _rewardScreen;
        private RestScreen _restScreen;
        private ShopScreen _shopScreen;
        private ManageTeamScreen _manageTeamScreen;
        private StartPickScreen _startPickScreen;
        private InstructionScreen _instructionScreen;
        private BattleCombatManager _combatManager;
        private BattleResult _lastBattleResult;
        private ContentGenerator _contentGenerator;

        private void Start()
        {
            contentDatabase = ResolveContentDatabase();
            if (contentDatabase == null)
            {
                Debug.LogError("GameBootstrap requires a ContentDatabase asset. Assign one in the inspector or create Resources/Content/DefaultContentDatabase.");
                enabled = false;
                return;
            }

            _contentGenerator = new ContentGenerator(contentDatabase);
            EnsureEventSystem();
            BuildScreens();
            StartRun();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }

        private void BuildScreens()
        {
            _mapScreen = MapScreen.Create(transform, OnNodeSelected, OnManageTeam, ShowInstructions);
            _battleScreen = BattleScreen.Create(transform, OnBattleContinue);
            _rewardScreen = RewardScreen.Create(transform, OnRewardSelected);
            _restScreen = RestScreen.Create(transform, OnRestContinue);
            _shopScreen = ShopScreen.Create(transform, OnShopComplete);
            _manageTeamScreen = ManageTeamScreen.Create(transform, OnManageTeamDone);
            _startPickScreen = StartPickScreen.Create(transform, OnStartingPicksDone, ShowInstructions);
            _instructionScreen = InstructionScreen.Create(transform, () => { });

            var cmGo = new GameObject("CombatManager");
            cmGo.transform.SetParent(transform);
            _combatManager = cmGo.AddComponent<BattleCombatManager>();

            _battleScreen.SetCombatManager(_combatManager);
        }

        private void ShowInstructions()
        {
            _instructionScreen.Show();
        }

        public void StartRun()
        {
            _runState = new RunState();
            _runState.Team.Clear();
            _runState.CampRoster.Clear();
            _runState.CampItems.Clear();

            // Show starting pick screen
            var picks = _contentGenerator.GenerateStartingPicks(6);
            if (picks.Count > 0)
            {
                HideAll();
                _startPickScreen.Show(picks, 3);
            }
            else
            {
                // Fallback: use default team
                _runState.Team.AddRange(_contentGenerator.GeneratePlayerTeam());
                _runState.ReindexPositions();
                _runState.Map = new MapGenerator(_contentGenerator).Generate(Floors, Width, Seed);
                ShowMap();
            }
        }

        private void OnStartingPicksDone(List<UnitData> selectedUnits)
        {
            _runState.Team.Clear();
            for (int i = 0; i < selectedUnits.Count; i++)
            {
                var unit = new UnitInstance(selectedUnits[i]);
                unit.Position = i;
                unit.IsActive = true;
                _runState.Team.Add(unit);
            }
            _runState.ReindexPositions();
            _runState.Map = new MapGenerator(_contentGenerator).Generate(Floors, Width, Seed);
            ShowMap();
        }

        private void HideAll()
        {
            _battleScreen.Hide();
            _rewardScreen.Hide();
            _restScreen.Hide();
            _shopScreen.Hide();
            _manageTeamScreen.Hide();
            _mapScreen.Hide();
            _startPickScreen.Hide();
            _instructionScreen.Hide();
        }

        private void ShowMap()
        {
            HideAll();
            _mapScreen.Show(_runState);
        }

        private void OnNodeSelected(MapNode node)
        {
            if (!_runState.Map.IsNodeSelectable(node))
                return;

            node.Visited = true;
            _runState.Map.CurrentNode = node;
            _mapScreen.Hide();

            if (node.Type == MapNodeType.Rest)
            {
                _restScreen.Show(_runState);
                return;
            }

            if (node.Type == MapNodeType.Shop)
            {
                var (units, items) = _contentGenerator.GenerateShopOfferings(4);
                _shopScreen.Show(_runState, units, items);
                return;
            }

            // Battle, Elite, or Boss
            var activeTeam = _runState.GetActiveTeam();
            var (allies, enemies) = _battleScreen.ShowBattle(node, activeTeam, node.Encounter);
            _combatManager.StartBattle(allies, enemies, _battleScreen.OnTurnAction, OnBattleEnd, _battleScreen.OnNewRound);
        }

        private void OnBattleEnd(BattleResult result)
        {
            _lastBattleResult = result;
            _battleScreen.ShowResult(result.PlayerWon);
        }

        private void OnBattleContinue(bool playerWon)
        {
            if (_runState.IsGameOver || _runState.IsVictory)
            {
                StartRun();
                return;
            }

            if (playerWon)
            {
                // Dead critters are revived by BattleCombatManager (FullHeal on all allies)
                // No need to remove dead units anymore

                bool clearedBoss = _runState.Map.CurrentNode != null &&
                                   _runState.Map.CurrentNode.Type == MapNodeType.Boss;
                if (clearedBoss)
                {
                    _runState.IsVictory = true;
                    _battleScreen.SetFooter("Victory! You have conquered the dungeon! Press Continue to restart.");
                    return;
                }

                var rewards = _contentGenerator.GenerateItemRewards(3);
                _rewardScreen.Show(rewards, _runState.Team);
                _battleScreen.Hide();
            }
            else
            {
                if (_lastBattleResult != null && _lastBattleResult.SurvivingEnemies.Count > 0)
                    _runState.Map.InjectSurvivingEnemies(_lastBattleResult.SurvivingEnemies);

                // Dead critters are revived by BattleCombatManager
                // Check if all were dead at end of battle (game over condition)
                bool anyAlive = false;
                foreach (var u in _runState.Team)
                {
                    if (u.IsAlive) { anyAlive = true; break; }
                }

                if (!anyAlive)
                {
                    _runState.IsGameOver = true;
                    _battleScreen.SetFooter("Game Over! Your entire team has fallen. Press Continue to restart.");
                    return;
                }

                ShowMap();
            }
        }

        private void OnRestContinue()
        {
            // Rest heals all allies to full HP
            foreach (var unit in _runState.Team)
            {
                unit.FullHeal();
            }
            foreach (var unit in _runState.CampRoster)
            {
                unit.FullHeal();
            }
            ShowMap();
        }

        private void OnShopComplete()
        {
            ShowMap();
        }

        private void OnManageTeam()
        {
            _mapScreen.Hide();
            _manageTeamScreen.Show(_runState);
        }

        private void OnManageTeamDone()
        {
            _manageTeamScreen.Hide();
            ShowMap();
        }

        private void OnRewardSelected(ItemData item, UnitInstance unit)
        {
            if (item != null)
            {
                _runState.CollectedItems.Add(item);
                Debug.Log($"Applied {item.Name} to {unit?.DisplayName}");
            }

            ShowMap();
        }

        private ContentDatabase ResolveContentDatabase()
        {
            if (contentDatabase != null)
                return contentDatabase;
            return Resources.Load<ContentDatabase>("Content/DefaultContentDatabase");
        }
    }
}

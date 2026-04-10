using System.Collections.Generic;
using System.Linq;
using AutobattlerSample.Data;
using AutobattlerSample.Map;

namespace AutobattlerSample.Core
{
    public class RunState
    {
        public MapModel Map;
        public readonly List<UnitInstance> Team = new();
        public readonly List<UnitInstance> CampRoster = new();
        public readonly List<ItemData> CollectedItems = new();
        public readonly List<ItemData> CampItems = new();
        public bool IsGameOver;
        public bool IsVictory;

        public const int MaxSlots = 6;

        public int UsedSlots
        {
            get
            {
                int total = 0;
                foreach (var u in Team)
                {
                    if (u.IsActive)
                        total += u.SlotCost;
                }
                return total;
            }
        }

        public int RemainingSlots => MaxSlots - UsedSlots;

        public void RestoreHP(float percent)
        {
            foreach (var unit in Team)
            {
                if (!unit.IsAlive) continue;
                int heal = (int)(unit.EffectiveMaxHP * percent);
                unit.CurrentHP = System.Math.Min(unit.CurrentHP + heal, unit.EffectiveMaxHP);
            }
        }

        public bool UpgradeOrAddUnit(UnitData data)
        {
            // Check Team
            foreach (var u in Team)
            {
                if (u.BaseData != null && u.BaseData.UnitId == data.UnitId)
                {
                    u.RankUp();
                    return true;
                }
            }
            // Check Camp
            foreach (var u in CampRoster)
            {
                if (u.BaseData != null && u.BaseData.UnitId == data.UnitId)
                {
                    u.RankUp();
                    return true;
                }
            }

            var newUnit = new UnitInstance(data);
            int cost = newUnit.SlotCost;

            if (UsedSlots + cost <= MaxSlots)
            {
                newUnit.Position = Team.Count;
                newUnit.IsActive = true;
                Team.Add(newUnit);
            }
            else
            {
                // Send to camp
                newUnit.IsActive = false;
                CampRoster.Add(newUnit);
            }
            return false;
        }

        public void SendToCamp(UnitInstance unit)
        {
            if (!Team.Contains(unit)) return;
            Team.Remove(unit);
            unit.IsActive = false;
            CampRoster.Add(unit);
            ReindexPositions();
        }

        public bool ActivateFromCamp(UnitInstance unit)
        {
            if (!CampRoster.Contains(unit)) return false;
            if (UsedSlots + unit.SlotCost > MaxSlots) return false;
            CampRoster.Remove(unit);
            unit.IsActive = true;
            unit.Position = Team.Count;
            Team.Add(unit);
            return true;
        }

        public bool ActivateFromCampAtIndex(UnitInstance unit, int targetIndex)
        {
            if (!CampRoster.Contains(unit)) return false;
            if (UsedSlots + unit.SlotCost > MaxSlots) return false;

            CampRoster.Remove(unit);
            unit.IsActive = true;

            targetIndex = System.Math.Clamp(targetIndex, 0, Team.Count);
            Team.Insert(targetIndex, unit);
            ReindexPositions();
            return true;
        }

        public void SwapPositions(int idxA, int idxB)
        {
            if (idxA < 0 || idxA >= Team.Count || idxB < 0 || idxB >= Team.Count) return;
            (Team[idxA], Team[idxB]) = (Team[idxB], Team[idxA]);
            ReindexPositions();
        }

        public void MoveUnitUp(int index)
        {
            if (index <= 0 || index >= Team.Count) return;
            (Team[index], Team[index - 1]) = (Team[index - 1], Team[index]);
            ReindexPositions();
        }

        public void MoveUnitDown(int index)
        {
            if (index < 0 || index >= Team.Count - 1) return;
            (Team[index], Team[index + 1]) = (Team[index + 1], Team[index]);
            ReindexPositions();
        }

        public void MoveUnitToIndex(UnitInstance unit, int targetIndex)
        {
            if (!Team.Contains(unit)) return;

            Team.Remove(unit);
            targetIndex = System.Math.Clamp(targetIndex, 0, Team.Count);
            Team.Insert(targetIndex, unit);
            ReindexPositions();
        }

        public void ReindexPositions()
        {
            for (int i = 0; i < Team.Count; i++)
                Team[i].Position = i;
        }

        /// <summary>
        /// Get the active (fighting) units from the Team list.
        /// </summary>
        public List<UnitInstance> GetActiveTeam()
        {
            return Team.Where(u => u.IsActive).ToList();
        }
    }
}

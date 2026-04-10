using System.Collections.Generic;
using AutobattlerSample.Data;
using AutobattlerSample.Map;

namespace AutobattlerSample.Core
{
    public class RunState
    {
        public MapModel Map;
        public readonly List<UnitInstance> Team = new();
        public readonly List<ItemData> CollectedItems = new();
        public bool IsGameOver;
        public bool IsVictory;

        public void RestoreHP(float percent)
        {
            foreach (var unit in Team)
            {
                if (!unit.IsAlive) continue;
                int heal = (int)(unit.EffectiveMaxHP * percent);
                unit.CurrentHP = System.Math.Min(unit.CurrentHP + heal, unit.EffectiveMaxHP);
            }
        }

        /// <summary>
        /// If the team already has a unit with the same UnitId, rank it up.
        /// Otherwise add a new instance to the team.
        /// Returns true if it was an upgrade, false if a new addition.
        /// </summary>
        public bool UpgradeOrAddUnit(UnitData data)
        {
            foreach (var u in Team)
            {
                if (u.BaseData != null && u.BaseData.UnitId == data.UnitId)
                {
                    u.RankUp();
                    return true;
                }
            }
            Team.Add(new UnitInstance(data));
            return false;
        }

        public void MoveUnitUp(int index)
        {
            if (index <= 0 || index >= Team.Count) return;
            (Team[index], Team[index - 1]) = (Team[index - 1], Team[index]);
        }

        public void MoveUnitDown(int index)
        {
            if (index < 0 || index >= Team.Count - 1) return;
            (Team[index], Team[index + 1]) = (Team[index + 1], Team[index]);
        }
    }
}

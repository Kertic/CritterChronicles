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
    }
}

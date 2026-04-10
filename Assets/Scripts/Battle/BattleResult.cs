using System.Collections.Generic;
using AutobattlerSample.Data;

namespace AutobattlerSample.Battle
{
    public class BattleResult
    {
        public bool PlayerWon;
        public bool TimedOut;
        public int TotalTurns;
        public readonly List<TurnAction> TurnLog = new();
        public readonly List<UnitInstance> SurvivingEnemies = new();
    }
}

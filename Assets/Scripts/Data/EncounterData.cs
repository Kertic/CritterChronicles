using System.Collections.Generic;

namespace AutobattlerSample.Data
{
    public class EncounterData
    {
        public string DisplayName;
        public bool IsBoss;
        public List<UnitInstance> Enemies = new();
    }
}

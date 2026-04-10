using System.Collections.Generic;
using AutobattlerSample.Data;

namespace AutobattlerSample.Map
{
    [System.Serializable]
    public class MapNode
    {
        public int Floor;
        public int Index;
        public MapNodeType Type;
        public EncounterData Encounter;
        public bool Visited;
        public bool Reinforced;
        public readonly List<MapNode> Parents = new();
        public readonly List<MapNode> Children = new();

        public string Label
        {
            get
            {
                if (Type == MapNodeType.Boss) return "BOSS";
                if (Type == MapNodeType.Rest) return "Rest";
                if (Type == MapNodeType.Shop) return "Shop";
                string prefix = Reinforced ? "!! " : "";
                if (Type == MapNodeType.Elite)
                    return prefix + "Elite" + (Encounter != null ? ": " + Encounter.DisplayName : "");
                return prefix + (Encounter != null ? Encounter.DisplayName : Type.ToString());
            }
        }
    }
}

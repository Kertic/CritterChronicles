using System.Collections.Generic;
using AutobattlerSample.Data;

namespace AutobattlerSample.Map
{
    [System.Serializable]
    public class MapModel
    {
        public readonly List<List<MapNode>> Floors = new();
        public MapNode CurrentNode;

        public bool IsNodeSelectable(MapNode node)
        {
            if (node == null || node.Visited) return false;
            if (CurrentNode == null) return node.Floor == 0;
            return CurrentNode.Children.Contains(node);
        }

        public void InjectSurvivingEnemies(List<UnitInstance> survivors)
        {
            if (survivors == null || survivors.Count == 0) return;
            if (CurrentNode == null) return;

            // Find next reachable combat nodes
            var targets = new List<MapNode>();
            foreach (var child in CurrentNode.Children)
            {
                if (!child.Visited && child.Type != MapNodeType.Rest && child.Encounter != null)
                    targets.Add(child);
            }

            // If no direct children have encounters, search further
            if (targets.Count == 0)
            {
                for (int f = CurrentNode.Floor + 1; f < Floors.Count; f++)
                {
                    foreach (var node in Floors[f])
                    {
                        if (!node.Visited && node.Type != MapNodeType.Rest && node.Encounter != null)
                            targets.Add(node);
                    }
                    if (targets.Count > 0) break;
                }
            }

            if (targets.Count == 0) return;

            // Distribute survivors across target nodes
            for (int i = 0; i < survivors.Count; i++)
            {
                var target = targets[i % targets.Count];
                var clone = survivors[i].Clone();
                clone.FullHeal();
                target.Encounter.Enemies.Add(clone);
                target.Reinforced = true;
            }
        }
    }
}

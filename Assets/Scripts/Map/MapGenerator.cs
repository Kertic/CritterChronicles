using System.Collections.Generic;
using AutobattlerSample.Core;
using UnityEngine;

namespace AutobattlerSample.Map
{
    public class MapGenerator
    {
        private readonly ContentGenerator _contentGenerator;

        public MapGenerator(ContentGenerator contentGenerator)
        {
            _contentGenerator = contentGenerator;
        }

        public MapModel Generate(int floors = 6, int width = 3, int seed = 0)
        {
            Random.InitState(seed == 0 ? System.Environment.TickCount : seed);
            var model = new MapModel();

            for (int floor = 0; floor < floors; floor++)
            {
                var row = new List<MapNode>();
                bool isBossFloor = floor == floors - 1;
                int nodesThisFloor = isBossFloor ? 1 : width;

                for (int i = 0; i < nodesThisFloor; i++)
                {
                    MapNodeType nodeType;
                    Data.EncounterData encounter = null;

                    if (isBossFloor)
                    {
                        nodeType = MapNodeType.Boss;
                        encounter = _contentGenerator.GenerateBossEncounter();
                    }
                    else if (floor == 0)
                    {
                        nodeType = MapNodeType.Battle;
                        encounter = _contentGenerator.GenerateEncounter(floor, i);
                    }
                    else
                    {
                        float roll = Random.value;
                        if (roll < 0.12f)
                        {
                            nodeType = MapNodeType.Rest;
                        }
                        else if (roll < 0.27f)
                        {
                            nodeType = MapNodeType.Shop;
                        }
                        else if (roll < 0.42f)
                        {
                            nodeType = MapNodeType.Elite;
                            encounter = _contentGenerator.GenerateEliteEncounter(floor, i);
                        }
                        else
                        {
                            nodeType = MapNodeType.Battle;
                            encounter = _contentGenerator.GenerateEncounter(floor, i);
                        }
                    }

                    row.Add(new MapNode
                    {
                        Floor = floor,
                        Index = i,
                        Type = nodeType,
                        Encounter = encounter
                    });
                }

                model.Floors.Add(row);
            }

            // Connect nodes between floors
            for (int floor = 0; floor < model.Floors.Count - 1; floor++)
            {
                var current = model.Floors[floor];
                var next = model.Floors[floor + 1];

                for (int i = 0; i < current.Count; i++)
                {
                    var source = current[i];
                    if (next.Count == 1)
                    {
                        Connect(source, next[0]);
                        continue;
                    }

                    var targets = new HashSet<int>
                    {
                        Mathf.Clamp(i, 0, next.Count - 1)
                    };
                    if (i > 0) targets.Add(i - 1);
                    if (i < next.Count - 1) targets.Add(i + 1);

                    foreach (int targetIndex in targets)
                        Connect(source, next[targetIndex]);
                }
            }

            return model;
        }

        private static void Connect(MapNode parent, MapNode child)
        {
            if (!parent.Children.Contains(child))
                parent.Children.Add(child);
            if (!child.Parents.Contains(parent))
                child.Parents.Add(parent);
        }
    }
}

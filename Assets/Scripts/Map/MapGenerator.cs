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

        public MapModel Generate(int floors = 10, int width = 4, int seed = 0)
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
                        encounter = _contentGenerator.GenerateBossEncounter(floor);
                    }
                    else if (floor == 0)
                    {
                        nodeType = MapNodeType.Battle;
                        encounter = _contentGenerator.GenerateEncounter(floor, i);
                    }
                    else
                    {
                        float roll = Random.value;
                        if (roll < 0.10f)
                        {
                            nodeType = MapNodeType.Rest;
                        }
                        else if (roll < 0.22f)
                        {
                            nodeType = MapNodeType.Shop;
                        }
                        else if (roll < 0.38f)
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

            // Connect nodes between floors with random connections
            for (int floor = 0; floor < model.Floors.Count - 1; floor++)
            {
                var current = model.Floors[floor];
                var next = model.Floors[floor + 1];

                if (next.Count == 1)
                {
                    // Boss floor — all connect to it
                    foreach (var node in current)
                        Connect(node, next[0]);
                    continue;
                }

                // Track which next-layer nodes have at least one parent
                var nextConnected = new HashSet<int>();

                // Each current node connects to 1-2 random nodes in the next layer
                for (int i = 0; i < current.Count; i++)
                {
                    var source = current[i];
                    int connectionCount = Random.Range(1, 3); // 1 or 2 connections

                    // Always connect to at least the closest aligned node
                    int aligned = Mathf.Clamp(Mathf.RoundToInt((float)i / current.Count * (next.Count - 1)), 0, next.Count - 1);
                    Connect(source, next[aligned]);
                    nextConnected.Add(aligned);

                    // Add random extra connections
                    for (int c = 1; c < connectionCount; c++)
                    {
                        // Pick a random node within +/-1 of aligned
                        int min = Mathf.Max(0, aligned - 1);
                        int max = Mathf.Min(next.Count - 1, aligned + 1);
                        int target = Random.Range(min, max + 1);
                        Connect(source, next[target]);
                        nextConnected.Add(target);
                    }
                }

                // Ensure every next-layer node has at least one parent
                for (int j = 0; j < next.Count; j++)
                {
                    if (!nextConnected.Contains(j))
                    {
                        // Connect from the closest current-layer node
                        int closest = Mathf.Clamp(Mathf.RoundToInt((float)j / next.Count * (current.Count - 1)), 0, current.Count - 1);
                        Connect(current[closest], next[j]);
                    }
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

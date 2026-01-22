using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Grid;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Core.Data;

namespace CarbonWorld.Features.WorldMap
{
    public class WorldMap : MonoBehaviour
    {
        [Title("Map Size")]
        [SerializeField, Min(1)]
        private int rings = 5;

        [SerializeField, Min(0)]
        private int coreRadius = 1;

        [Title("Resource Generation")]
        [SerializeField]
        private List<ResourceSpawnRule> resourceRules = new();

        [SerializeField, Min(1)]
        private int minResourceSpacing = 2;

        [Title("Enhancement Tiles")]
        [SerializeField, Min(0)]
        private int enhancementTileCount = 8;

        [SerializeField, Min(1)]
        private int minEnhancementDistanceFromCore = 2;

        [Title("Tile Prefabs")]
        [SerializeField, Required, AssetsOnly]
        private Tile coreTilePrefab;

        [SerializeField, Required, AssetsOnly]
        private ResourceTile resourceTilePrefab;

        [SerializeField, Required, AssetsOnly]
        private Tile enhancementTilePrefab;

        [SerializeField, Required, AssetsOnly]
        private Tile productionTilePrefab;

        private HexGrid _grid = new();

        public HexGrid Grid => _grid;

        [Button("Generate", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        public void Generate()
        {
            Clear();

            var rng = new System.Random();
            var coords = HexUtils.GetSpiral(HexCoord.Zero, rings);
            var assignments = new Dictionary<HexCoord, TileAssignment>();

            // Phase 1: Core tiles (center)
            foreach (var coord in coords)
            {
                if (HexUtils.Distance(HexCoord.Zero, coord) <= coreRadius)
                    assignments[coord] = new TileAssignment { Type = TileType.Core };
            }

            // Phase 2: Resource tiles
            foreach (var rule in resourceRules)
            {
                int count = rng.Next(rule.minCount, rule.maxCount + 1);
                var candidates = GetValidCandidates(coords, assignments, rule.minDistanceFromCore);
                Shuffle(candidates, rng);

                for (int i = 0; i < count && candidates.Count > 0; i++)
                {
                    var coord = candidates[0];
                    candidates.RemoveAt(0);
                    assignments[coord] = new TileAssignment
                    {
                        Type = TileType.Resource,
                        Item = rule.item,
                        OutputPerTick = rule.outputPerTick
                    };
                    // Remove nearby candidates to enforce spacing
                    candidates.RemoveAll(c => HexUtils.Distance(coord, c) < minResourceSpacing);
                }
            }

            // Phase 3: Enhancement tiles
            var enhancementCandidates = GetValidCandidates(coords, assignments, minEnhancementDistanceFromCore);
            Shuffle(enhancementCandidates, rng);
            for (int i = 0; i < enhancementTileCount && i < enhancementCandidates.Count; i++)
            {
                assignments[enhancementCandidates[i]] = new TileAssignment { Type = TileType.Enhancement };
            }

            // Phase 4: Fill remaining with production tiles
            foreach (var coord in coords)
            {
                if (!assignments.ContainsKey(coord))
                    assignments[coord] = new TileAssignment { Type = TileType.Production };
            }

            // Phase 5: Instantiate all tiles
            InstantiateTiles(assignments);
        }

        [Button("Clear"), GUIColor(0.8f, 0.4f, 0.4f)]
        public void Clear()
        {
            _grid.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        private List<HexCoord> GetValidCandidates(
            List<HexCoord> allCoords,
            Dictionary<HexCoord, TileAssignment> assignments,
            int minDistanceFromCore)
        {
            var candidates = new List<HexCoord>();
            foreach (var coord in allCoords)
            {
                if (assignments.ContainsKey(coord))
                    continue;
                if (HexUtils.Distance(HexCoord.Zero, coord) < minDistanceFromCore)
                    continue;
                candidates.Add(coord);
            }
            return candidates;
        }

        private void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void InstantiateTiles(Dictionary<HexCoord, TileAssignment> assignments)
        {
            foreach (var (coord, assignment) in assignments)
            {
                var worldPos = HexUtils.HexToWorld(coord);
                Tile tile;

                switch (assignment.Type)
                {
                    case TileType.Core:
                        tile = Instantiate(coreTilePrefab, worldPos, Quaternion.identity, transform);
                        break;

                    case TileType.Resource:
                        var resourceTile = Instantiate(resourceTilePrefab, worldPos, Quaternion.identity, transform);
                        resourceTile.Initialize(coord);
                        resourceTile.ResourceItem = assignment.Item;
                        resourceTile.OutputPerTick = assignment.OutputPerTick;
                        resourceTile.name = $"{assignment.Type} {coord}";
                        _grid.Add(coord, resourceTile);
                        continue;

                    case TileType.Enhancement:
                        tile = Instantiate(enhancementTilePrefab, worldPos, Quaternion.identity, transform);
                        break;

                    case TileType.Production:
                    default:
                        tile = Instantiate(productionTilePrefab, worldPos, Quaternion.identity, transform);
                        break;
                }

                tile.Initialize(coord);
                tile.name = $"{assignment.Type} {coord}";
                _grid.Add(coord, tile);
            }
        }

        private enum TileType { Core, Resource, Enhancement, Production }

        private struct TileAssignment
        {
            public TileType Type;
            public ItemDefinition Item;
            public int OutputPerTick;
        }
    }

    [Serializable]
    public struct ResourceSpawnRule
    {
        [Required]
        public ItemDefinition item;

        [Min(1)]
        public int minCount;

        [Min(1)]
        public int maxCount;

        [Min(1)]
        public int minDistanceFromCore;

        [Min(1)]
        public int outputPerTick;
    }
}

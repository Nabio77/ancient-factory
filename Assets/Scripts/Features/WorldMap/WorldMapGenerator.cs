using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CarbonWorld.Features.Grid;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Features.WorldMap
{
    public class WorldMapGenerator
    {
        private static readonly Vector3Int Center = Vector3Int.zero;
        private System.Random _rng;

        public Dictionary<Vector3Int, TileAssignment> Generate(WorldGenProfile profile)
        {
            var assignments = new Dictionary<Vector3Int, TileAssignment>();
            _rng = new System.Random();
            var coords = HexUtils.GetSpiral(Center, profile.Rings);
            
            // Phase 1: Core tiles
            foreach (var coord in coords)
            {
                if (HexUtils.Distance(Center, coord) <= profile.CoreRadius)
                {
                    assignments[coord] = new TileAssignment { Type = TileType.Core };
                }
            }

            // Phase 2: Resource tiles
            foreach (var rule in profile.ResourceRules)
            {
                int spawnEvents = _rng.Next(rule.CountMin, rule.CountMax + 1);
                
                for (int i = 0; i < spawnEvents; i++)
                {
                    // Find valid start point
                    var validCandidates = GetValidCandidates(coords, assignments, rule.MinDistanceFromCore);
                    if (validCandidates.Count == 0) break;

                    Shuffle(validCandidates, _rng);
                    var centerPos = validCandidates[0];

                    if (rule.UseClustering)
                    {
                        GenerateCluster(centerPos, rule, assignments, profile);
                    }
                    else
                    {
                        AssignResourceTile(centerPos, rule, assignments, profile);
                    }
                }
            }

            // Phase 3: Settlement tiles
            int settlementCount = profile.SettlementTileCount;
            if (settlementCount > 0)
            {
                var candidates = GetValidCandidates(coords, assignments, profile.MinSettlementDistanceFromCore);
                Shuffle(candidates, _rng);

                int placed = 0;
                while (placed < settlementCount && candidates.Count > 0)
                {
                    var coord = candidates[0];
                    candidates.RemoveAt(0);

                    // Check spacing
                    bool tooClose = false;
                    foreach (var kvp in assignments)
                    {
                        if (kvp.Value.Type == TileType.Settlement)
                        {
                            if (HexUtils.Distance(coord, kvp.Key) < profile.MinSettlementSpacing)
                            {
                                tooClose = true;
                                break;
                            }
                        }
                    }

                    if (!tooClose)
                    {
                        assignments[coord] = new TileAssignment { Type = TileType.Settlement };
                        placed++;
                    }
                }
            }

            return assignments;
        }

        private void GenerateCluster(
            Vector3Int centerPos, 
            ResourceSpawnRule rule, 
            Dictionary<Vector3Int, TileAssignment> assignments,
            WorldGenProfile profile)
        {
            int clusterSize = _rng.Next(rule.ClusterSizeMin, rule.ClusterSizeMax + 1);
            
            // Get all tiles within radius of center
            var clusterCandidates = HexUtils.GetSpiral(centerPos, rule.ClusterRadius);
            
            var validClusterSpots = new List<Vector3Int>();
            foreach (var spot in clusterCandidates)
            {
                if (!assignments.ContainsKey(spot))
                {
                    validClusterSpots.Add(spot);
                }
            }

            // Shuffle valid spots to make the cluster organic
            Shuffle(validClusterSpots, _rng);

            // Assign
            int placed = 0;
            foreach (var spot in validClusterSpots)
            {
                if (placed >= clusterSize) break;
                AssignResourceTile(spot, rule, assignments, profile);
                placed++;
            }
        }

        private void AssignResourceTile(
            Vector3Int pos, 
            ResourceSpawnRule rule, 
            Dictionary<Vector3Int, TileAssignment> assignments,
            WorldGenProfile profile)
        {
            // Select Quality based on Weights
            var qualitySetting = SelectWeightedQuality(rule.QualitySettings);
            
            // Get Amount from Global Profile Settings based on Quality
            Vector2Int amountRange = qualitySetting.Quality switch
            {
                ResourceQuality.Impure => profile.ImpureAmounts,
                ResourceQuality.Normal => profile.NormalAmounts,
                ResourceQuality.Pure => profile.PureAmounts,
                _ => profile.NormalAmounts
            };

            int amount = _rng.Next(amountRange.x, amountRange.y + 1);

            assignments[pos] = new TileAssignment 
            { 
                Type = TileType.Resource, 
                Item = rule.Item,
                Quality = qualitySetting.Quality,
                Amount = amount
            };

            // Process Linked Rules
            if (rule.LinkedRules != null && rule.LinkedRules.Count > 0)
            {
                foreach (var linkedRule in rule.LinkedRules)
                {
                    if (_rng.Next(0, 100) < linkedRule.Chance)
                    {
                        GenerateLinkedResource(pos, linkedRule, assignments, profile);
                    }
                }
            }
        }

        private void GenerateLinkedResource(
            Vector3Int originPos,
            LinkedResourceRule linkedRule,
            Dictionary<Vector3Int, TileAssignment> assignments,
            WorldGenProfile profile)
        {
            int distMin = linkedRule.DistanceRange.x;
            int distMax = linkedRule.DistanceRange.y;

            // Find valid spot in ring range
            // Using a simple inefficient scan for now since map is small, or iterate rings
            // Better: GetSpiral up to Max, assign if Dist >= Min
            
            var potentialSpots = HexUtils.GetSpiral(originPos, distMax);
            Shuffle(potentialSpots, _rng);

            foreach (var spot in potentialSpots)
            {
                int d = HexUtils.Distance(originPos, spot);
                if (d < distMin) continue;

                if (!assignments.ContainsKey(spot))
                {
                    // For linked resources, use Normal quality amounts by default
                    // or we could randomize quality, but keep it simple: Normal.
                    
                    var quality = ResourceQuality.Normal;
                    Vector2Int amountRange = profile.NormalAmounts;
                    int amount = _rng.Next(amountRange.x, amountRange.y + 1);
                    
                    assignments[spot] = new TileAssignment 
                    { 
                        Type = TileType.Resource, 
                        Item = linkedRule.Item,
                        Quality = quality,
                        Amount = amount
                    };
                    break; // Only spawn one per linked rule instance? "LinkedRules" implies 1:1 or chance.
                }
            }
        }
        
        private ResourceQualitySetting SelectWeightedQuality(List<ResourceQualitySetting> settings)
        {
            if (settings == null || settings.Count == 0)
            {
                // Fallback struct (fake) just to return something usable
                // We will handle the amount lookup upstream
                return new ResourceQualitySetting 
                { 
                    Quality = ResourceQuality.Normal, 
                    Weight = 10 
                };
            }

            int totalWeight = settings.Sum(s => s.Weight);
            int roll = _rng.Next(0, totalWeight);
            int current = 0;

            foreach (var s in settings)
            {
                current += s.Weight;
                if (roll < current) return s;
            }

            return settings.Last();
        }

        private List<Vector3Int> GetValidCandidates(
            List<Vector3Int> allCoords,
            Dictionary<Vector3Int, TileAssignment> assignments,
            int minDistanceFromCore)
        {
            var candidates = new List<Vector3Int>();
            foreach (var coord in allCoords)
            {
                if (assignments.ContainsKey(coord))
                    continue;
                if (HexUtils.Distance(Center, coord) < minDistanceFromCore)
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
    }

    public struct TileAssignment
    {
        public TileType Type;
        public ItemDefinition Item;
        public ResourceQuality Quality;
        public int Amount;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Grid;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Core.Data;
using CarbonWorld.Types;

namespace CarbonWorld.Features.WorldMap
{
    public class WorldMap : MonoBehaviour
    {
        [Title("Tilemap References")]
        [SerializeField, Required]
        private UnityEngine.Grid grid;

        [SerializeField, Required]
        private Tilemap tilemap;

        [SerializeField, Required]
        private Tilemap highlightTilemap;

        [Title("Tile Assets")]
        [SerializeField, Required]
        private TileBase coreTile;

        [SerializeField, Required]
        private TileBase resourceTile;

        [SerializeField, Required]
        private TileBase productionTile;

        [SerializeField, Required]
        private TileBase enhancementTile;

        [SerializeField, Required]
        private TileBase powerTile;

        [SerializeField]
        private TileBase hoverHighlightTile;

        [SerializeField]
        private TileBase selectedHighlightTile;

        [SerializeField]
        private TileBase powerRangeHighlightTile;

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

        [Title("Power Tiles")]
        [SerializeField, Min(0)]
        private int powerTileCount = 4;

        [SerializeField, Min(1)]
        private int minPowerDistanceFromCore = 2;

        [SerializeField, HideInInspector]
        private List<TileSaveData> _savedTiles = new();

        private TileDataGrid _tileData = new();

        public TileDataGrid TileData => _tileData;
        public UnityEngine.Grid Grid => grid;
        public Tilemap Tilemap => tilemap;
        public Tilemap HighlightTilemap => highlightTilemap;
        public TileBase HoverHighlightTile => hoverHighlightTile;
        public TileBase SelectedHighlightTile => selectedHighlightTile;
        public TileBase PowerRangeHighlightTile => powerRangeHighlightTile;

        private static readonly Vector3Int Center = Vector3Int.zero;

        private void Awake()
        {
            // Ensure highlight is drawn on top
            var mainRenderer = tilemap.GetComponent<TilemapRenderer>();
            var highlightRenderer = highlightTilemap.GetComponent<TilemapRenderer>();
            
            if (mainRenderer != null && highlightRenderer != null)
            {
                if (highlightRenderer.sortingOrder <= mainRenderer.sortingOrder)
                {
                    highlightRenderer.sortingOrder = mainRenderer.sortingOrder + 1;
                }
            }

            if (_tileData.Count == 0 && _savedTiles.Count > 0)
            {
                foreach (var data in _savedTiles)
                {
                    BaseTile tileData;
                    switch (data.Type)
                    {
                        case TileType.Core:
                            tileData = new CoreTile(data.Position, TileType.Core);
                            break;
                        case TileType.Resource:
                            tileData = new ResourceTile(data.Position, data.Item, data.OutputPerTick);
                            break;
                        case TileType.Enhancement:
                            tileData = new EnhancementTile(data.Position, TileType.Enhancement);
                            break;
                        case TileType.Power:
                            tileData = new PowerTile(data.Position);
                            break;
                        case TileType.Production:
                        default:
                            tileData = new ProductionTile(data.Position);
                            break;
                    }
                    _tileData.Add(data.Position, tileData);
                }
            }
        }

        [Button("Generate", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        public void Generate()
        {
            Clear();

            var rng = new System.Random();
            var coords = HexUtils.GetSpiral(Center, rings);
            var assignments = new Dictionary<Vector3Int, TileAssignment>();

            // Phase 1: Core tiles (center)
            foreach (var coord in coords)
            {
                if (HexUtils.Distance(Center, coord) <= coreRadius)
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

            // Phase 4: Power tiles
            var powerCandidates = GetValidCandidates(coords, assignments, minPowerDistanceFromCore);
            Shuffle(powerCandidates, rng);
            for (int i = 0; i < powerTileCount && i < powerCandidates.Count; i++)
            {
                assignments[powerCandidates[i]] = new TileAssignment { Type = TileType.Power };
            }

            // Phase 5: Fill remaining with production tiles
            foreach (var coord in coords)
            {
                if (!assignments.ContainsKey(coord))
                    assignments[coord] = new TileAssignment { Type = TileType.Production };
            }

            // Phase 5: Create tiles
            CreateTiles(assignments);
        }

        [Button("Clear"), GUIColor(0.8f, 0.4f, 0.4f)]
        public void Clear()
        {
            _tileData.Clear();
            _savedTiles.Clear();
            tilemap.ClearAllTiles();
            highlightTilemap.ClearAllTiles();
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

        private void CreateTiles(Dictionary<Vector3Int, TileAssignment> assignments)
        {
            _savedTiles.Clear();
            foreach (var (coord, assignment) in assignments)
            {
                TileBase visualTile;
                BaseTile tileData;

                switch (assignment.Type)
                {
                    case TileType.Core:
                        visualTile = coreTile;
                        tileData = new CoreTile(coord, TileType.Core);
                        break;

                    case TileType.Resource:
                        visualTile = resourceTile;
                        tileData = new ResourceTile(coord, assignment.Item, assignment.OutputPerTick);
                        break;

                    case TileType.Enhancement:
                        visualTile = enhancementTile;
                        tileData = new EnhancementTile(coord, TileType.Enhancement);
                        break;

                    case TileType.Power:
                        visualTile = powerTile;
                        tileData = new PowerTile(coord);
                        break;

                    case TileType.Production:
                    default:
                        visualTile = productionTile;
                        tileData = new ProductionTile(coord);
                        break;
                }

                _savedTiles.Add(new TileSaveData
                {
                    Position = coord,
                    Type = assignment.Type,
                    Item = assignment.Item,
                    OutputPerTick = assignment.OutputPerTick
                });

                tilemap.SetTile(coord, visualTile);
                _tileData.Add(coord, tileData);
            }
        }

        public Vector3 CellToWorld(Vector3Int cellPos)
        {
            return grid.CellToWorld(cellPos);
        }

        public Vector3Int WorldToCell(Vector3 worldPos)
        {
            // Use tilemap.WorldToCell for correct hexagonal coordinate conversion
            // Grid.WorldToCell uses simple floor which doesn't work for hex tiles
            return tilemap.WorldToCell(worldPos);
        }

        private struct TileAssignment
        {
            public TileType Type;
            public ItemDefinition Item;
            public int OutputPerTick;
        }
    }

    [Serializable]
    public struct TileSaveData
    {
        public Vector3Int Position;
        public TileType Type;
        public ItemDefinition Item;
        public int OutputPerTick;
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

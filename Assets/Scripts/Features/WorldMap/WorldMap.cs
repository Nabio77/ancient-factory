using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Grid;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Features.WorldMap
{
    public class WorldMap : MonoBehaviour
    {
        public event Action OnMapGenerated;
        public event Action<Vector3Int> OnTileChanged;

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
        private TileBase settlementTile;

        [SerializeField, Required]
        private TileBase powerTile;

        [SerializeField]
        private TileBase transportTile;

        [SerializeField]
        private TileBase natureTile;

        [SerializeField]
        private TileBase floodedTile;

        [SerializeField]
        private TileBase deadZoneTile;

        [SerializeField]
        private TileBase refugeeCampTile;

        [SerializeField]
        private TileBase heatwaveTile;

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

        [Title("Settlement Tiles")]
        [SerializeField, Min(0)]
        private int settlementTileCount = 3;

        [SerializeField, Min(1)]
        private int minSettlementDistanceFromCore = 3;

        [SerializeField, HideInInspector]
        private List<TileSaveData> _savedTiles = new();

        [Title("Systems")]
        [SerializeField]
        private TileGraphSystem graphSystem;

        private TileDataGrid _tileData = new();

        public TileDataGrid TileData => _tileData;
        public TileGraphSystem GraphSystem => graphSystem;
        public UnityEngine.Grid Grid => grid;
        public Tilemap Tilemap => tilemap;
        public Tilemap HighlightTilemap => highlightTilemap;
        public TileBase HoverHighlightTile => hoverHighlightTile;
        public TileBase SelectedHighlightTile => selectedHighlightTile;
        public TileBase PowerRangeHighlightTile => powerRangeHighlightTile;

        private static readonly Vector3Int Center = Vector3Int.zero;

        private void Awake()
        {
            // Ensure system exists
            if (graphSystem == null)
            {
                graphSystem = GetComponent<TileGraphSystem>();
                if (graphSystem == null)
                {
                    graphSystem = gameObject.AddComponent<TileGraphSystem>();
                }
            }
            
            graphSystem.Initialize(_tileData, this);

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
                        case TileType.Settlement:
                            tileData = new SettlementTile(data.Position);
                            break;
                        case TileType.Power:
                            tileData = new PowerTile(data.Position);
                            break;
                        case TileType.Nature:
                            tileData = new NatureTile(data.Position);
                            break;
                        case TileType.Transport:
                            tileData = new TransportTile(data.Position);
                            break;
                        case TileType.Flooded:
                            tileData = new FloodedTile(data.Position, 0);
                            break;
                        case TileType.DeadZone:
                            tileData = new DeadZoneTile(data.Position, 0);
                            break;
                        case TileType.RefugeeCamp:
                            tileData = new RefugeeCampTile(data.Position, 0, 50);
                            break;
                        case TileType.Heatwave:
                            tileData = new HeatwaveTile(data.Position, 0);
                            break;
                        case TileType.Production:
                        default:
                            tileData = new ProductionTile(data.Position);
                            break;
                    }
                    _tileData.Add(data.Position, tileData);
                }

                // Initialize Graph Tiles after loading
                graphSystem.RefreshAll(_tileData);
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

            // Phase 3: Settlement tiles (mini settlements as goals)
            var settlementCandidates = GetValidCandidates(coords, assignments, minSettlementDistanceFromCore);
            Shuffle(settlementCandidates, rng);
            for (int i = 0; i < settlementTileCount && i < settlementCandidates.Count; i++)
            {
                assignments[settlementCandidates[i]] = new TileAssignment { Type = TileType.Settlement };
            }

            // Note: No longer filling with production tiles - player will place tiles manually

            // Phase 5: Create tiles
            CreateTiles(assignments);

            // Phase 6: Initialize Graph Tiles
            graphSystem.RefreshAll(_tileData);

            OnMapGenerated?.Invoke();
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

                    case TileType.Settlement:
                        visualTile = settlementTile;
                        tileData = new SettlementTile(coord);
                        break;

                    case TileType.Power:
                        visualTile = powerTile;
                        tileData = new PowerTile(coord);
                        break;

                    case TileType.Nature:
                        visualTile = natureTile;
                        tileData = new NatureTile(coord);
                        break;

                    case TileType.Transport:
                        visualTile = transportTile;
                        tileData = new TransportTile(coord);
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

        /// <summary>
        /// Replaces a tile at the given position with a new type.
        /// </summary>
        public void ReplaceTile(Vector3Int position, TileType newType)
        {
            if (!_tileData.Contains(position)) return;

            // 1. Create new tile data
            BaseTile newTileData;
            switch (newType)
            {
                case TileType.Power:
                    newTileData = new PowerTile(position);
                    break;
                case TileType.Nature:
                    newTileData = new NatureTile(position);
                    break;
                case TileType.Transport:
                    newTileData = new TransportTile(position);
                    break;
                case TileType.Production:
                    newTileData = new ProductionTile(position);
                    break;
                default:
                    Debug.LogWarning($"ReplaceTile: Unsupported type {newType}");
                    return;
            }

            // 2. Update TileDataGrid
            _tileData.Remove(position);
            _tileData.Add(position, newTileData);

            // 3. Update Visuals
            UpdateTileVisual(position);

            // 4. Update Saved Data
            int saveIndex = _savedTiles.FindIndex(t => t.Position == position);
            if (saveIndex != -1)
            {
                var save = _savedTiles[saveIndex];
                save.Type = newType;
                // Reset item/output for non-resource types (assuming we don't convert to resource)
                save.Item = null;
                save.OutputPerTick = 0;
                _savedTiles[saveIndex] = save;
            }
            else
            {
                // Should exist, but if not, add it
                _savedTiles.Add(new TileSaveData
                {
                    Position = position,
                    Type = newType
                });
            }
            
            // 5. Notify neighbors if they are graph tiles to update IO?
            // This is important for immediate feedback in UI if lines are drawn
            graphSystem.UpdateNeighbors(_tileData, position);

            OnTileChanged?.Invoke(position);
        }

        public Vector3 CellToWorld(Vector3Int cellPos)
        {
            return tilemap.GetCellCenterWorld(cellPos);
        }

        public Vector3Int WorldToCell(Vector3 worldPos)
        {
            // Use tilemap.WorldToCell for correct hexagonal coordinate conversion
            // Grid.WorldToCell uses simple floor which doesn't work for hex tiles
            return tilemap.WorldToCell(worldPos);
        }

        /// <summary>
        /// Updates the visual tile at the given position to match the tile type.
        /// Called by CarbonSystem when disasters spawn.
        /// </summary>
        public void UpdateTileVisual(Vector3Int position)
        {
            var tile = _tileData.GetTile(position);
            if (tile == null) return;

            TileBase visualTile = tile.Type switch
            {
                TileType.Core => coreTile,
                TileType.Resource => resourceTile,
                TileType.Production => productionTile,
                TileType.Settlement => settlementTile,
                TileType.Power => powerTile,
                TileType.Nature => natureTile,
                TileType.Transport => transportTile,
                TileType.Flooded => floodedTile,
                TileType.DeadZone => deadZoneTile,
                TileType.RefugeeCamp => refugeeCampTile,
                TileType.Heatwave => heatwaveTile,
                _ => productionTile
            };

            tilemap.SetTile(position, visualTile);
        }

        /// <summary>
        /// Gets the visual tile asset for a given tile type.
        /// </summary>
        public TileBase GetTileAsset(TileType type)
        {
            return type switch
            {
                TileType.Core => coreTile,
                TileType.Resource => resourceTile,
                TileType.Production => productionTile,
                TileType.Settlement => settlementTile,
                TileType.Power => powerTile,
                TileType.Nature => natureTile,
                TileType.Transport => transportTile,
                TileType.Flooded => floodedTile,
                TileType.DeadZone => deadZoneTile,
                TileType.RefugeeCamp => refugeeCampTile,
                TileType.Heatwave => heatwaveTile,
                _ => productionTile
            };
        }

        public bool CanPlaceTile(Vector3Int position)
        {
            // Position must be empty
            if (_tileData.Contains(position))
                return false;

            // Must be adjacent to at least one existing tile
            var neighbors = HexUtils.GetNeighbors(position);
            foreach (var neighbor in neighbors)
            {
                if (_tileData.Contains(neighbor))
                    return true;
            }
            return false;
        }

        public bool AddTile(Vector3Int position, TileType type)
        {
            if (!CanPlaceTile(position))
                return false;

            BaseTile tileData;
            switch (type)
            {
                case TileType.Production:
                    tileData = new ProductionTile(position);
                    break;
                case TileType.Power:
                    tileData = new PowerTile(position);
                    break;
                case TileType.Nature:
                    tileData = new NatureTile(position);
                    break;
                case TileType.Transport:
                    tileData = new TransportTile(position);
                    break;
                default:
                    Debug.LogWarning($"AddTile: Cannot place tile of type {type}");
                    return false;
            }

            _tileData.Add(position, tileData);
            tilemap.SetTile(position, GetTileAsset(type));
            _savedTiles.Add(new TileSaveData
            {
                Position = position,
                Type = type
            });

            // Update graph system
            graphSystem.UpdateNeighbors(_tileData, position);

            OnTileChanged?.Invoke(position);
            return true;
        }

        public List<Vector3Int> GetValidPlacementPositions()
        {
            var validPositions = new HashSet<Vector3Int>();

            foreach (var tile in _tileData.GetAllTiles())
            {
                var neighbors = HexUtils.GetNeighbors(tile.CellPosition);
                foreach (var neighbor in neighbors)
                {
                    if (!_tileData.Contains(neighbor))
                    {
                        validPositions.Add(neighbor);
                    }
                }
            }

            return new List<Vector3Int>(validPositions);
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

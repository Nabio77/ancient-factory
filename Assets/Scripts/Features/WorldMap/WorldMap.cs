using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Grid;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Types;
using CarbonWorld.Core.Systems;
using Drawing;

namespace CarbonWorld.Features.WorldMap
{
    public class WorldMap : MonoBehaviour
    {
        public event Action OnMapGenerated;
        public event Action<Vector3Int> OnTileChanged;

        [Title("Configuration")]
        [SerializeField, Required]
        private WorldGenProfile generationProfile;

        [SerializeField, HideInInspector]
        private List<TileSaveData> _savedTiles = new();

        [Title("Systems")]
        [SerializeField]
        private TileGraphSystem graphSystem;
        
        [SerializeField]
        private WorldMapVisualizer visualizer;

        private TileDataGrid _tileData = new();
        private WorldMapGenerator _generator = new();

        public TileDataGrid TileData => _tileData;
        public TileGraphSystem GraphSystem => graphSystem;
        public WorldMapVisualizer Visualizer => visualizer;
        public WorldGenProfile Profile => generationProfile;

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
            
            if (visualizer == null)
            {
                visualizer = GetComponent<WorldMapVisualizer>();
                if (visualizer == null)
                {
                    visualizer = gameObject.AddComponent<WorldMapVisualizer>();
                }
            }

            // Ensure other core systems exist in the scene
            EnsureSystem<ProductionSystem>();
            EnsureSystem<ItemFlowSystem>();
            EnsureSystem<PowerDistributionSystem>();
            EnsureSystem<SettlementSystem>();

            graphSystem.Initialize(_tileData, this);

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
                            tileData = new ResourceTile(data.Position, data.Item, data.Quality, data.Amount);
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
                        case TileType.Food:
                            tileData = new FoodTile(data.Position);
                            break;
                        case TileType.Production:
                        default:
                            tileData = new ProductionTile(data.Position);
                            break;
                    }
                    _tileData.Add(data.Position, tileData);
                }
                
                // Refresh visuals from data
                visualizer.RefreshAll(_tileData);

                // Initialize Graph Tiles after loading
                graphSystem.RefreshAll(_tileData);
            }
        }

        private void Update()
        {
            DrawPowerIndicators();
        }

        private void DrawPowerIndicators()
        {
            if (_tileData == null) return;
            var powerSystem = PowerDistributionSystem.Instance;
            if (powerSystem == null) return;

            var draw = Draw.ingame;

            // 1. Draw borders around ALL powered positions (Reach)
            using (draw.WithColor(Color.yellow))
            {
                // Pre-calculate hex points relative to center
                const float radius = 0.55f;
                var hexOffsets = new Vector3[7];
                for (int i = 0; i <= 6; i++)
                {
                    float angle = (30 + 60 * i) * Mathf.Deg2Rad;
                    hexOffsets[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
                }

                foreach (var pos in powerSystem.GetAllPoweredPositions())
                {
                    var center = visualizer.CellToWorld(pos);
                    var points = new Vector3[7];
                    for (int i = 0; i <= 6; i++)
                    {
                        points[i] = center + hexOffsets[i];
                    }
                    draw.Polyline(points);
                }
            }

            // 2. Draw labels on Source Tiles
            foreach (var tile in _tileData.GetAllTiles())
            {
                if (tile is PowerTile powerTile && powerTile.TotalPowerOutput > 0)
                {
                    var center = visualizer.CellToWorld(tile.CellPosition);
                    draw.Label2D(center, $"{powerTile.TotalPowerOutput}kW", 20f);
                }
            }
        }

        [Button("Generate", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        public void Generate()
        {
            if (generationProfile == null)
            {
                Debug.LogError("WorldGenProfile is missing!");
                return;
            }

            Clear();

            var assignments = _generator.Generate(generationProfile);
            ApplyAssignments(assignments);

            // Phase 6: Initialize Graph Tiles
            graphSystem.RefreshAll(_tileData);

            OnMapGenerated?.Invoke();
        }

        [Button("Clear"), GUIColor(0.8f, 0.4f, 0.4f)]
        public void Clear()
        {
            _tileData.Clear();
            _savedTiles.Clear();
            visualizer.Clear();
        }

        private void ApplyAssignments(Dictionary<Vector3Int, TileAssignment> assignments)
        {
            _savedTiles.Clear();
            
            foreach (var (coord, assignment) in assignments)
            {
                 BaseTile tileData;
                 switch (assignment.Type)
                 {
                     case TileType.Core:
                         tileData = new CoreTile(coord, TileType.Core);
                         break;
                     case TileType.Resource:
                         tileData = new ResourceTile(coord, assignment.Item, assignment.Quality, assignment.Amount);
                         break;
                     case TileType.Settlement:
                         tileData = new SettlementTile(coord);
                         break;
                     case TileType.Power:
                         tileData = new PowerTile(coord);
                         break;
                     case TileType.Nature:
                         tileData = new NatureTile(coord);
                         break;
                     case TileType.Transport:
                         tileData = new TransportTile(coord);
                         break;
                     case TileType.Food:
                         tileData = new FoodTile(coord);
                         break;
                     case TileType.Production:
                     default:
                         tileData = new ProductionTile(coord);
                         break;
                 }

                 _savedTiles.Add(new TileSaveData
                 {
                     Position = coord,
                     Type = assignment.Type,
                     Item = assignment.Item,
                     Quality = assignment.Quality,
                     Amount = assignment.Amount
                 });

                 _tileData.Add(coord, tileData);
                 visualizer.SetTile(coord, assignment.Type, assignment.Item);
            }
        }

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
                case TileType.Food:
                    newTileData = new FoodTile(position);
                    break;
                default:
                    Debug.LogWarning($"ReplaceTile: Unsupported type {newType}");
                    return;
            }

            // 2. Update TileDataGrid
            _tileData.Remove(position);
            _tileData.Add(position, newTileData);

            // 3. Update Visuals
            visualizer.SetTile(position, newType, null);

            // 4. Update Saved Data
            int saveIndex = _savedTiles.FindIndex(t => t.Position == position);
            if (saveIndex != -1)
            {
                var save = _savedTiles[saveIndex];
                save.Type = newType;
                // Reset item/quality for non-resource types (assuming we don't convert to resource)
                save.Item = null;
                save.Quality = default;
                _savedTiles[saveIndex] = save;
            }
            else
            {
                _savedTiles.Add(new TileSaveData
                {
                    Position = position,
                    Type = newType
                });
            }

            // 5. Notify neighbors if they are graph tiles to update IO
            graphSystem.UpdateNeighbors(_tileData, position);

            OnTileChanged?.Invoke(position);
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
                case TileType.Food:
                    tileData = new FoodTile(position);
                    break;
                default:
                    Debug.LogWarning($"AddTile: Cannot place tile of type {type}");
                    return false;
            }

            _tileData.Add(position, tileData);
            visualizer.SetTile(position, type, null);
            
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

        // Delegate to Visualizer or keep? Keeping for compatibility if other systems use it, 
        // but implementation calls visualizer.
        public void UpdateTileVisual(Vector3Int position)
        {
             var tile = _tileData.GetTile(position);
             if (tile == null) return;
             
             ItemDefinition item = null;
             if (tile is ResourceTile rt) item = rt.ResourceItem;
             
             visualizer.SetTile(position, tile.Type, item);
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

        #region Visualizer Proxies
        public Tilemap Tilemap => visualizer.Tilemap;
        public Tilemap HighlightTilemap => visualizer.HighlightTilemap;
        public TileBase HoverHighlightTile => visualizer.HoverHighlightTile;
        public TileBase SelectedHighlightTile => visualizer.SelectedHighlightTile;
        public TileBase PowerRangeHighlightTile => visualizer.PowerRangeHighlightTile;

        public Vector3 CellToWorld(Vector3Int cellPos) => visualizer.CellToWorld(cellPos);
        public Vector3Int WorldToCell(Vector3 worldPos) => visualizer.WorldToCell(worldPos);
        
        public TileBase GetTileAsset(TileType type) => visualizer.GetVisualTile(type, null);
        #endregion

        private void EnsureSystem<T>() where T : Component
        {
            if (FindFirstObjectByType<T>() == null)
            {
                gameObject.AddComponent<T>();
            }
        }
    }

    [Serializable]
    public struct TileSaveData
    {
        public Vector3Int Position;
        public TileType Type;
        public ItemDefinition Item;
        public ResourceQuality Quality;
        public int Amount;
    }
}

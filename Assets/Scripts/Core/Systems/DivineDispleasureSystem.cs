using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using AncientFactory.Features.Tiles;
using AncientFactory.Features.WorldMap;
using AncientFactory.Features.Grid;
using AncientFactory.Core.Types;

namespace AncientFactory.Core.Systems
{
    public class DivineDispleasureSystem : MonoBehaviour
    {
        public static DivineDispleasureSystem Instance { get; private set; }

        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [Title("Tick Settings")]
        [SerializeField, MinValue(0.1f)]
        private float tickInterval = 1f;

        [Title("Displeasure Settings")]
        [SerializeField, Tooltip("Minimum distance between disaster tiles")]
        private int minDisasterSpacing = 2;

        [SerializeField, Tooltip("Ticks without temples before penalty starts")]
        private int templeNeglectThreshold = 10;

        [SerializeField, Tooltip("Extra displeasure per tick when neglecting temples")]
        private int templeNeglectPenalty = 5;

        [Title("State (Read Only)")]
        [ShowInInspector, ReadOnly]
        private int _totalDispleasure;

        [ShowInInspector, ReadOnly]
        private int _displeasureGeneratedLastTick;

        [ShowInInspector, ReadOnly]
        private int _favorGeneratedLastTick;

        [ShowInInspector, ReadOnly]
        private int _currentTick;

        [ShowInInspector, ReadOnly]
        private DivineFavorState _currentState = DivineFavorState.Blessed;

        [ShowInInspector, ReadOnly]
        private int _templeCount;

        [ShowInInspector, ReadOnly]
        private int _ticksWithoutTemples;

        [ShowInInspector, ReadOnly]
        private float _slaveLaborIntensity;

        private float _tickTimer;
        private readonly System.Random _rng = new();

        // Events
        public event Action<int, int, int> OnDispleasureUpdated; // total, generated, favor
        public event Action<DivineFavorState> OnDivineStateChanged;
        public event Action<DisasterTile> OnDisasterSpawned;
        public event Action<BaseTile, DisasterTile> OnTileDestroyed; // original, replacement

        // Properties
        public int TotalDispleasure => _totalDispleasure;
        public int DispleasureGeneratedLastTick => _displeasureGeneratedLastTick;
        public int FavorGeneratedLastTick => _favorGeneratedLastTick;
        public DivineFavorState CurrentState => _currentState;
        public int NetDispleasurePerTick => _displeasureGeneratedLastTick - _favorGeneratedLastTick;
        public int TempleCount => _templeCount;
        public float SlaveLaborIntensity => _slaveLaborIntensity;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickInterval)
            {
                _tickTimer = 0f;
                ProcessTick();
            }
        }

        [Button("Process Tick")]
        public void ProcessTick()
        {
            _currentTick++;

            // Phase 1: Calculate displeasure from production
            int generated = CalculateDispleasure();

            // Phase 2: Calculate divine favor from nature and temples
            int favor = CalculateDivineFavor();

            // Phase 3: Apply temple neglect penalty
            _templeCount = CountTemples();
            if (_templeCount == 0)
            {
                _ticksWithoutTemples++;
                if (_ticksWithoutTemples > templeNeglectThreshold)
                {
                    generated += templeNeglectPenalty;
                }
            }
            else
            {
                _ticksWithoutTemples = 0;
            }

            // Phase 4: Update total
            _displeasureGeneratedLastTick = generated;
            _favorGeneratedLastTick = favor;
            _totalDispleasure = Mathf.Max(0, _totalDispleasure + generated - favor);

            OnDispleasureUpdated?.Invoke(_totalDispleasure, generated, favor);

            // Phase 5: Update divine state
            DivineFavorState newState = GetDivineState(_totalDispleasure);
            if (newState != _currentState)
            {
                _currentState = newState;
                OnDivineStateChanged?.Invoke(_currentState);
            }

            // Phase 6: Process divine punishment effects based on displeasure level
            ProcessDivinePunishments();

            // Phase 7: Process disaster spreading
            ProcessDisasterSpreading();
        }

        private int CalculateDispleasure()
        {
            int total = 0;
            float slaveLaborTotal = 0;
            int productionCount = 0;

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is FactoryTile factoryTile && factoryTile.IsPowered)
                {
                    foreach (var node in factoryTile.Graph.nodes)
                    {
                        if (node.blueprint != null)
                        {
                            total += node.blueprint.DivineDispleasure;
                            if (node.blueprint.DispleasureSource == DispleasureSource.SlaveLabor)
                            {
                                slaveLaborTotal += 1;
                            }
                            productionCount++;
                        }
                    }
                }
                else if (tile is PowerTile powerTile && powerTile.TotalPowerOutput > 0)
                {
                    foreach (var node in powerTile.Graph.nodes)
                    {
                        if (node.blueprint != null && node.blueprint.IsPowerGenerator)
                        {
                            total += node.blueprint.DivineDispleasure;
                        }
                    }
                }
            }

            _slaveLaborIntensity = productionCount > 0 ? slaveLaborTotal / productionCount : 0;

            return total;
        }

        private int CalculateDivineFavor()
        {
            int total = 0;

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is NatureTile natureTile)
                {
                    total += natureTile.GetDivineFavor();
                }
                else if (tile is TempleTile templeTile)
                {
                    total += templeTile.GetDivineFavor();
                }
            }

            return total;
        }

        private int CountTemples()
        {
            return worldMap.TileData.GetAllTiles().Count(t => t is TempleTile);
        }

        private DivineFavorState GetDivineState(int displeasure)
        {
            if (displeasure >= 500) return DivineFavorState.Forsaken;
            if (displeasure >= 350) return DivineFavorState.Wrathful;
            if (displeasure >= 200) return DivineFavorState.Displeased;
            if (displeasure >= 100) return DivineFavorState.Tolerated;
            if (displeasure >= 50) return DivineFavorState.Favored;
            return DivineFavorState.Blessed;
        }

        private void ProcessDivinePunishments()
        {
            // Blessed/Favored: Nothing happens
            if (_totalDispleasure < 200)
                return;

            // Displeased (200-349): Floods + Desert Expansion
            if (_totalDispleasure < 350)
            {
                if (_rng.NextDouble() < 0.05f)
                {
                    double roll = _rng.NextDouble();
                    DivinePunishment punishment;
                    if (roll < 0.5)
                        punishment = DivinePunishment.Flooding;
                    else
                        punishment = DivinePunishment.DesertExpansion;

                    TrySpawnDisaster(punishment);
                }

                // Check for slave revolt if slave labor intensity is high
                if (_slaveLaborIntensity > 0.5f && _rng.NextDouble() < 0.02f)
                {
                    TrySpawnDisaster(DivinePunishment.SlaveRevolt);
                }
                return;
            }

            // Wrathful (350-499): Add plague
            if (_totalDispleasure < 500)
            {
                if (_rng.NextDouble() < 0.07f)
                {
                    double roll = _rng.NextDouble();
                    DivinePunishment punishment;
                    if (roll < 0.33)
                        punishment = DivinePunishment.Flooding;
                    else if (roll < 0.66)
                        punishment = DivinePunishment.DesertExpansion;
                    else
                        punishment = DivinePunishment.Plague;

                    TrySpawnDisaster(punishment);
                }

                if (_slaveLaborIntensity > 0.3f && _rng.NextDouble() < 0.03f)
                {
                    TrySpawnDisaster(DivinePunishment.SlaveRevolt);
                }
                return;
            }

            // Forsaken (500+): All disaster types including cursed ground
            if (_rng.NextDouble() < 0.10f)
            {
                double roll = _rng.NextDouble();
                DivinePunishment punishment;
                if (roll < 0.20)
                    punishment = DivinePunishment.Flooding;
                else if (roll < 0.40)
                    punishment = DivinePunishment.DesertExpansion;
                else if (roll < 0.60)
                    punishment = DivinePunishment.Plague;
                else if (roll < 0.80)
                    punishment = DivinePunishment.SlaveRevolt;
                else
                    punishment = DivinePunishment.CursedGround;

                TrySpawnDisaster(punishment);
            }
        }

        private bool TrySpawnDisaster(DivinePunishment punishment)
        {
            var candidates = GetDisasterCandidates(punishment);
            if (candidates.Count == 0)
                return false;

            var target = candidates[_rng.Next(candidates.Count)];
            SpawnDisasterAt(target, punishment);
            return true;
        }

        private List<BaseTile> GetDisasterCandidates(DivinePunishment punishment)
        {
            var candidates = new List<BaseTile>();
            var disasterTypes = new HashSet<TileType>
            {
                TileType.Flooded,
                TileType.Plague, TileType.SlaveRevolt,
                TileType.CursedGround, TileType.DesertExpansion
            };

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                // Skip core, existing disasters, resources, and temples
                if (tile.Type == TileType.Core ||
                    tile.Type == TileType.Resource ||
                    tile.Type == TileType.Transport ||
                    tile.Type == TileType.Temple ||
                    disasterTypes.Contains(tile.Type))
                    continue;

                // For flooding, prefer tiles near existing floods or at map edges
                if (punishment == DivinePunishment.Flooding)
                {
                    if (IsNearFlood(tile.CellPosition) || IsAtMapEdge(tile.CellPosition))
                    {
                        candidates.Add(tile);
                    }
                }
                // For slave revolt, prefer production tiles
                else if (punishment == DivinePunishment.SlaveRevolt)
                {
                    if (tile.Type == TileType.Production || tile.Type == TileType.Food)
                    {
                        candidates.Add(tile);
                    }
                }
                else
                {
                    // Check spacing from existing disasters
                    if (!IsNearExistingDisaster(tile.CellPosition))
                    {
                        candidates.Add(tile);
                    }
                }
            }

            return candidates;
        }

        private bool IsNearFlood(Vector3Int pos)
        {
            foreach (var neighbor in worldMap.TileData.GetNeighbors(pos))
            {
                if (neighbor is FloodedTile)
                    return true;
            }
            return false;
        }

        private bool IsAtMapEdge(Vector3Int pos)
        {
            var neighbors = HexUtils.GetNeighbors(pos);
            return neighbors.Any(n => worldMap.TileData.GetTile(n) == null);
        }

        private bool IsNearExistingDisaster(Vector3Int pos)
        {
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is DisasterTile)
                {
                    if (HexUtils.Distance(pos, tile.CellPosition) < minDisasterSpacing)
                        return true;
                }
            }
            return false;
        }

        private void SpawnDisasterAt(BaseTile originalTile, DivinePunishment punishment)
        {
            DisasterTile disasterTile;

            switch (punishment)
            {
                case DivinePunishment.Flooding:
                    disasterTile = new FloodedTile(originalTile.CellPosition, _currentTick);
                    break;

                case DivinePunishment.Plague:
                    disasterTile = new PlagueTile(originalTile.CellPosition, _currentTick);
                    break;

                case DivinePunishment.SlaveRevolt:
                    int revoltStrength = Mathf.RoundToInt(20 + _slaveLaborIntensity * 50);
                    disasterTile = new SlaveRevoltTile(originalTile.CellPosition, _currentTick, revoltStrength);
                    break;

                case DivinePunishment.CursedGround:
                    disasterTile = new CursedGroundTile(originalTile.CellPosition, _currentTick);
                    break;

                case DivinePunishment.DesertExpansion:
                default:
                    disasterTile = new DesertExpansionTile(originalTile.CellPosition, _currentTick);
                    break;
            }

            // Replace tile in data grid
            worldMap.TileData.Remove(originalTile.CellPosition);
            worldMap.TileData.Add(disasterTile.CellPosition, disasterTile);

            // Update the visual tile on the tilemap
            worldMap.UpdateTileVisual(disasterTile.CellPosition);

            OnTileDestroyed?.Invoke(originalTile, disasterTile);
            OnDisasterSpawned?.Invoke(disasterTile);
        }

        private void ProcessDisasterSpreading()
        {
            // Process flood spreading
            ProcessFloodSpreading();

            // Process plague spreading
            ProcessPlagueSpreading();

            // Process desert expansion
            ProcessDesertSpreading();

            // Process cursed ground spreading (slower)
            ProcessCursedGroundSpreading();
        }

        private void ProcessFloodSpreading()
        {
            var floodTiles = worldMap.TileData.GetAllTiles()
                .OfType<FloodedTile>()
                .ToList();

            foreach (var flood in floodTiles)
            {
                if (_rng.NextDouble() < 0.05f)
                {
                    TrySpreadDisaster(flood, DivinePunishment.Flooding);
                }
            }
        }

        private void ProcessPlagueSpreading()
        {
            var plagueTiles = worldMap.TileData.GetAllTiles()
                .OfType<PlagueTile>()
                .ToList();

            foreach (var plague in plagueTiles)
            {
                plague.TicksUntilSpread--;
                if (plague.TicksUntilSpread <= 0)
                {
                    if (TrySpreadDisaster(plague, DivinePunishment.Plague))
                    {
                        plague.TicksUntilSpread = 5;
                    }
                }
            }
        }

        private void ProcessDesertSpreading()
        {
            var desertTiles = worldMap.TileData.GetAllTiles()
                .OfType<DesertExpansionTile>()
                .ToList();

            foreach (var desert in desertTiles)
            {
                if (_rng.NextDouble() < 0.03f)
                {
                    TrySpreadDisaster(desert, DivinePunishment.DesertExpansion);
                }
            }
        }

        private void ProcessCursedGroundSpreading()
        {
            var cursedTiles = worldMap.TileData.GetAllTiles()
                .OfType<CursedGroundTile>()
                .ToList();

            foreach (var cursed in cursedTiles)
            {
                if (_rng.NextDouble() < 0.02f)
                {
                    TrySpreadDisaster(cursed, DivinePunishment.CursedGround);
                }
            }
        }

        private bool TrySpreadDisaster(DisasterTile source, DivinePunishment type)
        {
            var disasterTypes = new HashSet<TileType>
            {
                TileType.Flooded,
                TileType.Plague, TileType.SlaveRevolt,
                TileType.CursedGround, TileType.DesertExpansion
            };

            var neighbors = worldMap.TileData.GetNeighbors(source.CellPosition)
                .Where(t => t.Type != TileType.Core &&
                           t.Type != TileType.Transport &&
                           t.Type != TileType.Temple &&
                           !disasterTypes.Contains(t.Type))
                .ToList();

            if (neighbors.Count == 0)
                return false;

            var target = neighbors[_rng.Next(neighbors.Count)];
            SpawnDisasterAt(target, type);
            return true;
        }

        // Public API for other systems
        public void AddDispleasure(int amount)
        {
            _totalDispleasure += amount;
        }

        public void RemoveDispleasure(int amount)
        {
            _totalDispleasure = Mathf.Max(0, _totalDispleasure - amount);
        }

        [Button("Reset Displeasure")]
        public void ResetDispleasure()
        {
            _totalDispleasure = 0;
            _displeasureGeneratedLastTick = 0;
            _favorGeneratedLastTick = 0;
            _currentState = DivineFavorState.Blessed;
            _ticksWithoutTemples = 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.WorldMap;
using CarbonWorld.Features.Grid;
using CarbonWorld.Types;
using Core.Types;

namespace CarbonWorld.Core.Systems
{
    public class CarbonSystem : MonoBehaviour
    {
        public static CarbonSystem Instance { get; private set; }

        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [Title("Tick Settings")]
        [SerializeField, MinValue(0.1f)]
        private float tickInterval = 1f;

        [Title("Carbon Settings")]
        [SerializeField, Tooltip("Base carbon absorbed per nature tile per tick")]
        private int carbonAbsorptionPerNatureTile = 5;

        [SerializeField, Tooltip("Minimum distance between disaster tiles")]
        private int minDisasterSpacing = 2;

        [Title("State (Read Only)")]
        [ShowInInspector, ReadOnly]
        private int _totalCarbon;

        [ShowInInspector, ReadOnly]
        private int _carbonEmittedLastTick;

        [ShowInInspector, ReadOnly]
        private int _carbonAbsorbedLastTick;

        [ShowInInspector, ReadOnly]
        private int _currentTick;

        [ShowInInspector, ReadOnly]
        private string _currentClimateState = "Safe";

        private float _tickTimer;
        private System.Random _rng = new();

        // Events
        public event Action<int, int, int> OnCarbonUpdated; // total, emitted, absorbed
        public event Action<string> OnClimateStateChanged;
        public event Action<DisasterTile> OnDisasterSpawned;
        public event Action<BaseTile, DisasterTile> OnTileDestroyed; // original, replacement

        // Properties
        public int TotalCarbon => _totalCarbon;
        public int CarbonEmittedLastTick => _carbonEmittedLastTick;
        public int CarbonAbsorbedLastTick => _carbonAbsorbedLastTick;
        public string CurrentClimateState => _currentClimateState;
        public int NetCarbonPerTick => _carbonEmittedLastTick - _carbonAbsorbedLastTick;

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

            // Phase 1: Calculate emissions from production
            int emitted = CalculateEmissions();

            // Phase 2: Calculate absorption from nature
            int absorbed = CalculateAbsorption();

            // Phase 3: Update total
            _carbonEmittedLastTick = emitted;
            _carbonAbsorbedLastTick = absorbed;
            _totalCarbon = Mathf.Max(0, _totalCarbon + emitted - absorbed);

            OnCarbonUpdated?.Invoke(_totalCarbon, emitted, absorbed);

            // Phase 4: Update climate state
            string newState = GetClimateState(_totalCarbon);
            if (newState != _currentClimateState)
            {
                _currentClimateState = newState;
                OnClimateStateChanged?.Invoke(_currentClimateState);
            }

            // Phase 5: Process disaster effects based on carbon level
            ProcessDisasterEffects();

            // Phase 6: Process flood spreading
            ProcessFloodSpreading();
        }

        private int CalculateEmissions()
        {
            int total = 0;

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is ProductionTile productionTile && productionTile.IsPowered)
                {
                    foreach (var node in productionTile.Graph.nodes)
                    {
                        if (node.blueprint != null)
                        {
                            total += node.blueprint.CarbonEmission;
                        }
                    }
                }
                else if (tile is PowerTile powerTile && powerTile.TotalPowerOutput > 0)
                {
                    foreach (var node in powerTile.Graph.nodes)
                    {
                        if (node.blueprint != null && node.blueprint.IsPowerGenerator)
                        {
                            total += node.blueprint.CarbonEmission;
                        }
                    }
                }
            }

            return total;
        }

        private int CalculateAbsorption()
        {
            int total = 0;

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is NatureTile natureTile)
                {
                    total += natureTile.GetCarbonAbsorption();
                }
            }

            return total;
        }

        private string GetClimateState(int carbon)
        {
            if (carbon >= 500) return "Catastrophic";
            if (carbon >= 250) return "Critical";
            if (carbon >= 100) return "Warning";
            return "Safe";
        }

        private void ProcessDisasterEffects()
        {
            // 0-100: Nothing happens
            if (_totalCarbon < 100)
                return;

            // 100-250: Refugee camps only, low chance
            if (_totalCarbon < 250)
            {
                if (_rng.NextDouble() < 0.03f) // 3% chance per tick
                {
                    TrySpawnDisaster(ClimateEffect.RefugeeCamp);
                }
                return;
            }

            // 250-500: Refugee camps + floods + heatwaves
            if (_totalCarbon < 500)
            {
                if (_rng.NextDouble() < 0.05f) // 5% chance per tick
                {
                    double roll = _rng.NextDouble();
                    ClimateEffect effect;
                    if (roll < 0.33)
                        effect = ClimateEffect.RefugeeCamp;
                    else if (roll < 0.66)
                        effect = ClimateEffect.Flooding;
                    else
                        effect = ClimateEffect.Heatwave;

                    TrySpawnDisaster(effect);
                }
                return;
            }

            // 500+: All disaster types including dead zones
            if (_rng.NextDouble() < 0.08f) // 8% chance per tick
            {
                // Pick random disaster type
                double roll = _rng.NextDouble();
                ClimateEffect effect;
                if (roll < 0.25)
                    effect = ClimateEffect.RefugeeCamp;
                else if (roll < 0.5)
                    effect = ClimateEffect.Flooding;
                else if (roll < 0.75)
                    effect = ClimateEffect.Heatwave;
                else
                    effect = ClimateEffect.DeadZone;

                TrySpawnDisaster(effect);
            }
        }

        private bool TrySpawnDisaster(ClimateEffect effect)
        {
            var candidates = GetDisasterCandidates(effect);
            if (candidates.Count == 0)
                return false;

            var target = candidates[_rng.Next(candidates.Count)];
            SpawnDisasterAt(target, effect);
            return true;
        }

        private List<BaseTile> GetDisasterCandidates(ClimateEffect effect)
        {
            var candidates = new List<BaseTile>();

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                // Skip core, existing disasters, and resources
                if (tile.Type == TileType.Core ||
                    tile.Type == TileType.Flooded ||
                    tile.Type == TileType.DeadZone ||
                    tile.Type == TileType.RefugeeCamp ||
                    tile.Type == TileType.Heatwave ||
                    tile.Type == TileType.Resource ||
                    tile.Type == TileType.Transport)
                    continue;

                // For flooding, prefer tiles near existing floods or at map edges
                if (effect == ClimateEffect.Flooding)
                {
                    if (IsNearFlood(tile.CellPosition) || IsAtMapEdge(tile.CellPosition))
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

        private void SpawnDisasterAt(BaseTile originalTile, ClimateEffect effect)
        {
            DisasterTile disasterTile;

            switch (effect)
            {
                case ClimateEffect.Flooding:
                    disasterTile = new FloodedTile(originalTile.CellPosition, _currentTick);
                    break;

                case ClimateEffect.RefugeeCamp:
                    int refugeeCount = CoreDemandSystem.Instance?.Population / 10 ?? 50;
                    disasterTile = new RefugeeCampTile(originalTile.CellPosition, _currentTick, refugeeCount);
                    break;

                case ClimateEffect.Heatwave:
                    disasterTile = new HeatwaveTile(originalTile.CellPosition, _currentTick);
                    break;

                case ClimateEffect.DeadZone:
                default:
                    disasterTile = new DeadZoneTile(originalTile.CellPosition, _currentTick);
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

        private void ProcessFloodSpreading()
        {
            var floodTiles = worldMap.TileData.GetAllTiles()
                .OfType<FloodedTile>()
                .ToList();

            foreach (var flood in floodTiles)
            {
                // Floods spread with low probability each tick
                if (_rng.NextDouble() < 0.05f)
                {
                    TrySpreadFlood(flood);
                }
            }
        }

        private void TrySpreadFlood(FloodedTile source)
        {
            var neighbors = worldMap.TileData.GetNeighbors(source.CellPosition)
                .Where(t => t.Type != TileType.Core &&
                           t.Type != TileType.Flooded &&
                           t.Type != TileType.DeadZone &&
                           t.Type != TileType.RefugeeCamp &&
                           t.Type != TileType.Heatwave &&
                           t.Type != TileType.Transport)
                .ToList();

            if (neighbors.Count == 0)
                return;

            var target = neighbors[_rng.Next(neighbors.Count)];
            SpawnDisasterAt(target, ClimateEffect.Flooding);
        }

        // Public API for other systems
        public void AddCarbon(int amount)
        {
            _totalCarbon += amount;
        }

        public void RemoveCarbon(int amount)
        {
            _totalCarbon = Mathf.Max(0, _totalCarbon - amount);
        }

        [Button("Reset Carbon")]
        public void ResetCarbon()
        {
            _totalCarbon = 0;
            _carbonEmittedLastTick = 0;
            _carbonAbsorbedLastTick = 0;
            _currentClimateState = "Safe";
        }
    }
}

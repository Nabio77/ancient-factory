using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.WorldMap;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Core.Systems
{
    public class PowerDistributionSystem : MonoBehaviour
    {
        public static PowerDistributionSystem Instance { get; private set; }

        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [Title("Settings")]
        [SerializeField]
        private float recalculateInterval = 0.5f;

        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        private HashSet<Vector3Int> _poweredPositions = new();

        private float _recalculateTimer;
        private bool _needsRecalculation = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (worldMap == null)
            {
                worldMap = FindFirstObjectByType<WorldMap>();
            }
        }

        private void Start()
        {
            // Subscribe to events and do initial calculation
            SubscribeToEvents();
            RecalculatePower();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (worldMap != null)
            {
                // Unsubscribe first to avoid double subscription
                worldMap.OnTileChanged -= OnTileChanged;
                worldMap.OnMapGenerated -= OnMapGenerated;
                // Subscribe
                worldMap.OnTileChanged += OnTileChanged;
                worldMap.OnMapGenerated += OnMapGenerated;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (worldMap != null)
            {
                worldMap.OnTileChanged -= OnTileChanged;
                worldMap.OnMapGenerated -= OnMapGenerated;
            }
        }

        private void OnTileChanged(Vector3Int position)
        {
            _needsRecalculation = true;
        }

        private void OnMapGenerated()
        {
            // Recalculate immediately on map generation
            RecalculatePower();
        }

        private void Update()
        {
            // Continuously recalculate to catch graph changes in power tiles
            _recalculateTimer += Time.deltaTime;
            if (_recalculateTimer >= recalculateInterval || _needsRecalculation)
            {
                _recalculateTimer = 0f;
                _needsRecalculation = false;
                RecalculatePower();
            }
        }


        [Button("Recalculate Power")]
        public void RecalculatePower()
        {
            _poweredPositions.Clear();

            // Phase 1: Update IO nodes and calculate power output for all power tiles
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is PowerTile powerTile)
                {
                    // Update IO nodes first (needed for fuel input checks)
                    powerTile.UpdateIO(worldMap.TileData);
                    powerTile.CalculatePowerOutput();
                }
            }

            // Phase 2: Collect all powered positions
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is PowerTile powerTile && powerTile.TotalPowerOutput > 0)
                {
                    foreach (var pos in powerTile.GetPoweredPositions())
                    {
                        _poweredPositions.Add(pos);
                    }
                }
            }

            // Phase 3: Update IsPowered on all production tiles and detect power loss
            int lostPowerCount = 0;
            Vector3Int? lastLostPos = null;

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is ProductionTile productionTile)
                {
                    bool wasPowered = productionTile.IsPowered;
                    bool isNowPowered = _poweredPositions.Contains(tile.CellPosition);
                    productionTile.IsPowered = isNowPowered;

                    if (wasPowered && !isNowPowered)
                    {
                        lostPowerCount++;
                        lastLostPos = tile.CellPosition;
                    }
                }
                else if (tile is FoodTile foodTile)
                {
                    bool wasPowered = foodTile.IsPowered;
                    bool isNowPowered = _poweredPositions.Contains(tile.CellPosition);
                    foodTile.IsPowered = isNowPowered;

                    if (wasPowered && !isNowPowered)
                    {
                        lostPowerCount++;
                        lastLostPos = tile.CellPosition;
                    }
                }
            }

            // Trigger Notification
            if (lostPowerCount > 0 && NotificationSystem.Instance != null && Application.isPlaying)
            {
                if (lostPowerCount == 1 && lastLostPos.HasValue)
                {
                    NotificationSystem.Instance.ShowNotification("Power Lost", $"Facility at {lastLostPos.Value} lost power!", NotificationType.Warning);
                }
                else
                {
                    NotificationSystem.Instance.ShowNotification("Power Grid Unstable", $"{lostPowerCount} facilities lost power!", NotificationType.Warning);
                }
            }
        }

        [Button("Debug: Show Power Status")]
        public void DebugShowPowerStatus()
        {
            Debug.Log("=== POWER SYSTEM DEBUG ===");
            Debug.Log($"Total powered positions: {_poweredPositions.Count}");

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is PowerTile powerTile)
                {
                    Debug.Log($"PowerTile at {powerTile.CellPosition}:");
                    Debug.Log($"  - Blueprint nodes: {powerTile.Graph.nodes.Count}");
                    Debug.Log($"  - IO nodes: {powerTile.Graph.ioNodes.Count}");
                    Debug.Log($"  - Connections: {powerTile.Graph.connections.Count}");
                    Debug.Log($"  - TotalPowerOutput: {powerTile.TotalPowerOutput}");
                    Debug.Log($"  - EffectiveRadius: {powerTile.EffectiveRadius}");

                    foreach (var node in powerTile.Graph.nodes)
                    {
                        Debug.Log($"    Blueprint: {node.blueprint?.BlueprintName ?? "null"} (IsPowerGenerator: {node.blueprint?.IsPowerGenerator})");
                        Debug.Log($"    PowerOutput: {node.blueprint?.PowerOutput ?? 0}");
                        Debug.Log($"    Inputs required: {node.blueprint?.Inputs.Count ?? 0}");
                    }
                }
            }

            Debug.Log($"Powered positions: {string.Join(", ", _poweredPositions)}");
        }

        public bool IsPositionPowered(Vector3Int position)
        {
            return _poweredPositions.Contains(position);
        }

        public IReadOnlyCollection<Vector3Int> GetAllPoweredPositions()
        {
            return _poweredPositions;
        }
    }
}

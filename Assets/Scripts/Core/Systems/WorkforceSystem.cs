using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using AncientFactory.Features.Tiles;
using AncientFactory.Features.WorldMap;
using AncientFactory.Core.Types;
using AncientFactory.Features.Grid;
using Drawing;

namespace AncientFactory.Core.Systems
{
    public class WorkforceSystem : MonoBehaviour
    {
        public static WorkforceSystem Instance { get; private set; }

        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [Title("Settings")]
        [SerializeField]
        private float recalculateInterval = 0.5f;

        [SerializeField]
        private int settlementWorkforce = 50;

        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        private int _totalWorkforce;

        [ShowInInspector, ReadOnly]
        private int _usedWorkforce;

        [ShowInInspector, ReadOnly]
        private HashSet<Vector3Int> _servicedPositions = new(); // Positions within commute radius

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
            SubscribeToEvents();
            RecalculateWorkforce();
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
                worldMap.OnTileChanged -= OnTileChanged;
                worldMap.OnMapGenerated -= OnMapGenerated;
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
            RecalculateWorkforce();
        }

        private void Update()
        {
            _recalculateTimer += Time.deltaTime;
            if (_needsRecalculation && _recalculateTimer >= recalculateInterval)
            {
                _recalculateTimer = 0f;
                _needsRecalculation = false;
                RecalculateWorkforce();
            }
        }

        [Button("Recalculate Workforce")]
        public void RecalculateWorkforce()
        {
            _totalWorkforce = 0;
            _usedWorkforce = 0;
            _servicedPositions.Clear();

            var activeProviders = new List<BaseTile>();

            // Phase 1: Identify Active Providers (Settlements + Connected Houses)
            // BFS from Settlements
            var visited = new HashSet<Vector3Int>();
            var queue = new Queue<Vector3Int>();

            // 1a. Start with all Settlements
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is SettlementTile settlement)
                {
                    _totalWorkforce += settlementWorkforce;
                    activeProviders.Add(settlement);

                    queue.Enqueue(settlement.CellPosition);
                    visited.Add(settlement.CellPosition);
                }
            }

            // 1b. Propagate connection to HousingTiles
            while (queue.Count > 0)
            {
                var currentPos = queue.Dequeue();
                var neighbors = HexUtils.GetNeighbors(currentPos);

                foreach (var neighborPos in neighbors)
                {
                    if (visited.Contains(neighborPos)) continue;

                    var neighborTile = worldMap.TileData.GetTile(neighborPos);

                    if (neighborTile is HousingTile housingTile)
                    {
                        visited.Add(neighborPos);
                        queue.Enqueue(neighborPos);

                        // Mark as connected and add workforce
                        housingTile.IsConnectedToSettlement = true;
                        _totalWorkforce += housingTile.Residents;
                        activeProviders.Add(housingTile);
                    }
                }
            }

            // Reset unconnected houses
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is HousingTile house && !visited.Contains(house.CellPosition))
                {
                    house.IsConnectedToSettlement = false;
                }
            }

            // Phase 2: Calculate Serviced Positions (Commute Radius)
            // Union of all active provider radii
            foreach (var provider in activeProviders)
            {
                int radius = (provider is HousingTile h) ? h.CommuteRadius : 2; // Default radius for Settlement too?
                if (provider is SettlementTile) radius = 2; // Hardcode settlement radius for now

                var covered = HexUtils.GetSpiral(provider.CellPosition, radius);
                foreach (var pos in covered)
                {
                    _servicedPositions.Add(pos);
                }
            }

            // Phase 3: Distribute Workforce to Factories
            var candidateFactories = new List<IFactoryTile>();

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is IFactoryTile factory && !(tile is HousingTile)) // HousingTile shouldn't check inputs, but just in case
                {
                    // Must be in serviced area
                    if (_servicedPositions.Contains(tile.CellPosition))
                    {
                        candidateFactories.Add(factory);
                    }
                    else
                    {
                        factory.HasWorkers = false;
                    }
                }
            }

            // Sort factories? (e.g. by establishment date, or just consistent random)
            // For now, rely on iteration order, but sorting by Coordinate ensures determinism
            candidateFactories.Sort((a, b) =>
            {
                var posA = (a as BaseTile).CellPosition;
                var posB = (b as BaseTile).CellPosition;
                if (posA.x != posB.x) return posA.x.CompareTo(posB.x);
                return posA.y.CompareTo(posB.y);
            });

            // Allocate
            int remainingWorkforce = _totalWorkforce;

            foreach (var factory in candidateFactories)
            {
                var factoryTile = factory as FactoryTile;
                if (factoryTile == null) continue;

                // Sum up requirements from all producer nodes
                int req = 0;
                foreach (var node in factoryTile.Graph.nodes)
                {
                    if (node.blueprint != null && node.blueprint.IsProducer)
                    {
                        req += node.blueprint.WorkforceRequirement;
                    }
                }

                if (req == 0)
                {
                    factory.HasWorkers = true; // Free to run
                    continue;
                }

                if (remainingWorkforce >= req)
                {
                    remainingWorkforce -= req;
                    _usedWorkforce += req;
                    factory.HasWorkers = true;
                }
                else
                {
                    factory.HasWorkers = false;
                    // Notification Logic could go here (Not enough workers!)
                }
            }
        }

        public bool IsPositionServiced(Vector3Int position)
        {
            return _servicedPositions.Contains(position);
        }

        public int AvailableWorkforce => _totalWorkforce;
        public int UsedWorkforce => _usedWorkforce;
        public IReadOnlyCollection<Vector3Int> GetServicedPositions() => _servicedPositions;

        public void DrawWorkforceIndicators(IEnumerable<Vector3Int> positions)
        {
            var draw = Drawing.Draw.ingame;

            // Draw borders around ALL given positions (as borders/indicators)
            // Color: Cyan for workforce
            using (draw.WithColor(Color.cyan))
            {
                // Pre-calculate hex points relative to center
                const float radius = 0.55f;
                // Avoid re-allocating array every call if possible, but safe here for clarity
                var hexOffsets = new Vector3[7];
                for (int i = 0; i <= 6; i++)
                {
                    float angle = (30 + 60 * i) * Mathf.Deg2Rad;
                    hexOffsets[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
                }

                foreach (var pos in positions)
                {
                    // Convert position to world
                    // Dependency on WorldMap here
                    var center = worldMap.CellToWorld(pos);

                    var points = new Vector3[7];
                    for (int i = 0; i <= 6; i++)
                    {
                        points[i] = center + hexOffsets[i];
                    }
                    draw.Polyline(points);
                }
            }
        }
    }
}

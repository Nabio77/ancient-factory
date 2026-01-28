using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Types;
using CarbonWorld.Features.WorldMap;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.Grid;

namespace CarbonWorld.Core.Systems
{
    public class SettlementSystem : MonoBehaviour
    {
        public static SettlementSystem Instance { get; private set; }

        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [SerializeField, Required]
        private ItemDatabase itemDatabase;

        [Title("Demand Settings")]
        [SerializeField, MinValue(1), MaxValue(4)]
        private int minDemands = 2;

        [SerializeField, MinValue(1), MaxValue(4)]
        private int maxDemands = 4;

        [SerializeField, MinValue(1)]
        private int baseMinQuantity = 20;

        [SerializeField, MinValue(1)]
        private int baseMaxQuantity = 100;

        [SerializeField, MinValue(0.1f)]
        private float processingInterval = 1f;

        [Title("Distance-Based Tier Settings")]
        [SerializeField, Tooltip("Distance at which settlements get lowest tier items (T1)")]
        private int farDistance = 6;

        [SerializeField, Tooltip("Distance at which settlements get highest tier items (T5)")]
        private int closeDistance = 2;

        [Title("State")]
        [ShowInInspector, ReadOnly]
        private List<SettlementTile> _trackedSettlements = new();

        private Dictionary<int, List<ItemDefinition>> _itemsByTier = new();
        private Vector3Int _corePosition;
        private float _processTimer;

        public event Action<SettlementTile> OnSettlementSatisfied;
        public event Action<SettlementTile, ItemDefinition, int, int> OnDemandProgress;

        public IReadOnlyList<SettlementTile> TrackedSettlements => _trackedSettlements;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadEligibleItems();
        }

        void OnEnable()
        {
            if (worldMap != null)
            {
                worldMap.OnMapGenerated += OnMapGenerated;
            }
        }

        void OnDisable()
        {
            if (worldMap != null)
            {
                worldMap.OnMapGenerated -= OnMapGenerated;
            }
        }

        void Start()
        {
            // Initialize settlements that may already exist
            InitializeExistingSettlements();
        }

        void Update()
        {
            _processTimer += Time.deltaTime;
            if (_processTimer >= processingInterval)
            {
                _processTimer = 0f;
                ProcessSettlementInventories();
            }
        }

        private void LoadEligibleItems()
        {
            if (itemDatabase == null)
            {
                Debug.LogError("SettlementSystem: ItemDatabase reference is missing!");
                return;
            }

            // Group items by tier (T1-T5)
            _itemsByTier.Clear();
            for (int tier = 1; tier <= 5; tier++)
            {
                _itemsByTier[tier] = itemDatabase.Items
                    .Where(item => item.Tier == tier)
                    .ToList();
            }

            int totalItems = _itemsByTier.Values.Sum(list => list.Count);
            if (totalItems == 0)
            {
                Debug.LogWarning("SettlementSystem: No items found in tier range 1-5!");
            }
        }

        private void FindCorePosition()
        {
            if (worldMap == null) return;

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile.Type == TileType.Core)
                {
                    _corePosition = tile.CellPosition;
                    return;
                }
            }
            _corePosition = Vector3Int.zero;
        }

        private void OnMapGenerated()
        {
            _trackedSettlements.Clear();
            InitializeExistingSettlements();
        }

        private void InitializeExistingSettlements()
        {
            if (worldMap == null) return;

            FindCorePosition();

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is SettlementTile settlement)
                {
                    if (settlement.Demands.Count == 0)
                    {
                        GenerateDemandsForSettlement(settlement);
                    }
                    _trackedSettlements.Add(settlement);
                }
            }
        }

        [Button("Regenerate All Settlement Demands")]
        private void RegenerateAllDemands()
        {
            foreach (var settlement in _trackedSettlements)
            {
                settlement.Demands.Clear();
                GenerateDemandsForSettlement(settlement);
            }
        }

        private void GenerateDemandsForSettlement(SettlementTile settlement)
        {
            // Calculate tier range based on distance from core
            int distance = HexUtils.Distance(_corePosition, settlement.CellPosition);
            var (minTier, maxTier) = GetTierRangeForDistance(distance);

            // Collect eligible items from the tier range
            var availableItems = new List<ItemDefinition>();
            for (int tier = minTier; tier <= maxTier; tier++)
            {
                if (_itemsByTier.TryGetValue(tier, out var tierItems))
                {
                    availableItems.AddRange(tierItems);
                }
            }

            if (availableItems.Count == 0) return;

            int demandCount = UnityEngine.Random.Range(minDemands, maxDemands + 1);

            for (int i = 0; i < demandCount && availableItems.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, availableItems.Count);
                var item = availableItems[index];
                availableItems.RemoveAt(index);

                int quantity = GetQuantityForItem(item);
                settlement.Demands.Add(new ItemStack(item, quantity));
            }
        }

        private (int minTier, int maxTier) GetTierRangeForDistance(int distance)
        {
            // Close to core (distance <= closeDistance): T4-T5 (high tier)
            // Far from core (distance >= farDistance): T1-T2 (low tier)
            // In between: interpolate

            if (distance <= closeDistance)
            {
                return (4, 5);
            }
            if (distance >= farDistance)
            {
                return (1, 2);
            }

            // Linear interpolation between close and far
            float t = (float)(distance - closeDistance) / (farDistance - closeDistance);
            // t=0 -> high tier (4-5), t=1 -> low tier (1-2)
            int centerTier = Mathf.RoundToInt(Mathf.Lerp(4.5f, 1.5f, t));
            int minTier = Mathf.Max(1, centerTier - 1);
            int maxTier = Mathf.Min(5, centerTier + 1);

            return (minTier, maxTier);
        }

        private int GetQuantityForItem(ItemDefinition item)
        {
            // Higher tier items require smaller quantities
            float tierFactor = 1f - (item.Tier - 1) * 0.15f;
            tierFactor = Mathf.Max(0.4f, tierFactor);

            int min = Mathf.RoundToInt(baseMinQuantity * tierFactor);
            int max = Mathf.RoundToInt(baseMaxQuantity * tierFactor);

            return UnityEngine.Random.Range(min, max + 1);
        }

        private void ProcessSettlementInventories()
        {
            for (int i = _trackedSettlements.Count - 1; i >= 0; i--)
            {
                var settlement = _trackedSettlements[i];
                if (settlement == null) continue;

                if (settlement.IsSatisfied)
                {
                    OnSettlementSatisfied?.Invoke(settlement);
                    continue;
                }

                ProcessSettlementDemands(settlement);
            }
        }

        private void ProcessSettlementDemands(SettlementTile settlement)
        {
            var inventory = settlement.Inventory;

            foreach (var demand in settlement.Demands)
            {
                if (!demand.IsValid) continue;

                int current = inventory.Get(demand.Item);
                int needed = demand.Amount;

                if (current > 0 && current < needed)
                {
                    OnDemandProgress?.Invoke(settlement, demand.Item, current, needed);
                }
            }
        }

        public List<ItemStack> GetUnfulfilledDemands(SettlementTile settlement)
        {
            var unfulfilled = new List<ItemStack>();
            foreach (var demand in settlement.Demands)
            {
                if (!demand.IsValid) continue;

                int current = settlement.Inventory.Get(demand.Item);
                if (current < demand.Amount)
                {
                    unfulfilled.Add(new ItemStack(demand.Item, demand.Amount - current));
                }
            }
            return unfulfilled;
        }
    }
}

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

        [Title("Growth Settings")]
        [SerializeField]
        private int populationGrowthPerDemand = 5;

        [Title("Distance-Based Tier Settings")]
        [SerializeField, Tooltip("Distance at which settlements get lowest tier items (T1)")]
        private int farDistance = 6;

        [SerializeField, Tooltip("Distance at which settlements get highest tier items (T5)")]
        private int closeDistance = 2;

        [Title("State")]
        [ShowInInspector, ReadOnly]
        private List<SettlementTile> _trackedSettlements = new();

        private Dictionary<int, List<ItemDefinition>> _itemsByTier = new();
        private List<ItemDefinition> _foodItems = new();
        private Vector3Int _corePosition;
        private float _processTimer;

        public event Action<SettlementTile, ItemDefinition, int, int> OnDemandProgress;
        public event Action<SettlementTile> OnSettlementUpdated; // New event for UI

        public IReadOnlyList<SettlementTile> TrackedSettlements => _trackedSettlements;

        public int TotalPopulation => _trackedSettlements.Sum(s => s.Population);

        void Awake()
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

            _foodItems = itemDatabase.GetFoodItems().ToList();

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
                    if (!_trackedSettlements.Contains(settlement))
                    {
                        _trackedSettlements.Add(settlement);
                    }
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

            // Generate regular demands
            int demandCount = UnityEngine.Random.Range(minDemands, maxDemands + 1);

            for (int i = 0; i < demandCount && availableItems.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, availableItems.Count);
                var item = availableItems[index];
                availableItems.RemoveAt(index);

                int quantity = GetQuantityForItem(item);
                
                // Add new demand if not present
                if (!settlement.Demands.Any(d => d.Item == item))
                {
                    settlement.Demands.Add(new ItemStack(item, quantity));
                }
            }

            OnSettlementUpdated?.Invoke(settlement);
        }

        private (int minTier, int maxTier) GetTierRangeForDistance(int distance)
        {
            if (distance <= closeDistance) return (4, 5);
            if (distance >= farDistance) return (1, 2);

            float t = (float)(distance - closeDistance) / (farDistance - closeDistance);
            int centerTier = Mathf.RoundToInt(Mathf.Lerp(4.5f, 1.5f, t));
            int minTier = Mathf.Max(1, centerTier - 1);
            int maxTier = Mathf.Min(5, centerTier + 1);

            return (minTier, maxTier);
        }

        private int GetQuantityForItem(ItemDefinition item)
        {
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

                ProcessSettlementDemands(settlement);
            }
        }

        private void ProcessSettlementDemands(SettlementTile settlement)
        {
            var inventory = settlement.Inventory;

            // Check for completed demands
            for (int i = settlement.Demands.Count - 1; i >= 0; i--)
            {
                var demand = settlement.Demands[i];
                if (!demand.IsValid) continue;

                int currentCount = inventory.Get(demand.Item);
                
                // Report progress
                OnDemandProgress?.Invoke(settlement, demand.Item, currentCount, demand.Amount);

                // Check fulfillment
                if (currentCount >= demand.Amount)
                {
                    FulfillDemand(settlement, demand);
                }
            }
        }

        private void FulfillDemand(SettlementTile settlement, ItemStack demand)
        {
            // 1. Consume Items
            settlement.Inventory.Remove(demand.Item, demand.Amount);

            // 2. Grant Reward (Placeholder)
            Debug.Log($"Settlement at {settlement.CellPosition} fulfilled demand for {demand.Amount} {demand.Item.ItemName}. REWARD GRANTED!");

            // 3. Grow Population
            settlement.Population += populationGrowthPerDemand;
            
            // 4. Update Level/Stats
            settlement.Experience += 10;
            // Simplified leveling logic
            if (settlement.Experience >= settlement.Level * 100)
            {
                settlement.Level++;
                settlement.Experience = 0;
            }

            // 5. Remove completed demand
            settlement.Demands.Remove(demand);

            // 6. Generate New Demand (loop)
            AddNewDemand(settlement);
            
            // UI Update
            OnSettlementUpdated?.Invoke(settlement);
        }

        private void AddNewDemand(SettlementTile settlement)
        {
            // Find a new item to demand
            // In a real system, might be harder based on level
            
            int distance = HexUtils.Distance(_corePosition, settlement.CellPosition);
            var (minTier, maxTier) = GetTierRangeForDistance(distance);
            
            // Get all possible items for this tier
            var eligibleItems = new List<ItemDefinition>();
            for (int tier = minTier; tier <= maxTier; tier++)
            {
                 if (_itemsByTier.TryGetValue(tier, out var list)) eligibleItems.AddRange(list);
            }

            // Filter out items already demanded
            var currentDemands = settlement.Demands.Select(d => d.Item).ToHashSet();
            eligibleItems.RemoveAll(i => currentDemands.Contains(i));

            if (eligibleItems.Count > 0)
            {
                 var newItem = eligibleItems[UnityEngine.Random.Range(0, eligibleItems.Count)];
                 int quantity = GetQuantityForItem(newItem);
                 settlement.Demands.Add(new ItemStack(newItem, quantity));
            }
            else
            {
                // Fallback: just increase quantity of something else or wait
                Debug.Log("SettlementSystem: No new unique items available for demand.");
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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.Core;
using CarbonWorld.Features.WorldMap;
using CarbonWorld.Core.Types;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.Inventories;

namespace CarbonWorld.Core.Systems
{
    public class CoreDemandSystem : MonoBehaviour
    {
        public static CoreDemandSystem Instance { get; private set; }

        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [SerializeField, Required]
        private ItemDatabase itemDatabase;

        [Title("Settings")]
        [SerializeField, MinValue(1)]
        private int baseMinQuantity = 100;

        [SerializeField, MinValue(1)]
        private int baseMaxQuantity = 500;

        [SerializeField, MinValue(1)]
        private int maxActiveDemands = 3;

        [SerializeField, MinValue(0.1f)]
        private float demandCooldown = 3f;

        [SerializeField, MinValue(0.1f)]
        private float initialDelay = 5f;

        [SerializeField, MinValue(1)]
        private int demandsToWin = 10;

        [SerializeField, Tooltip("Core levels up every X demands fulfilled")]
        private int demandsPerLevel = 2;

        [Title("Food Settings")]
        [SerializeField, Tooltip("Food consumed per population per tick")]
        private int foodPerPopulation = 1;

        [SerializeField, Tooltip("Population added per player tile")]
        private int populationPerTile = 10;

        [SerializeField, MinValue(0.1f)]
        private float foodTickInterval = 1f;

        [Title("State")]
        [ShowInInspector, ReadOnly]
        private int _coreLevel;

        [ShowInInspector, ReadOnly]
        private int _totalDemandsFulfilled;

        [ShowInInspector, ReadOnly]
        private List<Demand> _activeDemands = new();

        [ShowInInspector, ReadOnly]
        private bool _victoryAchieved;

        [ShowInInspector, ReadOnly]
        private int _population;

        [ShowInInspector, ReadOnly]
        private int _foodConsumedLastTick;

        [ShowInInspector, ReadOnly]
        private int _foodDeficit;

        private float _demandCooldownTimer;
        private float _foodTickTimer;
        private List<ItemDefinition> _t6Items = new();
        private List<ItemDefinition> _foodItems = new();
        private BaseTile _coreTileData;

        // Events
        public event Action<Demand> OnDemandCreated;
        public event Action<Demand, int, int> OnDemandProgress;
        public event Action<Demand> OnDemandFulfilled;
        public event Action<int> OnCoreLevelUp;
        public event Action OnVictory;
        public event Action<int, int, int> OnFoodUpdated;

        // Properties
        public int CoreLevel => _coreLevel;
        public int TotalDemandsFulfilled => _totalDemandsFulfilled;
        public IReadOnlyList<Demand> ActiveDemands => _activeDemands;
        public bool VictoryAchieved => _victoryAchieved;
        public int DemandsToWin => demandsToWin;
        public int Population => _population;
        public int FoodNeededPerTick => _population * foodPerPopulation;
        public int FoodConsumedLastTick => _foodConsumedLastTick;
        public int FoodDeficit => _foodDeficit;
        public int FoodInStock => _foodItems.Sum(i => _coreTileData?.Inventory.Get(i) ?? 0);
        public IReadOnlyList<ItemDefinition> FoodItems => _foodItems;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadItems();
        }

        void Start()
        {
            _demandCooldownTimer = initialDelay;
        }

        void Update()
        {
            if (_victoryAchieved) return;

            if (_coreTileData == null)
            {
                TryFindCoreTile();
                if (_coreTileData == null) return;
            }

            ProcessDemandGeneration();
            ProcessInventory();
            ProcessFood();
        }

        private void TryFindCoreTile()
        {
            if (worldMap == null) return;

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile.Type == TileType.Core)
                {
                    _coreTileData = tile;
                    return;
                }
            }
        }

        private void LoadItems()
        {
            if (itemDatabase == null)
            {
                Debug.LogError("CoreDemandSystem: ItemDatabase reference is missing!");
                return;
            }

            _t6Items = itemDatabase.GetFinalProducts().ToList();
            _foodItems = itemDatabase.GetFoodItems().ToList();

            if (_t6Items.Count == 0)
            {
                Debug.LogWarning("CoreDemandSystem: No T6 (FinalProduct) items found in ItemDatabase!");
            }

            if (_foodItems.Count == 0)
            {
               Debug.LogWarning("CoreDemandSystem: No Food items found in ItemDatabase!");
            }
        }

        private void ProcessDemandGeneration()
        {
            if (_activeDemands.Count >= maxActiveDemands) return;
            if (_t6Items.Count == 0) return;

            _demandCooldownTimer -= Time.deltaTime;
            if (_demandCooldownTimer <= 0f)
            {
                GenerateNewDemand();
                _demandCooldownTimer = demandCooldown;
            }
        }

        [Button("Generate Demand")]
        private void GenerateNewDemand()
        {
            if (_t6Items.Count == 0) return;

            var item = _t6Items[UnityEngine.Random.Range(0, _t6Items.Count)];
            int quantity = GetQuantityForLevel(_coreLevel);
            var demand = new Demand(item, quantity);
            _activeDemands.Add(demand);

            OnDemandCreated?.Invoke(demand);
        }

        private int GetQuantityForLevel(int level)
        {
            float scale = Mathf.Pow(1.2f, level);
            int min = Mathf.RoundToInt(baseMinQuantity * scale);
            int max = Mathf.RoundToInt(baseMaxQuantity * scale);
            return UnityEngine.Random.Range(min, max + 1);
        }

        private void ProcessInventory()
        {
            var inventory = _coreTileData.Inventory;

            using (new InventoryBatch(inventory, this, "DemandProcessing"))
            {
                foreach (var demand in _activeDemands)
                {
                    if (demand.State != DemandState.Active) continue;

                    int available = inventory.Get(demand.Item);
                    if (available <= 0) continue;

                    int oldAmount = demand.CurrentAmount;
                    int consumed = demand.Contribute(available);

                    if (consumed > 0)
                    {
                        inventory.Remove(demand.Item, consumed);
                        OnDemandProgress?.Invoke(demand, oldAmount, demand.CurrentAmount);
                    }
                }
            }

            for (int i = _activeDemands.Count - 1; i >= 0; i--)
            {
                if (_activeDemands[i].State == DemandState.Fulfilled)
                {
                    HandleDemandFulfilled(_activeDemands[i]);
                    _activeDemands.RemoveAt(i);
                }
            }
        }

        private void HandleDemandFulfilled(Demand demand)
        {
            _totalDemandsFulfilled++;
            OnDemandFulfilled?.Invoke(demand);

            if (_totalDemandsFulfilled % demandsPerLevel == 0)
            {
                _coreLevel++;
                OnCoreLevelUp?.Invoke(_coreLevel);
            }

            if (_totalDemandsFulfilled >= demandsToWin)
            {
                _victoryAchieved = true;
                OnVictory?.Invoke();
            }
        }

        public Demand GetDemandForItem(ItemDefinition item)
        {
            foreach (var demand in _activeDemands)
            {
                if (demand.Item == item && demand.State == DemandState.Active)
                    return demand;
            }
            return null;
        }

        private void ProcessFood()
        {
            if (_foodItems.Count == 0) return;

            _foodTickTimer += Time.deltaTime;
            if (_foodTickTimer < foodTickInterval) return;
            _foodTickTimer = 0f;

            UpdatePopulation();

            // Calculate base food needed plus refugee drain
            int refugeeFoodDrain = CalculateRefugeeFoodDrain();
            int foodNeeded = FoodNeededPerTick + refugeeFoodDrain;

            if (foodNeeded <= 0)
            {
                _foodConsumedLastTick = 0;
                _foodDeficit = 0;
                OnFoodUpdated?.Invoke(_population, 0, 0);
                return;
            }

            var inventory = _coreTileData.Inventory;
            int totalConsumed = 0;

            using (new InventoryBatch(inventory, this, "FoodConsumption"))
            {
                foreach (var foodItem in _foodItems)
                {
                    if (totalConsumed >= foodNeeded) break;

                    int needed = foodNeeded - totalConsumed;
                    // We treat 1 item as 1 unit of food unless we implement nutritional values fully
                    // Using NutritionalValue property
                    int nutrition = foodItem.NutritionalValue > 0 ? foodItem.NutritionalValue : 1;
                    
                    int availableItems = inventory.Get(foodItem);
                    if (availableItems <= 0) continue;

                    // How many items do we need to satisfy 'needed' food points?
                    // Ceil(needed / nutrition)
                    int itemsNeededToEat = Mathf.CeilToInt((float)needed / nutrition);
                    int itemsToEat = Mathf.Min(availableItems, itemsNeededToEat);

                    if (itemsToEat > 0)
                    {
                        inventory.Remove(foodItem, itemsToEat);
                        totalConsumed += itemsToEat * nutrition;
                    }
                }
            }

            _foodConsumedLastTick = totalConsumed;
            _foodDeficit = Mathf.Max(0, foodNeeded - totalConsumed);

            OnFoodUpdated?.Invoke(_population, totalConsumed, _foodDeficit);
        }

        private void UpdatePopulation()
        {
            if (worldMap == null)
            {
                _population = 0;
                return;
            }

            int tileCount = 0;
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile.Type == TileType.Production || 
                    tile.Type == TileType.Power || 
                    tile.Type == TileType.Food || 
                    tile.Type == TileType.Transport)
                {
                    tileCount++;
                }
            }

            _population = tileCount * populationPerTile;
        }

        private int CalculateRefugeeFoodDrain()
        {
            if (worldMap == null) return 0;

            int totalDrain = 0;
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is RefugeeCampTile refugeeCamp)
                {
                    totalDrain += refugeeCamp.FoodDrainPerTick;
                }
            }
            return totalDrain;
        }
    }
}

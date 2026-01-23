using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.Core;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.WorldMap;

namespace CarbonWorld.Core.Systems
{
    public class CoreDemandSystem : MonoBehaviour
    {
        public static CoreDemandSystem Instance { get; private set; }

        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

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
        private ItemDefinition _foodItem;
        private CoreTile _coreTile;

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
        public int FoodInStock => _coreTile?.Inventory.Get(_foodItem) ?? 0;
        public ItemDefinition FoodItem => _foodItem;

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

            if (_coreTile == null)
            {
                TryFindCoreTile();
                if (_coreTile == null) return;
            }

            ProcessDemandGeneration();
            ProcessInventory();
            ProcessFood();
        }

        private void TryFindCoreTile()
        {
            if (worldMap == null) return;

            foreach (var kvp in worldMap.Grid.GetAllTiles())
            {
                if (kvp.Value is CoreTile coreTile)
                {
                    _coreTile = coreTile;
                    return;
                }
            }
        }

        private void LoadItems()
        {
            var allItems = Resources.LoadAll<ItemDefinition>("Items");
            _t6Items = allItems.Where(item => item.IsFinalProduct).ToList();
            _foodItem = allItems.FirstOrDefault(item => item.ItemName == "Food");

            if (_t6Items.Count == 0)
            {
                Debug.LogWarning("CoreDemandSystem: No T6 (FinalProduct) items found in Resources/Items!");
            }

            if (_foodItem == null)
            {
                Debug.LogWarning("CoreDemandSystem: Food item not found in Resources/Items!");
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
            var inventory = _coreTile.Inventory;

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
            if (_foodItem == null) return;

            _foodTickTimer += Time.deltaTime;
            if (_foodTickTimer < foodTickInterval) return;
            _foodTickTimer = 0f;

            UpdatePopulation();

            int foodNeeded = FoodNeededPerTick;
            if (foodNeeded <= 0)
            {
                _foodConsumedLastTick = 0;
                _foodDeficit = 0;
                OnFoodUpdated?.Invoke(_population, 0, 0);
                return;
            }

            var inventory = _coreTile.Inventory;
            int available = inventory.Get(_foodItem);
            int consumed = Mathf.Min(available, foodNeeded);

            if (consumed > 0)
            {
                inventory.Remove(_foodItem, consumed);
            }

            _foodConsumedLastTick = consumed;
            _foodDeficit = foodNeeded - consumed;

            OnFoodUpdated?.Invoke(_population, consumed, _foodDeficit);
        }

        private void UpdatePopulation()
        {
            if (worldMap == null)
            {
                _population = 0;
                return;
            }

            int tileCount = 0;
            foreach (var kvp in worldMap.Grid.GetAllTiles())
            {
                if (kvp.Value is ProductionTile)
                {
                    tileCount++;
                }
            }

            _population = tileCount * populationPerTile;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Systems;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.WorldMap;

namespace CarbonWorld.Features.UI
{
    public class SettlementUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required]
        private UIDocument uiDocument;

        [SerializeField, Required]
        private VisualTreeAsset demandEntryTemplate;

        [SerializeField, Required]
        private TileSelector tileSelector;

        private VisualElement _root;
        private VisualElement _container;
        private Label _nameLabel;
        private Label _populationLabel;
        private Label _growthLabel;
        private Label _foodLevelLabel;
        private VisualElement _demandsContainer;
        
        // State
        private SettlementTile _currentSettlement;

        void Awake()
        {
            _root = uiDocument.rootVisualElement;
            _container = _root.Q<VisualElement>("settlement-container");
            
            _nameLabel = _root.Q<Label>("settlement-name");
            _populationLabel = _root.Q<Label>("settlement-population");
            _growthLabel = _root.Q<Label>("settlement-growth");
            _foodLevelLabel = _root.Q<Label>("settlement-food-level");
            _demandsContainer = _root.Q<VisualElement>("demands-list");

            Hide(); // Hidden by default until selected
        }

        void OnEnable()
        {
            if (SettlementSystem.Instance != null)
            {
                SettlementSystem.Instance.OnSettlementUpdated += OnSettlementUpdated;
                SettlementSystem.Instance.OnDemandProgress += OnDemandProgress;
            }

            if (tileSelector != null)
            {
                tileSelector.OnTileSelected += OnTileSelected;
                tileSelector.OnTileDeselected += OnTileDeselected;
            }
        }

        void OnDisable()
        {
            if (SettlementSystem.Instance != null)
            {
                SettlementSystem.Instance.OnSettlementUpdated -= OnSettlementUpdated;
                SettlementSystem.Instance.OnDemandProgress -= OnDemandProgress;
            }

            if (tileSelector != null)
            {
                tileSelector.OnTileSelected -= OnTileSelected;
                tileSelector.OnTileDeselected -= OnTileDeselected;
            }
        }

        private void OnTileSelected(BaseTile tile)
        {
            if (tile is SettlementTile settlement)
            {
                SelectSettlement(settlement);
            }
            else
            {
                SelectSettlement(null);
            }
        }

        private void OnTileDeselected()
        {
            SelectSettlement(null);
        }

        public void SelectSettlement(SettlementTile settlement)
        {
            _currentSettlement = settlement;
            if (_currentSettlement != null)
            {
                Show();
                RefreshUI();
            }
            else
            {
                Hide();
            }
        }

        private void Show() => _root.style.display = DisplayStyle.Flex;
        private void Hide() => _root.style.display = DisplayStyle.None;

        private void RefreshUI()
        {
            if (_currentSettlement == null) return;

            _nameLabel.text = $"Settlement ({_currentSettlement.CellPosition.x}, {_currentSettlement.CellPosition.y})";
            _populationLabel.text = $"{_currentSettlement.Population}"; 
            _growthLabel.text = $"Lvl {_currentSettlement.Level}";
            
            // Food Status (Find aggregated food stats)
            int currentFood = 0;
            int maxFood = 0;
            
            _demandsContainer.Clear();
            foreach (var demand in _currentSettlement.Demands)
            {
                if (demand.Item.IsFood)
                {
                    currentFood += _currentSettlement.Inventory.Get(demand.Item);
                    maxFood += demand.Amount;
                    // Don't add food to the regular demands list if shown in header?
                    // User request: "We need to show the current food input".
                    // Let's show it in BOTH or just Header? 
                    // Usually header is summary. Let's keep it in list too for detail inview, or skip.
                    // Let's Skip adding to valid demands list to keep it clean if the header covers it.
                    continue; 
                }
                CreateDemandElement(demand);
            }
            
            _foodLevelLabel.text = $"{currentFood} / {maxFood}";
        }

        private void OnSettlementUpdated(SettlementTile settlement)
        {
            if (_currentSettlement == settlement)
            {
                RefreshUI();
            }
        }

        private void CreateDemandElement(ItemStack demand)
        {
            var element = demandEntryTemplate.Instantiate();
            var icon = element.Q<VisualElement>("demand-icon");
            var nameLabel = element.Q<Label>("demand-name"); // Get the name label
            
            if (nameLabel != null)
                nameLabel.text = demand.Item.ItemName; // FIX: Set the item name text

            UpdateElementState(element, _currentSettlement.Inventory.Get(demand.Item), demand.Amount);

            if (demand.Item.Icon != null)
                icon.style.backgroundImage = new StyleBackground(demand.Item.Icon);
            
            _demandsContainer.Add(element);
        }

        private void UpdateElementState(VisualElement element, int current, int required)
        {
            if (element == null) return;
            var label = element.Q<Label>("demand-quantity");
            var bar = element.Q<VisualElement>("demand-progress-fill"); 

            label.text = $"{current}/{required}";
            if (bar != null)
            {
                float pct = Mathf.Clamp01((float)current / required);
                bar.style.width = Length.Percent(pct * 100);
            }
        }

        private void OnDemandProgress(SettlementTile settlement, ItemDefinition item, int current, int required)
        {
            if (_currentSettlement != settlement) return;
            RefreshUI();
        }
    }
}

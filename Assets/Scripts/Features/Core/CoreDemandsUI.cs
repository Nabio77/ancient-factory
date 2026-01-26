using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Systems;
using CarbonWorld.Features.Production;

namespace CarbonWorld.Features.Core
{
    public class CoreDemandsUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required]
        private UIDocument uiDocument;

        [SerializeField, Required]
        private VisualTreeAsset demandEntryTemplate;

        [SerializeField, Required]
        private CoreDemandSystem demandSystem;

        [SerializeField]
        private ProductionGraphEditor graphEditor;

        private VisualElement _root;
        private VisualElement _demandsContainer;
        private Label _coreLevelLabel;
        private Label _demandsFulfilledLabel;
        private VisualElement _victoryProgressFill;

        // Food UI elements
        private Label _populationLabel;
        private Label _foodNeededLabel;
        private Label _foodStockLabel;
        private Label _foodStatusLabel;

        private readonly Dictionary<Demand, VisualElement> _demandElements = new();

        void Awake()
        {
            _root = uiDocument.rootVisualElement;
            _demandsContainer = _root.Q<VisualElement>("demands-container");
            _coreLevelLabel = _root.Q<Label>("core-level");
            _demandsFulfilledLabel = _root.Q<Label>("demands-fulfilled");
            _victoryProgressFill = _root.Q<VisualElement>("victory-progress-fill");

            // Food UI
            _populationLabel = _root.Q<Label>("population");
            _foodNeededLabel = _root.Q<Label>("food-needed");
            _foodStockLabel = _root.Q<Label>("food-stock");
            _foodStatusLabel = _root.Q<Label>("food-status");
        }

        void OnEnable()
        {
            if (demandSystem != null)
            {
                demandSystem.OnDemandCreated += OnDemandCreated;
                demandSystem.OnDemandProgress += OnDemandProgress;
                demandSystem.OnDemandFulfilled += OnDemandFulfilled;
                demandSystem.OnCoreLevelUp += OnCoreLevelUp;
                demandSystem.OnVictory += OnVictory;
                demandSystem.OnFoodUpdated += OnFoodUpdated;
                RefreshAll();
            }

            if (graphEditor != null)
            {
                graphEditor.OnEditorOpened += Hide;
                graphEditor.OnEditorClosed += Show;
            }
        }

        void OnDisable()
        {
            if (demandSystem != null)
            {
                demandSystem.OnDemandCreated -= OnDemandCreated;
                demandSystem.OnDemandProgress -= OnDemandProgress;
                demandSystem.OnDemandFulfilled -= OnDemandFulfilled;
                demandSystem.OnCoreLevelUp -= OnCoreLevelUp;
                demandSystem.OnVictory -= OnVictory;
                demandSystem.OnFoodUpdated -= OnFoodUpdated;
            }

            if (graphEditor != null)
            {
                graphEditor.OnEditorOpened -= Hide;
                graphEditor.OnEditorClosed -= Show;
            }
        }

        private void Show()
        {
            _root.style.display = DisplayStyle.Flex;
        }

        private void Hide()
        {
            _root.style.display = DisplayStyle.None;
        }

        private void RefreshAll()
        {
            UpdateCoreStatus();
            UpdateFoodStatus();

            _demandsContainer.Clear();
            _demandElements.Clear();

            foreach (var demand in demandSystem.ActiveDemands)
            {
                CreateDemandElement(demand);
            }
        }

        private void UpdateCoreStatus()
        {
            _coreLevelLabel.text = demandSystem.CoreLevel.ToString();
            _demandsFulfilledLabel.text = $"{demandSystem.TotalDemandsFulfilled}/{demandSystem.DemandsToWin} Demands Fulfilled";

            float victoryProgress = (float)demandSystem.TotalDemandsFulfilled / demandSystem.DemandsToWin;
            _victoryProgressFill.style.width = Length.Percent(victoryProgress * 100);
        }

        private void OnDemandCreated(Demand demand)
        {
            CreateDemandElement(demand);
        }

        private void CreateDemandElement(Demand demand)
        {
            var element = demandEntryTemplate.Instantiate();

            var icon = element.Q<VisualElement>("demand-icon");
            var nameLabel = element.Q<Label>("demand-name");

            if (demand.Item != null)
            {
                nameLabel.text = demand.Item.ItemName;

                if (demand.Item.Icon != null)
                {
                    icon.style.backgroundImage = new StyleBackground(demand.Item.Icon);
                }
            }

            UpdateDemandElement(element, demand);

            _demandsContainer.Add(element);
            _demandElements[demand] = element;
        }

        private void UpdateDemandElement(VisualElement element, Demand demand)
        {
            var quantityLabel = element.Q<Label>("demand-quantity");
            var progressFill = element.Q<VisualElement>("demand-progress-fill");

            quantityLabel.text = $"{demand.CurrentAmount} / {demand.RequiredAmount}";
            progressFill.style.width = Length.Percent(demand.Progress * 100);
        }

        private void OnDemandProgress(Demand demand, int oldAmount, int newAmount)
        {
            if (_demandElements.TryGetValue(demand, out var element))
            {
                UpdateDemandElement(element, demand);
            }
        }

        private void OnDemandFulfilled(Demand demand)
        {
            if (_demandElements.TryGetValue(demand, out var element))
            {
                element.AddToClassList("fulfilled");

                element.schedule.Execute(() =>
                {
                    _demandsContainer.Remove(element);
                    _demandElements.Remove(demand);
                }).StartingIn(500);
            }

            UpdateCoreStatus();
        }

        private void OnCoreLevelUp(int newLevel)
        {
            UpdateCoreStatus();
        }

        private void OnVictory()
        {
            Debug.Log("VICTORY! All demands fulfilled!");
        }

        private void OnFoodUpdated(int population, int consumed, int deficit)
        {
            UpdateFoodStatus();
        }

        private void UpdateFoodStatus()
        {
            _populationLabel.text = demandSystem.Population.ToString();
            _foodNeededLabel.text = demandSystem.FoodNeededPerTick.ToString();
            _foodStockLabel.text = demandSystem.FoodInStock.ToString();

            bool isStarving = demandSystem.FoodDeficit > 0;
            _foodStatusLabel.text = isStarving ? "Starving!" : "Fed";
            _foodStatusLabel.RemoveFromClassList("fed");
            _foodStatusLabel.RemoveFromClassList("starving");
            _foodStatusLabel.AddToClassList(isStarving ? "starving" : "fed");
        }
    }
}

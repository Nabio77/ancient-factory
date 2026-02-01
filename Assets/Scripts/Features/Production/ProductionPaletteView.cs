using System;
using UnityEngine;
using UnityEngine.UIElements;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Systems;
using CarbonWorld.Features.WorldMap;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Features.Production
{
    public class ProductionPaletteView
    {
        private readonly VisualElement _root;
        private readonly ScrollView _palettePanel;
        private readonly VisualElement _tabsContainer;
        private readonly BlueprintDatabase _database;
        private readonly VisualTreeAsset _cardTemplate;
        private readonly ProductionCanvasView _canvasView;

        // Palette Drag State
        private bool _isDraggingFromPalette;
        private BlueprintDefinition _paletteDragBlueprint;
        private VisualElement _paletteDragGhost;

        // Blueprint Filters
        private Func<BlueprintDefinition, bool> _contextFilter;
        private Func<BlueprintDefinition, bool> _categoryFilter;

        public bool IsDragging => _isDraggingFromPalette;

        public ProductionPaletteView(
            VisualElement root,
            ScrollView palettePanel,
            BlueprintDatabase database,
            VisualTreeAsset cardTemplate,
            ProductionCanvasView canvasView)
        {
            _root = root;
            _palettePanel = palettePanel;
            _tabsContainer = root.Q<VisualElement>("palette-tabs-container");
            _database = database;
            _cardTemplate = cardTemplate;
            _canvasView = canvasView;

            // Default filters
            _contextFilter = b => true;
            _categoryFilter = b => true; // All

            BindTabs();
            InitializePalette();
        }

        public void SetBlueprintFilter(Func<BlueprintDefinition, bool> filter)
        {
            _contextFilter = filter ?? (b => true);
            InitializePalette();
        }

        public void Refresh()
        {
            InitializePalette();
        }

        private void BindTabs()
        {
            if (_tabsContainer == null) return;
            
            _tabsContainer.Clear();

            // Add "All" Tab
            CreateTab("All", b => true, true);

            // Add Tab for each BlueprintType
            foreach (BlueprintType type in Enum.GetValues(typeof(BlueprintType)))
            {
                CreateTab(type.ToString(), b => b.Type == type);
            }
        }

        private void CreateTab(string text, Func<BlueprintDefinition, bool> filter, bool isSelected = false)
        {
            var tab = new Label(text);
            tab.AddToClassList("palette-tab");
            if (isSelected) tab.AddToClassList("selected");
            
            tab.RegisterCallback<MouseDownEvent>(evt => SelectTab(tab, filter));
            _tabsContainer.Add(tab);
        }

        private void SelectTab(VisualElement tab, Func<BlueprintDefinition, bool> filter)
        {
            foreach (var child in _tabsContainer.Children())
                child.RemoveFromClassList("selected");

            tab.AddToClassList("selected");
            _categoryFilter = filter;
            InitializePalette();
        }

        private void InitializePalette()
        {
            var content = _palettePanel.Q<VisualElement>("palette-content");
            content.Clear();

            foreach (var blueprint in _database.Blueprints)
            {
                // Apply both context filter (game logic) and category filter (user UI)
                if (!_contextFilter(blueprint) || !_categoryFilter(blueprint))
                    continue;

                // Check tech tree unlock status
                if (TechTreeSystem.Instance != null && !TechTreeSystem.Instance.IsBlueprintUnlocked(blueprint))
                    continue;

                var item = new VisualElement();
                item.AddToClassList("palette-item");
                item.AddToClassList($"type-{blueprint.Type}");

                var icon = new VisualElement();
                icon.AddToClassList("palette-icon");
                if (blueprint.Icon != null)
                    icon.style.backgroundImage = new StyleBackground(blueprint.Icon);
                item.Add(icon);

                var label = new Label(blueprint.BlueprintName);
                label.AddToClassList("palette-label");
                item.Add(label);

                var typeLabel = new Label(blueprint.Type.ToString());
                typeLabel.AddToClassList("palette-type");
                item.Add(typeLabel);

                // Drag from palette
                var bp = blueprint;
                item.RegisterCallback<MouseDownEvent>(evt => StartPaletteDrag(evt, bp));

                content.Add(item);
            }
        }

        private void StartPaletteDrag(MouseDownEvent evt, BlueprintDefinition blueprint)
        {
            if (_canvasView.CurrentGraph == null) return;

            _isDraggingFromPalette = true;
            _paletteDragBlueprint = blueprint;

            // Create ghost as a card preview
            _paletteDragGhost = _cardTemplate.Instantiate();
            _paletteDragGhost.AddToClassList($"type-{blueprint.Type}");
            _paletteDragGhost.style.position = Position.Absolute;
            _paletteDragGhost.style.opacity = 0.8f;
            _paletteDragGhost.pickingMode = PickingMode.Ignore;

            // Populate card with blueprint data
            _paletteDragGhost.Q<Label>("card-title").text = blueprint.BlueprintName;
            _paletteDragGhost.Q<Label>("card-type").text = blueprint.Type.ToString();
            if (blueprint.Icon != null)
                _paletteDragGhost.Q<VisualElement>("card-icon").style.backgroundImage = new StyleBackground(blueprint.Icon);

            var mousePos = evt.mousePosition;
            _paletteDragGhost.style.left = mousePos.x - 110;
            _paletteDragGhost.style.top = mousePos.y - 50;

            _root.Add(_paletteDragGhost);
            _root.CaptureMouse();
            evt.StopPropagation();
        }

        public void OnRootMouseMove(MouseMoveEvent evt)
        {
            if (_isDraggingFromPalette && _paletteDragGhost != null)
            {
                var mousePos = evt.mousePosition;
                _paletteDragGhost.style.left = mousePos.x - 110;
                _paletteDragGhost.style.top = mousePos.y - 50;
            }
        }

        public void OnRootMouseUp(MouseUpEvent evt)
        {
            if (_isDraggingFromPalette)
            {
                // Check if mouse is over canvas
                var canvasBounds = _canvasView.Canvas.worldBound;
                if (canvasBounds.Contains(evt.mousePosition))
                {
                    // Convert to content coordinates (accounting for zoom/pan)
                    var canvasLocal = _canvasView.Canvas.WorldToLocal(evt.mousePosition);
                    var contentPos = _canvasView.ScreenToContent(canvasLocal);
                    
                    var node = new BlueprintNode(_paletteDragBlueprint, contentPos - new Vector2(110, 50));
                    _canvasView.CurrentGraph.nodes.Add(node);
                    _canvasView.CreateNodeUI(node);
                }

                CleanupDrag();
                _root.ReleaseMouse();
            }
        }

        public void CleanupDrag()
        {
            if (_paletteDragGhost != null)
            {
                _paletteDragGhost.RemoveFromHierarchy();
                _paletteDragGhost = null;
            }
            _isDraggingFromPalette = false;
            _paletteDragBlueprint = null;
        }
    }
}
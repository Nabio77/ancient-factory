using System;
using UnityEngine;
using UnityEngine.UIElements;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.WorldMap;

namespace CarbonWorld.Features.Production
{
    public class ProductionPaletteView
    {
        private readonly VisualElement _root;
        private readonly ScrollView _palettePanel;
        private readonly BlueprintDatabase _database;
        private readonly VisualTreeAsset _cardTemplate;
        private readonly ProductionCanvasView _canvasView;

        // Palette Drag State
        private bool _isDraggingFromPalette;
        private BlueprintDefinition _paletteDragBlueprint;
        private VisualElement _paletteDragGhost;

        // Blueprint Filter
        private Func<BlueprintDefinition, bool> _blueprintFilter;

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
            _database = database;
            _cardTemplate = cardTemplate;
            _canvasView = canvasView;

            // Default filter shows all production/logistics blueprints
            _blueprintFilter = b => b.IsProducer || b.IsLogistics;
            InitializePalette();
        }

        public void SetBlueprintFilter(Func<BlueprintDefinition, bool> filter)
        {
            _blueprintFilter = filter ?? (b => true);
            InitializePalette();
        }

        private void InitializePalette()
        {
            var content = _palettePanel.Q<VisualElement>("palette-content");
            content.Clear();

            foreach (var blueprint in _database.Blueprints)
            {
                // Skip blueprints that don't match the current filter
                if (!_blueprintFilter(blueprint))
                    continue;
                var item = new VisualElement();
                item.AddToClassList("palette-item");

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
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.WorldMap;

namespace CarbonWorld.Features.Production
{
    public class ProductionGraphEditor : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required]
        private UIDocument uiDocument;

        [SerializeField, Required]
        private TileSelector tileSelector;

        [SerializeField, Required]
        private WorldMapCamera worldMapCamera;

        [SerializeField, Required]
        private BlueprintDatabase database;

        [SerializeField, Required]
        private VisualTreeAsset cardTemplate;

        [SerializeField, Required]
        private VisualTreeAsset tileIOCardTemplate;

        [SerializeField, Required]
        private WorldMap.WorldMap worldMap;

        private VisualElement _root;
        private VisualElement _canvas;
        private ScrollView _palettePanel;
        private Button _closeButton;

        private ProductionCanvasView _canvasView;
        private ProductionGraphInput _input;
        private ProductionIOView _ioView;
        private ProductionPaletteView _paletteView;

        private ProductionTile _currentTile;

        void Awake()
        {
            if (uiDocument == null) return;
            _root = uiDocument.rootVisualElement.Q<VisualElement>("root");
            _root.AddToClassList("hidden"); // Start hidden at runtime
            
            _canvas = _root.Q<VisualElement>("graph-canvas");
            _palettePanel = _root.Q<ScrollView>("palette-panel");
            _closeButton = _root.Q<Button>("close-button");

            _closeButton.clicked += Hide;

            // Initialize Sub-Systems
            InitializeSubSystems();

            // Root-level events for palette drag
            _root.RegisterCallback<MouseMoveEvent>(_paletteView.OnRootMouseMove);
            _root.RegisterCallback<MouseUpEvent>(evt => _paletteView.OnRootMouseUp(evt));
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void InitializeSubSystems()
        {
            // 1. View
            _canvasView = new ProductionCanvasView(_canvas, cardTemplate);

            // 2. Interaction
            _input = new ProductionGraphInput(_canvasView, _canvas);
            _input.OnGraphChanged += OnGraphChanged;
            _canvasView.Input = _input;

            // 3. IO Manager
            _ioView = new ProductionIOView(worldMap, tileIOCardTemplate, _canvasView);
            _canvasView.IOView = _ioView;

            // 4. Palette
            _paletteView = new ProductionPaletteView(_root, _palettePanel, database, cardTemplate, _canvasView);
        }

        private void OnGraphChanged()
        {
            if (_currentTile != null)
            {
                _ioView.PopulateIOCards(_currentTile);
                _root.schedule.Execute(() => _canvasView.MarkConnectionsDirty()).ExecuteLater(50);
            }
        }

        void OnEnable()
        {
            if (tileSelector != null)
                tileSelector.OnTileSelected += OnTileSelected;
        }

        void OnDisable()
        {
            if (tileSelector != null)
                tileSelector.OnTileSelected -= OnTileSelected;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Hide();
                evt.StopPropagation();
            }
        }

        private void OnTileSelected(BaseTile tile)
        {
            if (tile is ProductionTile productionTile)
            {
                Show(productionTile);
            }
        }

        public void Show(ProductionTile tile)
        {
            if (tileSelector != null && _currentTile == null)
                tileSelector.OnTileSelected -= OnTileSelected;

            if (worldMapCamera != null)
            {
                worldMapCamera.SavePosition();
                worldMapCamera.InputEnabled = false;
            }

            _currentTile = tile;
            _root.RemoveFromClassList("hidden");

            _ioView.CreateIOZones(_root);
            _canvasView.SetGraph(tile.Graph);
            _ioView.PopulateIOCards(tile);
            
            // Re-render connections after layout
            _root.schedule.Execute(() => _canvasView.MarkConnectionsDirty()).ExecuteLater(50);
        }

        public void Hide()
        {
            _input.ResetState();
            _paletteView.CleanupDrag();

            _root.Blur();
            _root.AddToClassList("hidden");

            if (worldMapCamera != null)
            {
                worldMapCamera.RestorePosition();
                worldMapCamera.InputEnabled = true;
            }

            if (tileSelector != null && _currentTile != null)
                tileSelector.OnTileSelected += OnTileSelected;

            _currentTile = null;
            _canvasView.Clear();
            _ioView.Cleanup();
        }
        
        // Ensure we clean up listeners on destroy
        private void OnDestroy()
        {
            if (_input != null)
                _input.OnGraphChanged -= OnGraphChanged;
            _input?.Cleanup();
        }
    }
}

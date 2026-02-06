using System;
using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using AncientFactory.Core.Data;
using AncientFactory.Features.Tiles;
using AncientFactory.Features.WorldMap;
using AncientFactory.Core.Systems;

namespace AncientFactory.Features.Factory
{
    public class FactoryGraphEditor : MonoBehaviour
    {
        public event Action OnEditorOpened;
        public event Action OnEditorClosed;

        [Title("References")]
        [SerializeField, Required]
        private UIDocument uiDocument;

        [SerializeField, Required]
        private TileSelector tileSelector;

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

        private FactoryCanvasView _canvasView;
        private FactoryGraphInput _input;
        private FactoryIOView _ioView;
        private FactoryPaletteView _paletteView;

        private IGraphTile _currentGraphTile;
        private BaseTile _currentTile;

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
            _canvasView = new FactoryCanvasView(_canvas, cardTemplate);

            // 2. Interaction
            _input = new FactoryGraphInput(_canvasView, _canvas);
            _input.OnGraphChanged += OnGraphChanged;
            _canvasView.Input = _input;

            // 3. IO Manager
            _ioView = new FactoryIOView(worldMap, tileIOCardTemplate, _canvasView);
            _canvasView.IOView = _ioView;

            // 4. Palette
            _paletteView = new FactoryPaletteView(_root, _palettePanel, database, cardTemplate, _canvasView);
        }

        private void OnGraphChanged()
        {
            if (_currentGraphTile != null)
            {
                // Force backend update to recalculate outputs/inputs
                if (worldMap != null && worldMap.GraphSystem != null && _currentTile != null)
                {
                    worldMap.GraphSystem.UpdateTile(worldMap.TileData, _currentTile);
                }

                _ioView.PopulateIOCards(_currentGraphTile);
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
            if (tile is IGraphTile graphTile)
            {
                Show(graphTile, tile);
            }
        }

        public void Show(IGraphTile graphTile, BaseTile tile)
        {
            if (tileSelector != null && _currentTile == null)
                tileSelector.OnTileSelected -= OnTileSelected;

            if (InterfaceSystem.Instance != null)
                InterfaceSystem.Instance.SetState(InterfaceState.FactoryEditor);

            _currentGraphTile = graphTile;
            _currentGraphTile.Graph.OnGraphUpdated += OnExternalGraphUpdate;
            _currentTile = tile;
            _root.RemoveFromClassList("hidden");

            // Ensure inputs are up-to-date before showing
            if (worldMap != null && worldMap.GraphSystem != null)
            {
                worldMap.GraphSystem.UpdateTile(worldMap.TileData, tile);
            }

            _ioView.CreateIOZones(_root, graphTile.HasOutput);
            _canvasView.SetGraph(graphTile.Graph);
            // _paletteView.SetBlueprintFilter(graphTile.BlueprintFilter); // Allow all blueprints, filtered by tabs

            // Schedule refresh to ensure layout is ready after un-hiding
            _root.schedule.Execute(() => _paletteView.Refresh());

            _ioView.PopulateIOCards(graphTile);

            // Re-render connections after layout
            _root.schedule.Execute(() => _canvasView.MarkConnectionsDirty()).ExecuteLater(50);

            OnEditorOpened?.Invoke();
        }

        public void Hide()
        {
            if (_currentGraphTile != null)
            {
                _currentGraphTile.Graph.OnGraphUpdated -= OnExternalGraphUpdate;
            }

            _input.ResetState();
            _paletteView.CleanupDrag();

            _root.Blur();
            _root.AddToClassList("hidden");

            if (InterfaceSystem.Instance != null)
                InterfaceSystem.Instance.SetState(InterfaceState.Gameplay);

            if (tileSelector != null && _currentTile != null)
                tileSelector.OnTileSelected += OnTileSelected;

            _currentGraphTile = null;
            _currentTile = null;
            _canvasView.Clear();
            _ioView.Cleanup();

            OnEditorClosed?.Invoke();
        }

        private void OnExternalGraphUpdate()
        {
            if (_currentGraphTile != null)
            {
                // Refresh IO cards and connections
                _ioView.PopulateIOCards(_currentGraphTile);
                _root.schedule.Execute(() => _canvasView.MarkConnectionsDirty()).ExecuteLater(50);
            }
        }

        // Ensure we clean up listeners on destroy
        private void OnDestroy()
        {
            if (_currentGraphTile != null)
            {
                _currentGraphTile.Graph.OnGraphUpdated -= OnExternalGraphUpdate;
            }
            if (_input != null)
                _input.OnGraphChanged -= OnGraphChanged;
            _input?.Cleanup();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.WorldMap;

namespace CarbonWorld.Features.Production
{
    public class ProductionUI : MonoBehaviour
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

        [Title("Settings")]
        [SerializeField]
        private Color connectionColor = new Color(0f, 0.67f, 1f);
        [SerializeField]
        private float lineWidth = 3f;

        private VisualElement _root;
        private VisualElement _canvas;
        private VisualElement _canvasContent;
        private VisualElement _connectionsLayer;
        private ScrollView _palettePanel;
        private Button _closeButton;

        private ProductionTile _currentTile;
        private BlueprintGraph _currentGraph;

        // Zoom & Pan State
        private float _zoom = 1f;
        private Vector2 _pan = Vector2.zero;
        private bool _isPanning;
        private Vector2 _panStartMouse;
        private Vector2 _panStartOffset;
        private const float MinZoom = 0.25f;
        private const float MaxZoom = 2f;
        private const float ZoomSpeed = 0.1f;
        
        // Interaction State
        private bool _isDraggingNode;
        private VisualElement _draggedNodeElement;
        private BlueprintNode _draggedNodeData;
        private Vector2 _dragOffset;

        private bool _isConnecting;
        private string _connectionStartNodeId;
        private int _connectionStartPortIndex;
        private Vector2 _connectionStartPos;
        private Vector2 _currentMousePos;

        // Palette Drag State
        private bool _isDraggingFromPalette;
        private BlueprintDefinition _paletteDragBlueprint;
        private VisualElement _paletteDragGhost;

        // UI Element Maps
        private Dictionary<string, VisualElement> _nodeElements = new();
        private Dictionary<string, List<VisualElement>> _inputPorts = new();
        private Dictionary<string, List<VisualElement>> _outputPorts = new();

        void Awake()
        {
            if (uiDocument == null) return;
            _root = uiDocument.rootVisualElement.Q<VisualElement>("root");
            _canvas = _root.Q<VisualElement>("graph-canvas");
            _palettePanel = _root.Q<ScrollView>("palette-panel");
            _closeButton = _root.Q<Button>("close-button");

            _closeButton.clicked += Hide;

            // Create content container for zoom/pan
            _canvasContent = new VisualElement { name = "canvas-content" };
            _canvasContent.style.position = Position.Absolute;
            _canvasContent.style.transformOrigin = new TransformOrigin(0, 0);
            _canvas.Add(_canvasContent);

            // Create layer for lines (inside content for zoom/pan)
            // Use large fixed size to cover panning range
            _connectionsLayer = new VisualElement { name = "connections-layer" };
            _connectionsLayer.style.position = Position.Absolute;
            _connectionsLayer.style.left = -5000;
            _connectionsLayer.style.top = -5000;
            _connectionsLayer.style.width = 10000;
            _connectionsLayer.style.height = 10000;
            _connectionsLayer.pickingMode = PickingMode.Ignore;
            _connectionsLayer.generateVisualContent += OnGenerateConnections;
            _canvasContent.Add(_connectionsLayer);

            // Canvas Events
            _canvas.RegisterCallback<MouseDownEvent>(OnCanvasMouseDown);
            _canvas.RegisterCallback<MouseMoveEvent>(OnCanvasMouseMove);
            _canvas.RegisterCallback<MouseUpEvent>(OnCanvasMouseUp);
            _canvas.RegisterCallback<WheelEvent>(OnCanvasWheel);

            // Root-level events for palette drag
            _root.RegisterCallback<MouseMoveEvent>(OnRootMouseMove);
            _root.RegisterCallback<MouseUpEvent>(OnRootMouseUp);
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);

            InitializePalette();
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

        private void InitializePalette()
        {
            var content = _palettePanel.Q<VisualElement>("palette-content");
            content.Clear();

            foreach (var blueprint in database.Blueprints)
            {
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
            if (_currentGraph == null) return;

            _isDraggingFromPalette = true;
            _paletteDragBlueprint = blueprint;

            // Create ghost element
            _paletteDragGhost = new VisualElement();
            _paletteDragGhost.AddToClassList("palette-drag-ghost");
            _paletteDragGhost.style.position = Position.Absolute;
            _paletteDragGhost.style.width = 120;
            _paletteDragGhost.style.height = 40;
            _paletteDragGhost.style.backgroundColor = new Color(0.2f, 0.5f, 0.8f, 0.8f);
            _paletteDragGhost.style.borderTopLeftRadius = 4;
            _paletteDragGhost.style.borderTopRightRadius = 4;
            _paletteDragGhost.style.borderBottomLeftRadius = 4;
            _paletteDragGhost.style.borderBottomRightRadius = 4;
            _paletteDragGhost.pickingMode = PickingMode.Ignore;

            var ghostLabel = new Label(blueprint.BlueprintName);
            ghostLabel.style.color = Color.white;
            ghostLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            ghostLabel.style.flexGrow = 1;
            _paletteDragGhost.Add(ghostLabel);

            var mousePos = evt.mousePosition;
            _paletteDragGhost.style.left = mousePos.x - 60;
            _paletteDragGhost.style.top = mousePos.y - 20;

            _root.Add(_paletteDragGhost);
            _root.CaptureMouse();
            evt.StopPropagation();
        }

        private void OnRootMouseMove(MouseMoveEvent evt)
        {
            if (_isDraggingFromPalette && _paletteDragGhost != null)
            {
                var mousePos = evt.mousePosition;
                _paletteDragGhost.style.left = mousePos.x - 60;
                _paletteDragGhost.style.top = mousePos.y - 20;
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Hide();
                evt.StopPropagation();
            }
        }

        private void OnRootMouseUp(MouseUpEvent evt)
        {
            if (_isDraggingFromPalette)
            {
                // Check if mouse is over canvas
                var canvasBounds = _canvas.worldBound;
                if (canvasBounds.Contains(evt.mousePosition))
                {
                    // Convert to content coordinates (accounting for zoom/pan)
                    var canvasLocal = _canvas.WorldToLocal(evt.mousePosition);
                    var contentPos = ScreenToContent(canvasLocal);
                    var node = new BlueprintNode(_paletteDragBlueprint, contentPos - new Vector2(110, 50));
                    _currentGraph.nodes.Add(node);
                    CreateNodeUI(node);
                }

                // Cleanup
                if (_paletteDragGhost != null)
                {
                    _paletteDragGhost.RemoveFromHierarchy();
                    _paletteDragGhost = null;
                }
                _isDraggingFromPalette = false;
                _paletteDragBlueprint = null;
                _root.ReleaseMouse();
            }
        }

        private void OnTileSelected(Tile tile)
        {
            if (tile is ProductionTile productionTile)
            {
                Show(productionTile);
            }
        }

        public void Show(ProductionTile tile)
        {
            // Unsubscribe from tile selection while editor is open to prevent
            // accidental closes from clicks passing through to the world
            if (tileSelector != null && _currentTile == null)
                tileSelector.OnTileSelected -= OnTileSelected;

            // Disable camera input and save position
            if (worldMapCamera != null)
            {
                worldMapCamera.SavePosition();
                worldMapCamera.InputEnabled = false;
            }

            _currentTile = tile;
            _currentGraph = tile.Graph;
            _root.AddToClassList("visible");

            RebuildGraphUI();
        }

        public void Hide()
        {
            // Reset all interaction state
            if (_isPanning || _isDraggingNode || _isConnecting)
            {
                _canvas.ReleaseMouse();
            }
            _isPanning = false;
            _isDraggingNode = false;
            _draggedNodeElement = null;
            _isConnecting = false;

            if (_isDraggingFromPalette)
            {
                _root.ReleaseMouse();
                if (_paletteDragGhost != null)
                {
                    _paletteDragGhost.RemoveFromHierarchy();
                    _paletteDragGhost = null;
                }
            }
            _isDraggingFromPalette = false;
            _paletteDragBlueprint = null;

            // Release focus
            _root.Blur();

            _root.RemoveFromClassList("visible");

            // Restore camera position and re-enable input
            if (worldMapCamera != null)
            {
                worldMapCamera.RestorePosition();
                worldMapCamera.InputEnabled = true;
            }

            // Resubscribe to tile selection
            if (tileSelector != null && _currentTile != null)
                tileSelector.OnTileSelected += OnTileSelected;

            _currentTile = null;
            _currentGraph = null;
            _nodeElements.Clear();
            _inputPorts.Clear();
            _outputPorts.Clear();
        }

        private void RebuildGraphUI()
        {
            // Clear existing nodes (except connections layer)
            for (int i = _canvasContent.childCount - 1; i >= 0; i--)
            {
                if (_canvasContent[i] != _connectionsLayer)
                {
                    _canvasContent.RemoveAt(i);
                }
            }

            _nodeElements.Clear();
            _inputPorts.Clear();
            _outputPorts.Clear();

            // Reset view
            ResetView();

            if (_currentGraph == null) return;

            foreach (var node in _currentGraph.nodes)
            {
                CreateNodeUI(node);
            }

            // Schedule repaint to ensure layout is ready for connection drawing
            _root.schedule.Execute(() => _connectionsLayer.MarkDirtyRepaint()).ExecuteLater(50);
        }

        private void CreateNodeUI(BlueprintNode node)
        {
            var card = cardTemplate.Instantiate();
            var root = card.Q<VisualElement>(className: "blueprint-card");
            var bp = node.blueprint;

            // Header
            root.Q<Label>("card-title").text = bp.BlueprintName;
            root.Q<Label>("card-type").text = bp.Type.ToString();
            var icon = root.Q<VisualElement>("card-icon");
            if (bp.Icon != null)
                icon.style.backgroundImage = new StyleBackground(bp.Icon);

            // Content
            var descLabel = root.Q<Label>("card-description");
            descLabel.text = !string.IsNullOrEmpty(bp.Description) ? bp.Description : "";
            descLabel.style.display = string.IsNullOrEmpty(bp.Description) ? DisplayStyle.None : DisplayStyle.Flex;

            // Stats - only show for producers
            var inputsRow = root.Q<VisualElement>("inputs-row");
            var outputRow = root.Q<VisualElement>("output-row");
            var timeRow = root.Q<VisualElement>("time-row");
            var powerRow = root.Q<VisualElement>("power-row");

            if (bp.IsProducer)
            {
                var inputsText = bp.Inputs.Count > 0
                    ? string.Join(", ", bp.Inputs.Select(i => i.ToString()))
                    : "None";
                root.Q<Label>("card-inputs").text = inputsText;

                root.Q<Label>("card-output").text = bp.Output.IsValid ? bp.Output.ToString() : "None";
                root.Q<Label>("card-time").text = $"{bp.ProductionTime:0.#}s";
                root.Q<Label>("card-power").text = $"{bp.PowerConsumption}W";
            }
            else
            {
                inputsRow.style.display = DisplayStyle.None;
                outputRow.style.display = DisplayStyle.None;
                timeRow.style.display = DisplayStyle.None;
                powerRow.style.display = DisplayStyle.None;
            }

            // Position
            root.style.left = node.position.x;
            root.style.top = node.position.y;

            // Store Reference
            _nodeElements[node.id] = root;
            _inputPorts[node.id] = new List<VisualElement>();
            _outputPorts[node.id] = new List<VisualElement>();

            // Inputs
            var inputsContainer = root.Q<VisualElement>("inputs-container");
            for (int i = 0; i < bp.InputCount; i++)
            {
                var port = new VisualElement();
                port.AddToClassList("port");
                int index = i;
                port.RegisterCallback<MouseDownEvent>(evt => StartConnection(evt, node.id, index));
                port.RegisterCallback<MouseUpEvent>(evt => CompleteConnection(evt, node.id, index));
                inputsContainer.Add(port);
                _inputPorts[node.id].Add(port);
            }

            // Outputs
            var outputsContainer = root.Q<VisualElement>("outputs-container");
            for (int i = 0; i < bp.OutputCount; i++)
            {
                var port = new VisualElement();
                port.AddToClassList("port");
                int index = i;
                port.RegisterCallback<MouseDownEvent>(evt => StartConnection(evt, node.id, index));
                outputsContainer.Add(port);
                _outputPorts[node.id].Add(port);
            }

            // Dragging - only register MouseDown, MouseUp is handled by canvas
            root.RegisterCallback<MouseDownEvent>(evt => StartDragNode(evt, node, root));

            _canvasContent.Add(root);
        }

        // --- Zoom & Pan ---

        private void OnCanvasWheel(WheelEvent evt)
        {
            var mousePos = evt.localMousePosition;

            float zoomDelta = -evt.delta.y * ZoomSpeed;
            float newZoom = Mathf.Clamp(_zoom + zoomDelta, MinZoom, MaxZoom);

            if (Mathf.Approximately(newZoom, _zoom)) return;

            // Zoom towards mouse position
            float zoomRatio = newZoom / _zoom;
            _pan = mousePos - (mousePos - _pan) * zoomRatio;
            _zoom = newZoom;

            ApplyTransform();
            evt.StopPropagation();
        }

        private void OnCanvasMouseDown(MouseDownEvent evt)
        {
            // Middle mouse or right mouse to pan
            if (evt.button == 2 || evt.button == 1)
            {
                _isPanning = true;
                _panStartMouse = evt.localMousePosition;
                _panStartOffset = _pan;
                _canvas.CaptureMouse();
                evt.StopPropagation();
            }
        }

        private void ApplyTransform()
        {
            _canvasContent.style.left = _pan.x;
            _canvasContent.style.top = _pan.y;
            _canvasContent.style.scale = new Scale(new Vector3(_zoom, _zoom, 1));
            _connectionsLayer.MarkDirtyRepaint();
        }

        private Vector2 ScreenToContent(Vector2 screenPos)
        {
            return (screenPos - _pan) / _zoom;
        }

        private void ResetView()
        {
            _zoom = 1f;
            _pan = Vector2.zero;
            ApplyTransform();
        }

        // --- Dragging Logic ---

        private void StartDragNode(MouseDownEvent evt, BlueprintNode node, VisualElement element)
        {
            if (evt.button != 0) return; // Only left mouse
            if (evt.target is VisualElement el && el.ClassListContains("port")) return;

            _isDraggingNode = true;
            _draggedNodeElement = element;
            _draggedNodeData = node;
            // Store offset from node position to mouse in content space
            var canvasPos = _canvas.WorldToLocal(evt.mousePosition);
            var contentPos = ScreenToContent(canvasPos);
            _dragOffset = contentPos - node.position;
            _canvas.CaptureMouse();
            evt.StopPropagation();
        }

        private void OnCanvasMouseMove(MouseMoveEvent evt)
        {
            // Always update current mouse position in content space
            _currentMousePos = ScreenToContent(evt.localMousePosition);

            // Panning
            if (_isPanning)
            {
                _pan = _panStartOffset + (evt.localMousePosition - _panStartMouse);
                ApplyTransform();
                return;
            }

            if (_isDraggingNode && _draggedNodeElement != null)
            {
                var newPos = _currentMousePos - _dragOffset;
                _draggedNodeElement.style.left = newPos.x;
                _draggedNodeElement.style.top = newPos.y;
                _draggedNodeData.position = newPos;
                _connectionsLayer.MarkDirtyRepaint();
            }

            if (_isConnecting)
            {
                _connectionsLayer.MarkDirtyRepaint();
            }
        }

        private void OnCanvasMouseUp(MouseUpEvent evt)
        {
            if (_isPanning)
            {
                _isPanning = false;
                _canvas.ReleaseMouse();
            }

            if (_isDraggingNode)
            {
                _isDraggingNode = false;
                _draggedNodeElement = null;
                _canvas.ReleaseMouse();
            }

            if (_isConnecting)
            {
                _isConnecting = false;
                _connectionsLayer.MarkDirtyRepaint();
            }
        }

        // --- Connection Logic ---

        private void StartConnection(MouseDownEvent evt, string nodeId, int portIndex)
        {
            _isConnecting = true;
            _connectionStartNodeId = nodeId;
            _connectionStartPortIndex = portIndex;

            // Get port position in content space
            var port = (VisualElement)evt.target;
            var canvasPos = _canvas.WorldToLocal(port.worldBound.center);
            _connectionStartPos = ScreenToContent(canvasPos);

            evt.StopPropagation();
        }

        private void CompleteConnection(MouseUpEvent evt, string targetNodeId, int targetPortIndex)
        {
            if (!_isConnecting) return;

            // Logic: Can only connect Output to Input (simplified)
            // Currently _connectionStartPortIndex assumes generic port, but usually we drag FROM Output TO Input
            // Let's assume user drags FROM Output.
            
            // Check if we are connecting to a different node
            if (_connectionStartNodeId == targetNodeId) return;

            // Create Connection
            var conn = new BlueprintConnection(_connectionStartNodeId, _connectionStartPortIndex, targetNodeId, targetPortIndex);
            _currentGraph.connections.Add(conn);
            
            _isConnecting = false;
            _connectionsLayer.MarkDirtyRepaint();
            evt.StopPropagation();
        }

        // --- Rendering ---

        private void OnGenerateConnections(MeshGenerationContext mgc)
        {
            var paint = mgc.painter2D;
            paint.lineWidth = lineWidth;
            paint.strokeColor = connectionColor;

            // Offset to account for connections layer position
            var offset = new Vector2(5000, 5000);

            // Draw existing connections
            if (_currentGraph != null)
            {
                foreach (var conn in _currentGraph.connections)
                {
                    var start = GetPortPosition(conn.fromNodeId, conn.fromPortIndex, false);
                    var end = GetPortPosition(conn.toNodeId, conn.toPortIndex, true);

                    if (start.HasValue && end.HasValue)
                    {
                        DrawConnection(paint, start.Value + offset, end.Value + offset);
                    }
                }
            }

            // Draw draft connection
            if (_isConnecting)
            {
                paint.strokeColor = new Color(1f, 1f, 1f, 0.5f);
                DrawConnection(paint, _connectionStartPos + offset, _currentMousePos + offset);
            }
        }

        private Vector2? GetPortPosition(string nodeId, int portIndex, bool isInput)
        {
            if (!_nodeElements.ContainsKey(nodeId)) return null;

            var list = isInput ? _inputPorts : _outputPorts;
            if (!list.ContainsKey(nodeId) || portIndex >= list[nodeId].Count) return null;

            var port = list[nodeId][portIndex];
            var worldCenter = port.worldBound.center;
            if (float.IsNaN(worldCenter.x)) return null; // Not laid out yet

            // Convert to content space
            var canvasPos = _canvas.WorldToLocal(worldCenter);
            return ScreenToContent(canvasPos);
        }

        private void DrawConnection(Painter2D paint, Vector2 start, Vector2 end)
        {
            paint.BeginPath();
            paint.MoveTo(start);
            
            // Simple Bezier
            float tangentDist = Mathf.Abs(start.x - end.x) * 0.5f;
            if (tangentDist < 50) tangentDist = 50;
            
            var cp1 = start + new Vector2(tangentDist, 0);
            var cp2 = end - new Vector2(tangentDist, 0);

            paint.BezierCurveTo(cp1, cp2, end);
            paint.Stroke();
        }
    }
}

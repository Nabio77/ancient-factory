using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Systems;

namespace CarbonWorld.Features.TechTree
{
    public class TechTreeUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required]
        private UIDocument uiDocument;

        [SerializeField]
        private CarbonWorld.Features.WorldMap.TileSelector tileSelector; // Added reference

        [Title("Connection Settings")]
        [SerializeField]
        private Color connectionColor = new Color(0.4f, 0.5f, 0.6f);
        [SerializeField]
        private Color unlockedConnectionColor = new Color(0.4f, 0.7f, 0.5f);

        [SerializeField]
        private float lineWidth = 3f;

        private VisualElement _root;
        private VisualElement _canvas;
        private VisualElement _connectionsLayer;
        private VisualElement _nodesContainer;
        private VisualElement _infoPanel;
        private Label _pointsValue;
        private Label _infoName;
        private Label _infoType;
        private Label _infoDescription;
        private Label _infoCost;
        private Label _infoPrereqs;
        private VisualElement _infoIcon;
        private Button _unlockButton;
        private Button _closeButton;

        private Dictionary<string, VisualElement> _nodeElements = new(); // Keyed by GUID
        private TechTreeNodeData _selectedNode;

        private VisualElement _content; // Added variable for content container

        private bool _isDragging;
        private Vector2 _panPosition;
        private float _zoom = 1f;
        private const float MinZoom = 0.5f;
        private const float MaxZoom = 2.0f;
        private const float ZoomStep = 0.1f;

        void Awake()
        {
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement.Q<VisualElement>("root");
            _canvas = _root.Q<VisualElement>("tree-canvas");
            _content = _root.Q<VisualElement>("canvas-content"); // Query content container
            _connectionsLayer = _root.Q<VisualElement>("connections-layer");
            _nodesContainer = _root.Q<VisualElement>("nodes-container");
            _infoPanel = _root.Q<VisualElement>("info-panel");
            _pointsValue = _root.Q<Label>("points-value");

            _infoName = _root.Q<Label>("info-name");
            _infoType = _root.Q<Label>("info-type");
            _infoDescription = _root.Q<Label>("info-description");
            _infoCost = _root.Q<Label>("info-cost");
            _infoPrereqs = _root.Q<Label>("info-prereqs");
            _infoIcon = _root.Q<VisualElement>("info-icon");
            _unlockButton = _root.Q<Button>("unlock-button");
            _closeButton = _root.Q<Button>("close-button");

            _closeButton.clicked += Hide;
            _unlockButton.clicked += OnUnlockClicked;

            _connectionsLayer.generateVisualContent += OnGenerateConnections;

            // Navigation Events
            _root.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _root.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _root.RegisterCallback<WheelEvent>(OnWheel);

            // Start hidden
            _root.AddToClassList("hidden");
            _infoPanel.AddToClassList("hidden");
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 2) // Middle click
            {
                _isDragging = true;
                _root.CapturePointer(evt.pointerId);
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_isDragging)
            {
                _panPosition += (Vector2)evt.deltaPosition;
                UpdateTransform();
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_isDragging && evt.button == 2)
            {
                _isDragging = false;
                _root.ReleasePointer(evt.pointerId);
            }
        }

        private void OnWheel(WheelEvent evt)
        {
            float delta = -evt.delta.y * 0.01f; // Adjust sensitivity
            float newZoom = Mathf.Clamp(_zoom + delta, MinZoom, MaxZoom);

            // Optional: Zoom towards mouse position could be added here, 
            // but simple center zoom or origin zoom is easier for now.

            _zoom = newZoom;
            UpdateTransform();
            evt.StopPropagation();
        }

        private void UpdateTransform()
        {
            if (_content != null)
            {
                _content.style.translate = new StyleTranslate(new Translate(_panPosition.x, _panPosition.y));
                _content.style.scale = new StyleScale(new Scale(new Vector2(_zoom, _zoom)));
            }
        }

        void Start()
        {
            if (tileSelector == null)
            {
                tileSelector = FindFirstObjectByType<CarbonWorld.Features.WorldMap.TileSelector>();
            }

            if (TechTreeSystem.Instance != null && TechTreeSystem.Instance.Graph != null)
            {
                BuildTree();
            }
        }

        void OnEnable()
        {
            if (TechTreeSystem.Instance != null)
            {
                TechTreeSystem.Instance.OnBlueprintUnlocked += OnBlueprintUnlocked;
            }
            if (tileSelector != null)
            {
                tileSelector.OnTileSelected += OnTileSelected;
                tileSelector.OnTileDeselected += OnTileDeselected;
            }
        }

        void OnDisable()
        {
            if (TechTreeSystem.Instance != null)
            {
                TechTreeSystem.Instance.OnBlueprintUnlocked -= OnBlueprintUnlocked;
            }
            if (tileSelector != null)
            {
                tileSelector.OnTileSelected -= OnTileSelected;
                tileSelector.OnTileDeselected -= OnTileDeselected;
            }
        }

        private void OnTileSelected(CarbonWorld.Features.Tiles.BaseTile tile)
        {
            if (tile is CarbonWorld.Features.Tiles.CoreTile)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void OnTileDeselected()
        {
            Hide();
        }

        public void Show()
        {
            if (TechTreeSystem.Instance == null || TechTreeSystem.Instance.Graph == null)
            {
                Debug.LogWarning("[TechTreeUI] TechTreeSystem or Graph is missing.");
                return;
            }

            if (InterfaceSystem.Instance != null)
                InterfaceSystem.Instance.SetState(InterfaceState.TechTree);

            // Rebuild if needed (e.g. graph changed or first load)
            if (_nodeElements.Count == 0)
            {
                BuildTree();
            }

            _root.RemoveFromClassList("hidden");
            RefreshUI();
        }

        public void Hide()
        {
            if (InterfaceSystem.Instance != null)
                InterfaceSystem.Instance.SetState(InterfaceState.Gameplay);

            _root.AddToClassList("hidden");
            DeselectNode();

            // Allow deselecting the core tile when closing manually?
            // Optional: tileSelector.Deselect();
        }

        public void Toggle()
        {
            if (_root.ClassListContains("hidden"))
                Show();
            else
                Hide();
        }

        private void BuildTree()
        {
            _nodesContainer.Clear();
            _nodeElements.Clear();

            var graph = TechTreeSystem.Instance.Graph;
            if (graph == null) return;

            foreach (var nodeData in graph.Nodes)
            {
                CreateNodeElement(nodeData);
            }

            _connectionsLayer.MarkDirtyRepaint();
        }

        private void CreateNodeElement(TechTreeNodeData nodeData)
        {
            var nodeEl = new VisualElement();
            nodeEl.AddToClassList("tech-node");

            var icon = new VisualElement();
            icon.AddToClassList("node-icon");

            Sprite sprite = null;
            string name = "Unknown";

            if (nodeData.blueprint != null)
            {
                sprite = nodeData.blueprint.Icon;
                name = nodeData.blueprint.BlueprintName;
            }
            else if (nodeData.item != null)
            {
                sprite = nodeData.item.Icon;
                name = nodeData.item.ItemName;
            }

            if (sprite != null)
            {
                icon.style.backgroundImage = new StyleBackground(sprite);
            }
            nodeEl.Add(icon);

            var label = new Label(name);
            label.AddToClassList("node-label");
            nodeEl.Add(label);

            nodeEl.style.left = nodeData.position.x;
            nodeEl.style.top = nodeData.position.y;

            nodeEl.RegisterCallback<ClickEvent>(evt => OnNodeClicked(nodeData));

            _nodesContainer.Add(nodeEl);
            _nodeElements[nodeData.guid] = nodeEl;

            UpdateNodeState(nodeData);
        }

        private void RefreshUI()
        {
            if (TechTreeSystem.Instance == null) return;

            _pointsValue.text = TechTreeSystem.Instance.GetAvailablePoints().ToString("N0");

            var graph = TechTreeSystem.Instance.Graph;
            if (graph != null)
            {
                foreach (var nodeData in graph.Nodes)
                {
                    UpdateNodeState(nodeData);
                }
            }

            _connectionsLayer.MarkDirtyRepaint();

            if (_selectedNode != null)
            {
                UpdateInfoPanel(_selectedNode);
            }
        }

        private void UpdateNodeState(TechTreeNodeData nodeData)
        {
            if (!_nodeElements.TryGetValue(nodeData.guid, out var nodeEl)) return;

            nodeEl.RemoveFromClassList("locked");
            nodeEl.RemoveFromClassList("available");
            nodeEl.RemoveFromClassList("unlocked");

            if (TechTreeSystem.Instance == null)
            {
                nodeEl.AddToClassList("locked");
                return;
            }

            if (TechTreeSystem.Instance.IsGuidUnlocked(nodeData.guid))
            {
                nodeEl.AddToClassList("unlocked");
            }
            else if (TechTreeSystem.Instance.CanUnlock(nodeData))
            {
                nodeEl.AddToClassList("available");
            }
            else
            {
                nodeEl.AddToClassList("locked");
            }
        }

        private void OnNodeClicked(TechTreeNodeData nodeData)
        {
            SelectNode(nodeData);
        }

        private void SelectNode(TechTreeNodeData nodeData)
        {
            // Deselect previous
            if (_selectedNode != null && _nodeElements.TryGetValue(_selectedNode.guid, out var prevEl))
            {
                prevEl.RemoveFromClassList("selected");
            }

            _selectedNode = nodeData;

            if (_selectedNode != null && _nodeElements.TryGetValue(_selectedNode.guid, out var newEl))
            {
                newEl.AddToClassList("selected");
            }

            UpdateInfoPanel(nodeData);
            _infoPanel.RemoveFromClassList("hidden");
        }

        private void DeselectNode()
        {
            if (_selectedNode != null && _nodeElements.TryGetValue(_selectedNode.guid, out var prevEl))
            {
                prevEl.RemoveFromClassList("selected");
            }
            _selectedNode = null;
            _infoPanel.AddToClassList("hidden");
        }

        private void UpdateInfoPanel(TechTreeNodeData nodeData)
        {
            if (nodeData == null) return;

            if (nodeData.blueprint != null)
            {
                var bp = nodeData.blueprint;
                _infoName.text = bp.BlueprintName;
                _infoType.text = bp.Type.ToString();
                _infoDescription.text = !string.IsNullOrEmpty(bp.Description) ? bp.Description : "No description.";
                _infoCost.text = bp.UnlockCost.ToString("N0");

                if (bp.Icon != null)
                    _infoIcon.style.backgroundImage = new StyleBackground(bp.Icon);
                else
                    _infoIcon.style.backgroundImage = null;
            }
            else if (nodeData.item != null)
            {
                var item = nodeData.item;
                _infoName.text = item.ItemName;
                _infoType.text = "Resource / Item";
                _infoDescription.text = !string.IsNullOrEmpty(item.Description) ? item.Description : "No description.";
                _infoCost.text = "Unlocked"; // Items are always unlocked

                if (item.Icon != null)
                    _infoIcon.style.backgroundImage = new StyleBackground(item.Icon);
                else
                    _infoIcon.style.backgroundImage = null;
            }
            else
            {
                return;
            }

            // Prerequisites
            if (nodeData.prerequisites.Count == 0)
            {
                _infoPrereqs.text = "None";
            }
            else
            {
                var prereqNames = new List<string>();
                var graph = TechTreeSystem.Instance.Graph;
                foreach (var guid in nodeData.prerequisites)
                {
                    var prereqNode = graph.Nodes.FirstOrDefault(n => n.guid == guid);
                    if (prereqNode != null)
                    {
                        if (prereqNode.blueprint != null)
                            prereqNames.Add(prereqNode.blueprint.BlueprintName);
                        else if (prereqNode.item != null)
                            prereqNames.Add(prereqNode.item.ItemName);
                    }
                }
                _infoPrereqs.text = string.Join(", ", prereqNames);
            }

            // Update button state
            if (TechTreeSystem.Instance != null)
            {
                if (TechTreeSystem.Instance.IsGuidUnlocked(nodeData.guid))
                {
                    _unlockButton.text = "Unlocked";
                    _unlockButton.SetEnabled(false);
                }
                else if (TechTreeSystem.Instance.CanUnlock(nodeData))
                {
                    _unlockButton.text = "Unlock";
                    _unlockButton.SetEnabled(true);
                }
                else
                {
                    _unlockButton.text = "Locked";
                    _unlockButton.SetEnabled(false);
                }
            }
        }

        private void OnUnlockClicked()
        {
            if (_selectedNode == null || TechTreeSystem.Instance == null) return;

            TechTreeSystem.Instance.TryUnlock(_selectedNode);
        }

        private void OnBlueprintUnlocked(BlueprintDefinition blueprint)
        {
            RefreshUI();
        }

        private void OnGenerateConnections(MeshGenerationContext mgc)
        {
            if (TechTreeSystem.Instance == null || TechTreeSystem.Instance.Graph == null) return;

            var paint = mgc.painter2D;
            paint.lineWidth = lineWidth;

            var offset = new Vector2(5000, 5000); // VisualElement coordinate quirk as before
            var graph = TechTreeSystem.Instance.Graph;

            foreach (var nodeData in graph.Nodes)
            {
                var nodePos = nodeData.position + new Vector2(40, 40);

                foreach (var prereqGuid in nodeData.prerequisites)
                {
                    var prereqNode = graph.Nodes.FirstOrDefault(n => n.guid == prereqGuid);
                    if (prereqNode == null) continue;

                    var prereqPos = prereqNode.position + new Vector2(40, 40);

                    bool bothUnlocked = TechTreeSystem.Instance.IsGuidUnlocked(prereqGuid) &&
                                         TechTreeSystem.Instance.IsGuidUnlocked(nodeData.guid);

                    paint.strokeColor = bothUnlocked ? unlockedConnectionColor : connectionColor;

                    DrawConnection(paint, prereqPos + offset, nodePos + offset);
                }
            }
        }

        private void DrawConnection(Painter2D paint, Vector2 start, Vector2 end)
        {
            paint.BeginPath();
            paint.MoveTo(start);

            float midX = (start.x + end.x) * 0.5f;
            var p1 = new Vector2(midX, start.y);
            var p2 = new Vector2(midX, end.y);

            paint.LineTo(p1);
            paint.LineTo(p2);
            paint.LineTo(end);
            paint.Stroke();
        }
    }
}

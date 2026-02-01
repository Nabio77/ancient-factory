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

        void Awake()
        {
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement.Q<VisualElement>("root");
            _canvas = _root.Q<VisualElement>("tree-canvas");
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

            // Start hidden
            _root.AddToClassList("hidden");
            _infoPanel.AddToClassList("hidden");
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

            // Block map input so UI clicks don't deselect the tile
            if (tileSelector != null) tileSelector.IsInputBlocked = true;

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
            // Restore map input
            if (tileSelector != null) tileSelector.IsInputBlocked = false;

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
            if (nodeData.blueprint != null && nodeData.blueprint.Icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(nodeData.blueprint.Icon);
            }
            nodeEl.Add(icon);

            var label = new Label(nodeData.blueprint != null ? nodeData.blueprint.BlueprintName : "Unknown");
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
            if (nodeData == null || nodeData.blueprint == null) return;

            var bp = nodeData.blueprint;
            _infoName.text = bp.BlueprintName;
            _infoType.text = bp.Type.ToString();
            _infoDescription.text = !string.IsNullOrEmpty(bp.Description) ? bp.Description : "No description.";
            _infoCost.text = bp.UnlockCost.ToString("N0");

            if (bp.Icon != null)
            {
                _infoIcon.style.backgroundImage = new StyleBackground(bp.Icon);
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
                    if (prereqNode != null && prereqNode.blueprint != null)
                        prereqNames.Add(prereqNode.blueprint.BlueprintName);
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

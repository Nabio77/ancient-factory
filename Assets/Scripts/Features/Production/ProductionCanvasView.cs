using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using CarbonWorld.Core.Data;

namespace CarbonWorld.Features.Production
{
    public class ProductionCanvasView
    {
        private readonly VisualElement _canvas;
        private readonly VisualElement _canvasContent;
        private readonly VisualElement _connectionsLayer;
        private readonly VisualTreeAsset _cardTemplate;
        
        private BlueprintGraph _currentGraph;
        
        // Settings
        private Color _connectionColor = new Color(0f, 0.67f, 1f);
        private float _lineWidth = 3f;

        // View State
        private float _zoom = 1f;
        private Vector2 _pan = Vector2.zero;

        // Element Maps
        private Dictionary<string, VisualElement> _nodeElements = new();
        private Dictionary<string, List<VisualElement>> _inputPorts = new();
        private Dictionary<string, List<VisualElement>> _outputPorts = new();

        public BlueprintGraph CurrentGraph => _currentGraph;
        public VisualElement Canvas => _canvas;
        public float Zoom => _zoom;
        public Vector2 Pan => _pan;

        public ProductionGraphInput Input { get; set; }
        public ProductionIOView IOView { get; set; }

        public ProductionCanvasView(VisualElement canvas, VisualTreeAsset cardTemplate)
        {
            _canvas = canvas;
            _cardTemplate = cardTemplate;
            
            _canvasContent = _canvas.Q<VisualElement>("canvas-content");
            _connectionsLayer = _canvasContent.Q<VisualElement>("connections-layer");
            
            _connectionsLayer.generateVisualContent += OnGenerateConnections;
        }

        public void SetGraph(BlueprintGraph graph)
        {
            _currentGraph = graph;
            RebuildGraphUI();
        }

        public void Clear()
        {
            _currentGraph = null;
            _nodeElements.Clear();
            _inputPorts.Clear();
            _outputPorts.Clear();
            
            // Clear content but keep connections layer
            for (int i = _canvasContent.childCount - 1; i >= 0; i--)
            {
                if (_canvasContent[i] != _connectionsLayer)
                {
                    _canvasContent.RemoveAt(i);
                }
            }
            
            ResetView();
        }

        public void RebuildGraphUI()
        {
            // Clear existing UI nodes
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

            if (_currentGraph == null) return;

            foreach (var node in _currentGraph.nodes)
            {
                CreateNodeUI(node);
            }

            MarkConnectionsDirty();
        }

        public void CreateNodeUI(BlueprintNode node)
        {
            var card = _cardTemplate.Instantiate();
            var bp = node.blueprint;

            // Add type class for styling
            card.AddToClassList($"type-{bp.Type}");

            // Header
            card.Q<Label>("card-title").text = bp.BlueprintName;
            card.Q<Label>("card-type").text = bp.Type.ToString();
            var icon = card.Q<VisualElement>("card-icon");
            if (bp.Icon != null)
                icon.style.backgroundImage = new StyleBackground(bp.Icon);

            // Content
            var descLabel = card.Q<Label>("card-description");
            descLabel.text = !string.IsNullOrEmpty(bp.Description) ? bp.Description : "";
            descLabel.style.display = string.IsNullOrEmpty(bp.Description) ? DisplayStyle.None : DisplayStyle.Flex;

            // Stats
            var inputsRow = card.Q<VisualElement>("inputs-row");
            var outputRow = card.Q<VisualElement>("output-row");
            var timeRow = card.Q<VisualElement>("time-row");
            var powerRow = card.Q<VisualElement>("power-row");

            if (bp.IsProducer)
            {
                var inputsText = bp.Inputs.Count > 0
                    ? string.Join(", ", bp.Inputs.Select(i => i.ToString()))
                    : "None";
                card.Q<Label>("card-inputs").text = inputsText;

                card.Q<Label>("card-output").text = bp.Output.IsValid ? bp.Output.ToString() : "None";
                card.Q<Label>("card-time").text = $"{bp.ProductionTime:0.#}s";
                card.Q<Label>("card-power").text = $"{bp.PowerConsumption}W";
            }
            else
            {
                inputsRow.style.display = DisplayStyle.None;
                outputRow.style.display = DisplayStyle.None;
                timeRow.style.display = DisplayStyle.None;
                powerRow.style.display = DisplayStyle.None;
            }

            card.style.position = Position.Absolute;
            card.style.left = node.position.x;
            card.style.top = node.position.y;

            _nodeElements[node.id] = card;
            _inputPorts[node.id] = new List<VisualElement>();
            _outputPorts[node.id] = new List<VisualElement>();

            // Inputs
            var inputsContainer = card.Q<VisualElement>("inputs-container");
            inputsContainer.Clear();
            for (int i = 0; i < bp.InputCount; i++)
            {
                var port = new VisualElement();
                port.AddToClassList("port");
                int index = i;
                if (Input != null)
                {
                    port.RegisterCallback<MouseDownEvent>(evt => Input.StartConnection(evt, node.id, index));
                    port.RegisterCallback<MouseUpEvent>(evt => Input.CompleteConnection(evt, node.id, index));
                }
                inputsContainer.Add(port);
                _inputPorts[node.id].Add(port);
            }

            // Outputs
            var outputsContainer = card.Q<VisualElement>("outputs-container");
            outputsContainer.Clear();
            for (int i = 0; i < bp.OutputCount; i++)
            {
                var port = new VisualElement();
                port.AddToClassList("port");
                int index = i;
                if (Input != null)
                {
                    port.RegisterCallback<MouseDownEvent>(evt => Input.StartConnection(evt, node.id, index));
                }
                outputsContainer.Add(port);
                _outputPorts[node.id].Add(port);
            }

            if (Input != null)
            {
                card.RegisterCallback<MouseDownEvent>(evt => Input.StartDragNode(evt, node, card));
            }

            _canvasContent.Add(card);
        }

        public void MarkConnectionsDirty()
        {
            _connectionsLayer.MarkDirtyRepaint();
        }

        public void SetTransform(Vector2 pan, float zoom)
        {
            _pan = pan;
            _zoom = zoom;
            
            _canvasContent.style.left = _pan.x;
            _canvasContent.style.top = _pan.y;
            _canvasContent.style.scale = new Scale(new Vector3(_zoom, _zoom, 1));
            
            MarkConnectionsDirty();
        }

        public void ResetView()
        {
            SetTransform(Vector2.zero, 1f);
        }

        public Vector2 ScreenToContent(Vector2 screenPos)
        {
            return (screenPos - _pan) / _zoom;
        }

        private void OnGenerateConnections(MeshGenerationContext mgc)
        {
            var paint = mgc.painter2D;
            paint.lineWidth = _lineWidth;
            paint.strokeColor = _connectionColor;

            var offset = new Vector2(5000, 5000); // VisualElement coordinate quirk adjustment if needed, inherited from original code

            if (_currentGraph != null)
            {
                foreach (var conn in _currentGraph.connections)
                {
                    Vector2? start = null, end = null;

                    if (_currentGraph.IsIONode(conn.fromNodeId))
                    {
                        start = GetIOPortPosition(conn.fromNodeId);
                        end = GetPortPosition(conn.toNodeId, conn.toPortIndex, true);
                    }
                    else if (_currentGraph.IsIONode(conn.toNodeId))
                    {
                        start = GetPortPosition(conn.fromNodeId, conn.fromPortIndex, false);
                        end = GetIOPortPosition(conn.toNodeId);
                    }
                    else
                    {
                        start = GetPortPosition(conn.fromNodeId, conn.fromPortIndex, false);
                        end = GetPortPosition(conn.toNodeId, conn.toPortIndex, true);
                    }

                    if (start.HasValue && end.HasValue)
                    {
                        DrawConnection(paint, start.Value + offset, end.Value + offset);
                    }
                }
            }

            if (Input != null && Input.IsConnecting)
            {
                paint.strokeColor = new Color(1f, 1f, 1f, 0.5f);
                DrawConnection(paint, Input.ConnectionStartPos + offset, Input.CurrentMousePos + offset);
            }
        }

        private Vector2? GetPortPosition(string nodeId, int portIndex, bool isInput)
        {
            if (!_nodeElements.ContainsKey(nodeId)) return null;

            var list = isInput ? _inputPorts : _outputPorts;
            if (!list.ContainsKey(nodeId) || portIndex >= list[nodeId].Count) return null;

            var port = list[nodeId][portIndex];
            var worldCenter = port.worldBound.center;
            if (float.IsNaN(worldCenter.x)) return null;

            var canvasPos = _canvas.WorldToLocal(worldCenter);
            return ScreenToContent(canvasPos);
        }

        private Vector2? GetIOPortPosition(string ioNodeId)
        {
            if (IOView == null) return null;
            var port = IOView.GetPort(ioNodeId);
            if (port == null) return null;

            var worldCenter = port.worldBound.center;
            if (float.IsNaN(worldCenter.x)) return null;

            var canvasPos = _canvas.WorldToLocal(worldCenter);
            return ScreenToContent(canvasPos);
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

        public bool TryDeleteConnectionAt(Vector2 point)
        {
            if (_currentGraph == null) return false;
            
            const float hitDistance = 10f;
            
            BlueprintConnection connToDelete = null;

            foreach (var conn in _currentGraph.connections)
            {
                // Resolve positions similar to OnGenerateConnections
                Vector2? start = null, end = null;
                
                 if (_currentGraph.IsIONode(conn.fromNodeId))
                {
                    start = GetIOPortPosition(conn.fromNodeId);
                    end = GetPortPosition(conn.toNodeId, conn.toPortIndex, true);
                }
                else if (_currentGraph.IsIONode(conn.toNodeId))
                {
                    start = GetPortPosition(conn.fromNodeId, conn.fromPortIndex, false);
                    end = GetIOPortPosition(conn.toNodeId);
                }
                else
                {
                    start = GetPortPosition(conn.fromNodeId, conn.fromPortIndex, false);
                    end = GetPortPosition(conn.toNodeId, conn.toPortIndex, true);
                }

                if (!start.HasValue || !end.HasValue) continue;

                if (IsPointNearPolyline(point, start.Value, end.Value, hitDistance))
                {
                    connToDelete = conn;
                    break;
                }
            }

            if (connToDelete != null)
            {
                _currentGraph.connections.Remove(connToDelete);
                MarkConnectionsDirty();
                return true;
            }
            return false;
        }

        private bool IsPointNearPolyline(Vector2 point, Vector2 start, Vector2 end, float maxDistance)
        {
            float midX = (start.x + end.x) * 0.5f;
            var p1 = new Vector2(midX, start.y);
            var p2 = new Vector2(midX, end.y);

            // Check distance to 3 segments
            if (DistanceToSegment(point, start, p1) < maxDistance) return true;
            if (DistanceToSegment(point, p1, p2) < maxDistance) return true;
            if (DistanceToSegment(point, p2, end) < maxDistance) return true;

            return false;
        }

        private float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }
    }
}
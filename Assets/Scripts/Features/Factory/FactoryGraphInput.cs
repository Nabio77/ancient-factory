using System;
using UnityEngine;
using UnityEngine.UIElements;
using CarbonWorld.Core.Data;

namespace CarbonWorld.Features.Factory
{
    public class FactoryGraphInput
    {
        private readonly FactoryCanvasView _canvasView;
        private readonly VisualElement _canvas;

        // Zoom & Pan Constants
        private const float MinZoom = 0.25f;
        private const float MaxZoom = 2f;
        private const float ZoomSpeed = 0.1f;

        // Interaction State
        private bool _isPanning;
        private Vector2 _panStartMouse;
        private Vector2 _panStartOffset;

        private bool _isDraggingNode;
        private VisualElement _draggedNodeElement;
        private BlueprintNode _draggedNodeData;
        private Vector2 _dragOffset;

        // Connection State
        private bool _isConnecting;
        private string _connectionStartNodeId;
        private int _connectionStartPortIndex;
        private Vector2 _connectionStartPos;
        private Vector2 _currentMousePos;

        public bool IsConnecting => _isConnecting;
        public Vector2 ConnectionStartPos => _connectionStartPos;
        public Vector2 CurrentMousePos => _currentMousePos;

        public event Action OnGraphChanged;

        public FactoryGraphInput(FactoryCanvasView canvasView, VisualElement canvas)
        {
            _canvasView = canvasView;
            _canvas = canvas;

            _canvas.RegisterCallback<MouseDownEvent>(OnCanvasMouseDown);
            _canvas.RegisterCallback<MouseMoveEvent>(OnCanvasMouseMove);
            _canvas.RegisterCallback<MouseUpEvent>(OnCanvasMouseUp);
            _canvas.RegisterCallback<WheelEvent>(OnCanvasWheel);
        }

        public void Cleanup()
        {
            _canvas.UnregisterCallback<MouseDownEvent>(OnCanvasMouseDown);
            _canvas.UnregisterCallback<MouseMoveEvent>(OnCanvasMouseMove);
            _canvas.UnregisterCallback<MouseUpEvent>(OnCanvasMouseUp);
            _canvas.UnregisterCallback<WheelEvent>(OnCanvasWheel);
        }

        private void OnCanvasWheel(WheelEvent evt)
        {
            var mousePos = evt.localMousePosition;
            var currentZoom = _canvasView.Zoom;
            var currentPan = _canvasView.Pan;

            float zoomDelta = -evt.delta.y * ZoomSpeed;
            float newZoom = Mathf.Clamp(currentZoom + zoomDelta, MinZoom, MaxZoom);

            if (Mathf.Approximately(newZoom, currentZoom)) return;

            // Zoom towards mouse position
            float zoomRatio = newZoom / currentZoom;
            var newPan = mousePos - (mousePos - currentPan) * zoomRatio;
            
            _canvasView.SetTransform(newPan, newZoom);
            evt.StopPropagation();
        }

        private void OnCanvasMouseDown(MouseDownEvent evt)
        {
            // Right-click to delete connections
            if (evt.button == 1)
            {
                var contentPos = _canvasView.ScreenToContent(evt.localMousePosition);
                if (_canvasView.TryDeleteConnectionAt(contentPos))
                {
                    _canvasView.CurrentGraph.NotifyGraphUpdated();
                    OnGraphChanged?.Invoke();
                }
                evt.StopPropagation();
                return;
            }

            // Middle mouse to pan
            if (evt.button == 2)
            {
                _isPanning = true;
                _panStartMouse = evt.localMousePosition;
                _panStartOffset = _canvasView.Pan;
                _canvas.CaptureMouse();
                evt.StopPropagation();
            }
        }

        public void StartDragNode(MouseDownEvent evt, BlueprintNode node, VisualElement element)
        {
            if (evt.button != 0) return; // Only left mouse
            if (evt.target is VisualElement el && el.ClassListContains("port")) return;

            _isDraggingNode = true;
            _draggedNodeElement = element;
            _draggedNodeData = node;
            
            // Store offset from node position to mouse in content space
            var canvasPos = _canvas.WorldToLocal(evt.mousePosition);
            var contentPos = _canvasView.ScreenToContent(canvasPos);
            _dragOffset = contentPos - node.position;
            
            _canvas.CaptureMouse();
            evt.StopPropagation();
        }

        private void OnCanvasMouseMove(MouseMoveEvent evt)
        {
            // Always update current mouse position in content space
            _currentMousePos = _canvasView.ScreenToContent(evt.localMousePosition);

            // Panning
            if (_isPanning)
            {
                var newPan = _panStartOffset + (evt.localMousePosition - _panStartMouse);
                _canvasView.SetTransform(newPan, _canvasView.Zoom);
                return;
            }

            if (_isDraggingNode && _draggedNodeElement != null)
            {
                var newPos = _currentMousePos - _dragOffset;
                _draggedNodeElement.style.left = newPos.x;
                _draggedNodeElement.style.top = newPos.y;
                _draggedNodeData.position = newPos;
                _canvasView.MarkConnectionsDirty();
            }

            if (_isConnecting)
            {
                _canvasView.MarkConnectionsDirty();
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
                _canvasView.MarkConnectionsDirty();
            }
        }

        public void StartConnection(MouseDownEvent evt, string nodeId, int portIndex)
        {
            _isConnecting = true;
            _connectionStartNodeId = nodeId;
            _connectionStartPortIndex = portIndex;

            // Get port position in content space
            var port = (VisualElement)evt.target;
            var canvasPos = _canvas.WorldToLocal(port.worldBound.center);
            _connectionStartPos = _canvasView.ScreenToContent(canvasPos);

            evt.StopPropagation();
        }

        public void CompleteConnection(MouseUpEvent evt, string targetNodeId, int targetPortIndex)
        {
            if (!_isConnecting) return;

            if (_connectionStartNodeId == targetNodeId) return;

            // Create Connection
            var conn = new BlueprintConnection(_connectionStartNodeId, _connectionStartPortIndex, targetNodeId, targetPortIndex);
            if (_canvasView.CurrentGraph != null)
            {
                _canvasView.CurrentGraph.connections.Add(conn);
                _canvasView.CurrentGraph.NotifyGraphUpdated();
                _canvasView.MarkConnectionsDirty();
                OnGraphChanged?.Invoke();
            }

            _isConnecting = false;
            evt.StopPropagation();
        }

        public void ResetState()
        {
            if (_isPanning || _isDraggingNode || _isConnecting)
            {
                _canvas.ReleaseMouse();
            }
            _isPanning = false;
            _isDraggingNode = false;
            _draggedNodeElement = null;
            _isConnecting = false;
        }
    }
}
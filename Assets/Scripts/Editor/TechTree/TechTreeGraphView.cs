using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using AncientFactory.Core.Data;
using AncientFactory.Core.Types;

namespace AncientFactory.Editor.TechTree
{
    public class TechTreeGraphView : GraphView
    {
        private TechTreeGraph _graph;
        private readonly Dictionary<string, TechTreeNodeView> _nodeCache = new();

        public TechTreeGraphView()
        {
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/TechTree/TechTreeGraph.uss");
            if (styleSheet != null)
                styleSheets.Add(styleSheet);
        }

        public void PopulateView(TechTreeGraph graph)
        {
            _graph = graph;
            _nodeCache.Clear();

            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            graphViewChanged += OnGraphViewChanged;

            if (_graph.Nodes == null) return;

            // Create Nodes
            foreach (var nodeData in _graph.Nodes)
            {
                CreateNodeView(nodeData);
            }

            // Create Edges
            foreach (var nodeData in _graph.Nodes)
            {
                if (!_nodeCache.TryGetValue(nodeData.guid, out var inputNode)) continue;

                foreach (var prereqGuid in nodeData.prerequisites)
                {
                    if (!_nodeCache.TryGetValue(prereqGuid, out var outputNode)) continue;

                    var outputPort = outputNode.outputContainer.Q<Port>();
                    var inputPort = inputNode.inputContainer.Q<Port>();

                    if (outputPort != null && inputPort != null)
                    {
                        var edge = outputPort.ConnectTo(inputPort);
                        AddElement(edge);
                    }
                }
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                foreach (var elem in graphViewChange.elementsToRemove)
                {
                    if (elem is TechTreeNodeView nodeView)
                    {
                        _nodeCache.Remove(nodeView.NodeData.guid);
                    }
                }
            }
            return graphViewChange;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            foreach (var port in ports)
            {
                if (port.direction != startPort.direction && port.node != startPort.node)
                {
                    compatiblePorts.Add(port);
                }
            }
            return compatiblePorts;
        }

        public void CreateNode(BlueprintDefinition blueprint, Vector2 position)
        {
            var nodeData = new TechTreeNodeData
            {
                blueprint = blueprint,
                position = position
            };

            CreateNodeView(nodeData);
        }

        public void CreateItemNode(ItemDefinition item, Vector2 position)
        {
            var nodeData = new TechTreeNodeData
            {
                item = item,
                position = position
            };

            CreateNodeView(nodeData);
        }

        private void CreateNodeView(TechTreeNodeData nodeData)
        {
            var node = new TechTreeNodeView(nodeData);

            // Create Ports with Vertical Orientation

            // Input Port (Requires Prerequisite) - Top
            // Items are roots, so they don't have inputs
            if (nodeData.item == null)
            {
                var inputPort = GeneratePort(node, Direction.Input, Port.Capacity.Multi);
                inputPort.portName = "Requires";
                // Add to title container for "Top" placement, or generic main container
                // node.titleContainer.Add(inputPort); // Can interfere with title
                // Better: Insert at top of main container or use a custom container
                node.mainContainer.Insert(0, inputPort);
                // Also align it
                inputPort.style.alignSelf = Align.Center;
            }

            // Output Port (Used as Prerequisite) - Bottom
            var outputPort = GeneratePort(node, Direction.Output, Port.Capacity.Multi);
            outputPort.portName = "Unlocks";
            // Add to normal output container but it might be side-by-side. 
            // For vertical, we typically want it at the bottom.
            // mainContainer usually contains title + contents.
            node.mainContainer.Add(outputPort);
            outputPort.style.alignSelf = Align.Center;

            // Hide default containers if we aren't using them for ports to avoid empty space
            node.inputContainer.style.display = DisplayStyle.None;
            node.outputContainer.style.display = DisplayStyle.None;

            node.RefreshExpandedState();
            node.RefreshPorts();

            node.SetPosition(new Rect(nodeData.position, Vector2.zero));

            // Visual setup
            node.style.width = 200;

            if (nodeData.blueprint != null && nodeData.blueprint.Icon != null)
            {
                // Add icon
                var icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(nodeData.blueprint.Icon);
                icon.style.width = 64;
                icon.style.height = 64;
                icon.style.alignSelf = Align.Center;
                icon.style.marginBottom = 10;
                node.extensionContainer.Add(icon);
            }
            else if (nodeData.item != null && nodeData.item.Icon != null)
            {
                // Add item icon
                var icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(nodeData.item.Icon);
                icon.style.width = 64;
                icon.style.height = 64;
                icon.style.alignSelf = Align.Center;
                icon.style.marginBottom = 10;
                node.extensionContainer.Add(icon);
            }



            node.RefreshExpandedState();

            _nodeCache[nodeData.guid] = node;
            AddElement(node);
        }

        private Port GeneratePort(TechTreeNodeView node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
        {
            return node.InstantiatePort(Orientation.Vertical, portDirection, capacity, typeof(bool));
        }

        public void SaveGraph()
        {
            if (_graph == null) return;

            _graph.Clear();

            // Save Nodes
            foreach (var nodeView in _nodeCache.Values)
            {
                nodeView.NodeData.position = nodeView.GetPosition().position;
                nodeView.NodeData.prerequisites.Clear();
                _graph.AddNode(nodeView.NodeData);
            }

            // Save Connections (Edges)
            foreach (var edge in edges)
            {
                if (edge.input.node is TechTreeNodeView inputNode && edge.output.node is TechTreeNodeView outputNode)
                {
                    inputNode.NodeData.prerequisites.Add(outputNode.NodeData.guid);
                }
            }

            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssets();
        }
    }

    public class TechTreeNodeView : Node // Changed to inherit Node directly
    {
        public TechTreeNodeData NodeData;

        public TechTreeNodeView(TechTreeNodeData data)
        {
            NodeData = data;
            title = data.Name;
            viewDataKey = data.guid;

            style.width = 200;

            if (data.blueprint != null && data.blueprint.Icon != null)
            {
                // Add icon if possible, simplified for now
            }
            else if (data.item != null && data.item.Icon != null)
            {
                // Add item icon
            }


            RefreshExpandedState();
        }
    }
}

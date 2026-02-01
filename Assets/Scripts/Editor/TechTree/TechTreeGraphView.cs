using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using CarbonWorld.Core.Data;

namespace CarbonWorld.Editor.TechTree
{
    public class TechTreeGraphView : GraphView
    {


        private TechTreeGraph _graph;

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
                var inputNode = GetNodeView(nodeData.guid) as TechTreeNodeView;
                if (inputNode == null) continue;

                foreach (var prereqGuid in nodeData.prerequisites)
                {
                    var outputNode = GetNodeView(prereqGuid) as TechTreeNodeView;
                    if (outputNode == null) continue;

                    // Connection: Prereq (Output) -> Node (Input)
                    // Prereq is the "parent" because you need it first.
                    // So flow is Prereq -> This Node.

                    var outputPort = outputNode.outputContainer.Q<Port>();
                    var inputPort = inputNode.inputContainer.Q<Port>();

                    var edge = outputPort.ConnectTo(inputPort);
                    AddElement(edge);
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
                        var nodeData = nodeView.NodeData;
                        // Remove from graph data handles in save
                    }
                    else if (elem is Edge edge)
                    {
                        // Remove connection
                    }
                }
            }
            return graphViewChange;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
                endPort.direction != startPort.direction &&
                endPort.node != startPort.node).ToList();
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

        private void CreateNodeView(TechTreeNodeData nodeData)
        {
            var node = new TechTreeNodeView(nodeData);

            // Input Port (Requires Prerequisite)
            var inputPort = GeneratePort(node, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Requires";
            node.inputContainer.Add(inputPort);

            // Output Port (Used as Prerequisite)
            var outputPort = GeneratePort(node, Direction.Output, Port.Capacity.Multi);
            outputPort.portName = "Unlocks";
            node.outputContainer.Add(outputPort);

            node.RefreshExpandedState();
            node.RefreshPorts();
            
            node.SetPosition(new Rect(nodeData.position, Vector2.zero));

            AddElement(node);
        }

        private Port GeneratePort(TechTreeNodeView node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
        {
            return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(bool)); // Type doesn't matter for logic
        }

        private Node GetNodeView(string guid)
        {
            return nodes.ToList().Cast<TechTreeNodeView>().FirstOrDefault(n => n.NodeData.guid == guid);
        }

        public void SaveGraph()
        {
            if (_graph == null) return;

            _graph.Clear();

            var nodes = this.nodes.ToList().Cast<TechTreeNodeView>().ToList();
            var edges = this.edges.ToList();

            // Save Nodes
            foreach (var nodeView in nodes)
            {
                // Update position
                nodeView.NodeData.position = nodeView.GetPosition().position;
                nodeView.NodeData.prerequisites.Clear();

                _graph.AddNode(nodeView.NodeData);
            }

            // Save Connections (Edges)
            foreach (var edge in edges)
            {
                var inputNode = edge.input.node as TechTreeNodeView;
                var outputNode = edge.output.node as TechTreeNodeView;

                if (inputNode != null && outputNode != null)
                {
                    // Flow is Output -> Input
                    // Prereq -> Dependent
                    // So Dependent has Prereq in its list
                    
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
            title = data.blueprint != null ? data.blueprint.BlueprintName : "New Node";
            viewDataKey = data.guid;

            style.width = 200;
            
            if (data.blueprint != null && data.blueprint.Icon != null)
            {
               // Add icon if possible, simplified for now
            }
        }
    }
}

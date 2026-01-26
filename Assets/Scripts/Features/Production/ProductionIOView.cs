using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.Tiles;

namespace CarbonWorld.Features.Production
{
    public class ProductionIOView
    {
        private readonly WorldMap.WorldMap _worldMap;
        private readonly VisualTreeAsset _tileIOCardTemplate;
        private readonly ProductionCanvasView _canvasView;
        
        private VisualElement _inputZone;
        private VisualElement _outputZone;
        
        // Maps to track elements
        private Dictionary<string, VisualElement> _ioCardElements = new();
        private Dictionary<string, VisualElement> _ioCardPorts = new();

        public ProductionIOView(WorldMap.WorldMap worldMap, VisualTreeAsset tileIOCardTemplate, ProductionCanvasView canvasView)
        {
            _worldMap = worldMap;
            _tileIOCardTemplate = tileIOCardTemplate;
            _canvasView = canvasView;
        }

        public void CreateIOZones(VisualElement root)
        {
            // Input zone
            _inputZone = new VisualElement();
            _inputZone.AddToClassList("io-zone");
            _inputZone.AddToClassList("input-zone");

            var inputTitle = new Label("INPUTS");
            inputTitle.AddToClassList("io-zone-title");
            _inputZone.Add(inputTitle);

            root.Add(_inputZone);

            // Output zone
            _outputZone = new VisualElement();
            _outputZone.AddToClassList("io-zone");
            _outputZone.AddToClassList("output-zone");

            var outputTitle = new Label("OUTPUT");
            outputTitle.AddToClassList("io-zone-title");
            _outputZone.Add(outputTitle);

            root.Add(_outputZone);
        }

        public void Cleanup()
        {
            _inputZone?.RemoveFromHierarchy();
            _outputZone?.RemoveFromHierarchy();
            _inputZone = null;
            _outputZone = null;
            _ioCardElements.Clear();
            _ioCardPorts.Clear();
        }

        public void PopulateIOCards(ProductionTile currentTile)
        {
            if (currentTile == null || _worldMap == null || _canvasView.CurrentGraph == null) return;

            // Clear existing IO nodes in graph data
            _canvasView.CurrentGraph.ioNodes.Clear();
            _ioCardElements.Clear();
            _ioCardPorts.Clear();
            
            // Clear UI containers (keeping titles)
            if (_inputZone != null)
                for (int i = _inputZone.childCount - 1; i >= 0; i--)
                    if (!_inputZone[i].ClassListContains("io-zone-title")) _inputZone.RemoveAt(i);
                    
            if (_outputZone != null)
                for (int i = _outputZone.childCount - 1; i >= 0; i--)
                    if (!_outputZone[i].ClassListContains("io-zone-title")) _outputZone.RemoveAt(i);

            // Get adjacent tiles
            var neighbors = _worldMap.TileData.GetNeighbors(currentTile.CellPosition);
            int inputIndex = 0;

            foreach (var neighbor in neighbors)
            {
                if (neighbor is ResourceTile resourceTile)
                {
                    var output = resourceTile.GetOutput();
                    if (output.IsValid)
                    {
                        var ioNode = new TileIONode(
                            TileIOType.Input,
                            neighbor.CellPosition,
                            neighbor.Type,
                            output,
                            inputIndex++
                        );
                        _canvasView.CurrentGraph.ioNodes.Add(ioNode);
                        CreateIOCardUI(ioNode);
                    }
                }
                else if (neighbor is ProductionTile productionTile)
                {
                    var graphOutputs = GetProductionTileOutputs(productionTile);
                    foreach (var outputItem in graphOutputs)
                    {
                        var ioNode = new TileIONode(
                            TileIOType.Input,
                            neighbor.CellPosition,
                            neighbor.Type,
                            outputItem,
                            inputIndex++
                        );
                        _canvasView.CurrentGraph.ioNodes.Add(ioNode);
                        CreateIOCardUI(ioNode);
                    }
                }
            }

            // Create single output card
            var outputNode = new TileIONode(
                TileIOType.Output,
                currentTile.CellPosition,
                currentTile.Type,
                new ItemStack(),
                0
            );
            _canvasView.CurrentGraph.ioNodes.Add(outputNode);
            CreateIOCardUI(outputNode);
        }

        private List<ItemStack> GetProductionTileOutputs(ProductionTile tile)
        {
            var outputs = new List<ItemStack>();
            var graph = tile.Graph;

            if (graph == null || graph.nodes.Count == 0)
                return outputs;

            // Find connections that go to the output IO node
            var outputConnections = graph.connections
                .Where(c => c.toNodeId.StartsWith("tile_io_output"))
                .ToList();

            foreach (var conn in outputConnections)
            {
                var sourceNode = graph.GetNode(conn.fromNodeId);
                if (sourceNode?.blueprint != null && sourceNode.blueprint.Output.IsValid)
                {
                    outputs.Add(sourceNode.blueprint.Output);
                }
            }

            // Fallback: unconnected producers
            if (outputs.Count == 0)
            {
                var connectedOutputs = new HashSet<(string nodeId, int portIndex)>();
                foreach (var conn in graph.connections)
                {
                    connectedOutputs.Add((conn.fromNodeId, conn.fromPortIndex));
                }

                foreach (var node in graph.nodes)
                {
                    if (node.blueprint == null || !node.blueprint.IsProducer)
                        continue;

                    for (int i = 0; i < node.blueprint.OutputCount; i++)
                    {
                        if (!connectedOutputs.Contains((node.id, i)) && node.blueprint.Output.IsValid)
                        {
                            outputs.Add(node.blueprint.Output);
                            break;
                        }
                    }
                }
            }

            return outputs;
        }

        private void CreateIOCardUI(TileIONode ioNode)
        {
            var card = _tileIOCardTemplate.Instantiate();

            bool isInput = ioNode.type == TileIOType.Input;

            card.AddToClassList(isInput ? "input-card" : "output-card");

            var typeLabel = card.Q<Label>("io-card-type");
            typeLabel.text = isInput ? ioNode.sourceTileType.ToString() : "Output";

            var itemLabel = card.Q<Label>("io-card-item");
            var amountLabel = card.Q<Label>("io-card-amount");

            if (ioNode.availableItem.IsValid)
            {
                itemLabel.text = ioNode.availableItem.Item.ItemName;
                amountLabel.text = $"{ioNode.availableItem.Amount}/tick";
            }
            else
            {
                itemLabel.text = isInput ? "Empty" : "Connect Output";
                amountLabel.text = "";
            }

            var icon = card.Q<VisualElement>("io-card-icon");
            if (ioNode.availableItem.IsValid && ioNode.availableItem.Item.Icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(ioNode.availableItem.Item.Icon);
            }

            var port = new VisualElement();
            port.AddToClassList("io-port");

            var portContainer = isInput
                ? card.Q<VisualElement>("io-port-right")
                : card.Q<VisualElement>("io-port-left");
            portContainer.Add(port);

            if (isInput)
            {
                port.RegisterCallback<MouseDownEvent>(evt => _canvasView.Input.StartConnection(evt, ioNode.id, 0));
            }
            else
            {
                port.RegisterCallback<MouseUpEvent>(evt => _canvasView.Input.CompleteConnection(evt, ioNode.id, 0));
            }

            _ioCardElements[ioNode.id] = card;
            _ioCardPorts[ioNode.id] = port;

            if (isInput)
                _inputZone.Add(card);
            else
                _outputZone.Add(card);
        }

        public VisualElement GetPort(string ioNodeId)
        {
            return _ioCardPorts.TryGetValue(ioNodeId, out var port) ? port : null;
        }
    }
}
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

        public void CreateIOZones(VisualElement root, bool hasOutput = true)
        {
            // Input zone
            _inputZone = new VisualElement();
            _inputZone.AddToClassList("io-zone");
            _inputZone.AddToClassList("input-zone");

            var inputTitle = new Label("INPUTS");
            inputTitle.AddToClassList("io-zone-title");
            _inputZone.Add(inputTitle);

            root.Add(_inputZone);

            // Output zone (only if tile has output)
            if (hasOutput)
            {
                _outputZone = new VisualElement();
                _outputZone.AddToClassList("io-zone");
                _outputZone.AddToClassList("output-zone");

                var outputTitle = new Label("OUTPUT");
                outputTitle.AddToClassList("io-zone-title");
                _outputZone.Add(outputTitle);

                root.Add(_outputZone);
            }
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

        public void PopulateIOCards(IGraphTile graphTile)
        {
            if (graphTile == null || _worldMap == null || _canvasView.CurrentGraph == null) return;

            _ioCardElements.Clear();
            _ioCardPorts.Clear();

            // Clear UI containers (keeping titles)
            if (_inputZone != null)
                for (int i = _inputZone.childCount - 1; i >= 0; i--)
                    if (!_inputZone[i].ClassListContains("io-zone-title")) _inputZone.RemoveAt(i);

            if (_outputZone != null)
                for (int i = _outputZone.childCount - 1; i >= 0; i--)
                    if (!_outputZone[i].ClassListContains("io-zone-title")) _outputZone.RemoveAt(i);

            // Render IO Nodes
            foreach (var ioNode in graphTile.Graph.ioNodes)
            {
                CreateIOCardUI(ioNode);
            }
        }

        private void CreateIOCardUI(TileIONode ioNode)
        {
            var card = _tileIOCardTemplate.Instantiate();

            bool isInput = ioNode.type == TileIOType.Input;

            card.AddToClassList(isInput ? "input-card" : "output-card");

            var typeLabel = card.Q<Label>("io-card-type");
            if (isInput)
                typeLabel.text = ioNode.sourceTileType.ToString();
            else if (ioNode.type == TileIOType.Core)
                typeLabel.text = "Core";
            else
                typeLabel.text = "Output";

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
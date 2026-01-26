using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Types;

namespace CarbonWorld.Features.Tiles
{
    public class TransportTile : BaseTile, IGraphTile
    {
        public BlueprintGraph Graph { get; } = new();
        public bool HasOutput => true;
        // Transport tiles do not accept any blueprints
        public Func<BlueprintDefinition, bool> BlueprintFilter => b => false;

        public TransportTile(Vector3Int cellPosition)
            : base(cellPosition, TileType.Transport)
        {
        }

        public void UpdateIO(TileDataGrid tileData)
        {
            var oldIoNodes = Graph.ioNodes.ToList();
            Graph.ioNodes.Clear();

            var neighbors = tileData.GetNeighbors(CellPosition);
            int inputIndex = 0;

            foreach (var neighbor in neighbors)
            {
                if (neighbor is ResourceTile resourceTile)
                {
                    var output = resourceTile.GetOutput();
                    if (output.IsValid)
                    {
                        AddInputNode(neighbor.CellPosition, neighbor.Type, output, inputIndex++);
                    }
                }
                else if (neighbor is ProductionTile productionTile)
                {
                    var outputs = productionTile.GetOutputs();
                    foreach (var output in outputs)
                    {
                        AddInputNode(neighbor.CellPosition, neighbor.Type, output, inputIndex++);
                    }
                }
                else if (neighbor is TransportTile transportTile)
                {
                    var outputs = transportTile.GetOutputs();
                    foreach (var output in outputs)
                    {
                        AddInputNode(neighbor.CellPosition, neighbor.Type, output, inputIndex++);
                    }
                }
            }

            // Transport tile simply passes inputs to outputs
            // We create an output node for visualization if needed, but GetOutputs() does the real work
            // Preserving IDs for inputs
            foreach (var newNode in Graph.ioNodes.Where(n => n.type == TileIOType.Input))
            {
                var match = oldIoNodes.Find(old =>
                    old.type == TileIOType.Input &&
                    old.sourceTilePosition == newNode.sourceTilePosition &&
                    old.availableItem.Item == newNode.availableItem.Item);

                if (match != null)
                {
                    newNode.id = match.id;
                }
            }
        }

        private void AddInputNode(Vector3Int sourcePos, TileType type, ItemStack item, int index)
        {
            var node = new TileIONode(TileIOType.Input, sourcePos, type, item, index);
            Graph.ioNodes.Add(node);
        }

        public List<ItemStack> GetOutputs()
        {
            // For a transport tile, all inputs are valid outputs
            return Graph.ioNodes
                .Where(n => n.type == TileIOType.Input && n.availableItem.IsValid)
                .Select(n => n.availableItem)
                .ToList();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Types;

namespace CarbonWorld.Features.Tiles
{
    public class ProductionTile : BaseTile, IGraphTile
    {
        public BlueprintGraph Graph { get; } = new();
        public bool IsPowered { get; set; }
        public bool HasOutput => true;
        public Func<BlueprintDefinition, bool> BlueprintFilter => b => b.IsProducer || b.IsLogistics;

        public ProductionTile(Vector3Int cellPosition)
            : base(cellPosition, TileType.Production)
        {
        }

        public void UpdateIO(TileDataGrid tileData)
        {
            // Clear existing IO nodes but preserve valid connections if possible?
            // For now, simpler to rebuild and let the graph validation handle broken links if needed.
            // However, we want to preserve IDs if possible to keep connections valid.
            // But the current implementation in View was clearing them.
            // Let's try to match existing logic: clear and rebuild.
            
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
                    // Avoid infinite recursion or circular dependency if possible.
                    // Ideally we read the cached state of the neighbor.
                    // But here we calculate it on the fly.
                    var graphOutputs = productionTile.GetOutputs();
                    foreach (var neighborOutput in graphOutputs)
                    {
                        AddInputNode(neighbor.CellPosition, neighbor.Type, neighborOutput, inputIndex++);
                    }
                }
            }

            // Create/Update Output Node
            // First create it with empty item so we can add it to the graph
            // This is required because GetGraphOutputItem -> GetOutputs checks Graph.ioNodes to find the output node
            var outputNode = new TileIONode(
                TileIOType.Output,
                CellPosition,
                Type,
                ItemStack.Empty,
                0
            );
            
            // Preserve ID if it existed to keep connections valid
            var existingOutput = oldIoNodes.Find(n => n.type == TileIOType.Output);
            if (existingOutput != null)
            {
                outputNode.id = existingOutput.id;
            }

            // Add to graph temporarily (or permanently) so GetOutputs can find it
            Graph.ioNodes.Add(outputNode);

            // Now calculate the actual output
            var outputItem = GetGraphOutputItem();
            
            // Update the item on the node we just added
            outputNode.availableItem = outputItem;
            
            // For input nodes, we also should try to preserve IDs if the source is the same
            // to avoid breaking connections when neighbors haven't changed.
            foreach (var newNode in Graph.ioNodes.Where(n => n.type == TileIOType.Input))
            {
                var match = oldIoNodes.Find(old => 
                    old.type == TileIOType.Input && 
                    old.sourceTilePosition == newNode.sourceTilePosition && 
                    old.availableItem.Item == newNode.availableItem.Item // Match by item type too? Or just position?
                );
                
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
            var outputs = new List<ItemStack>();

            // Find connections that go to the output IO node
            var outputConnections = Graph.connections
                .Where(c => c.toNodeId != null && Graph.ioNodes.Any(io => io.id == c.toNodeId && io.type == TileIOType.Output))
                .ToList();

            foreach (var conn in outputConnections)
            {
                var sourceNode = Graph.GetNode(conn.fromNodeId);
                if (sourceNode?.blueprint != null && sourceNode.blueprint.Output.IsValid)
                {
                    outputs.Add(sourceNode.blueprint.Output);
                }
            }

            return outputs;
        }

        private ItemStack GetGraphOutputItem()
        {
            var outputs = GetOutputs();
            return outputs.Count > 0 ? outputs[0] : ItemStack.Empty;
        }
    }
}

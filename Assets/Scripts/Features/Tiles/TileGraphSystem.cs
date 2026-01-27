using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Events;
using CarbonWorld.Features.Inventories;
using Drawing;

namespace CarbonWorld.Features.Tiles
{
    public class TileGraphSystem : MonoBehaviour
    {
        private TileDataGrid _tileData;
        private WorldMap.WorldMap _worldMap;

        public void Initialize(TileDataGrid tileData, WorldMap.WorldMap worldMap)
        {
            _tileData = tileData;
            _worldMap = worldMap;
        }

        private void Update()
        {
            if (_tileData == null || _worldMap == null) return;

            var draw = Draw.ingame;
            
            foreach (var tile in _tileData.GetAllTiles())
            {
                if (tile is IGraphTile graphTile)
                {
                    foreach (var node in graphTile.Graph.ioNodes)
                    {
                        if (node.type == TileIOType.Input && node.availableItem.IsValid)
                        {
                            var start = _worldMap.CellToWorld(node.sourceTilePosition);
                            var end = _worldMap.CellToWorld(tile.CellPosition);
                            
                            // Offset slightly to avoid Z-fighting or look better
                            start.z = -0.5f;
                            end.z = -0.5f;

                            // Shorten the arrow slightly
                            var dir = (end - start).normalized;
                            var dist = Vector3.Distance(start, end);
                            var margin = 0.3f;
                            
                            if (dist > margin * 2)
                            {
                                draw.Arrow(start + dir * margin, end - dir * margin, new Color(0f, 0f, 0f, 1f));
                            }
                        }
                    }
                }
            }
        }

        public void RefreshAll(TileDataGrid tileData)
        {
            // Simple single pass for now. 
            // Ideally, this should be a topological sort or iterative convergence if cycles are allowed.
            // For now, we stick to the original behavior: just update everyone once.
            foreach (var tile in tileData.GetAllTiles())
            {
                UpdateTile(tileData, tile);
            }
        }

        public void UpdateNeighbors(TileDataGrid tileData, Vector3Int center)
        {
            // Update the center tile
            UpdateTile(tileData, tileData.GetTile(center));

            // Update all neighbors
            foreach (var neighbor in tileData.GetNeighbors(center))
            {
                UpdateTile(tileData, neighbor);
            }
        }

        public void UpdateTile(TileDataGrid tileData, BaseTile tile)
        {
            if (tile is ProductionTile productionTile)
            {
                UpdateProductionTile(tileData, productionTile);
            }
            else if (tile is TransportTile transportTile)
            {
                UpdateTransportTile(tileData, transportTile);
            }
        }

        private void UpdateProductionTile(TileDataGrid tileData, ProductionTile tile)
        {
            var graph = tile.Graph;
            bool changed = false;

            // 1. Update Inputs
            var newInputs = new List<TileIONode>();
            var neighbors = tileData.GetNeighbors(tile.CellPosition);
            int inputIndex = 0;

            foreach (var neighbor in neighbors)
            {
                var outputs = GetOutputs(neighbor);
                foreach (var output in outputs)
                {
                    // Find existing or create new
                    var existingNode = graph.ioNodes.Find(n => 
                        n.type == TileIOType.Input && 
                        n.sourceTilePosition == neighbor.CellPosition && 
                        n.index == inputIndex);

                    if (existingNode != null)
                    {
                        // Update existing
                        if (existingNode.availableItem != output) // Check value equality if possible, or just reassign
                        {
                            existingNode.availableItem = output;
                            changed = true;
                        }
                        // Ensure other properties are sync'd if needed
                        existingNode.sourceTileType = neighbor.Type;
                        newInputs.Add(existingNode);
                    }
                    else
                    {
                        // Create new
                        var newNode = new TileIONode(TileIOType.Input, neighbor.CellPosition, neighbor.Type, output, inputIndex);
                        newInputs.Add(newNode);
                        changed = true;
                    }
                    inputIndex++;
                }
            }

            // 2. Update Output Node
            var outputNode = graph.ioNodes.Find(n => n.type == TileIOType.Output);
            if (outputNode == null)
            {
                outputNode = new TileIONode(TileIOType.Output, tile.CellPosition, tile.Type, ItemStack.Empty, 0);
                graph.ioNodes.Add(outputNode);
                changed = true;
            }

            // Remove stale inputs
            // We keep the output node, and replace the list of inputs with the valid ones we found/created
            var nodesToRemove = graph.ioNodes.Where(n => n.type == TileIOType.Input && !newInputs.Contains(n)).ToList();
            if (nodesToRemove.Count > 0)
            {
                foreach (var node in nodesToRemove)
                {
                    graph.ioNodes.Remove(node);
                    // Also remove connections to this node? 
                    // Ideally yes, but BlueprintGraph handles loose connections gracefully usually.
                    // For now, we leave connections until the user deletes them or we implement connection cleanup.
                }
                changed = true;
            }

            // Add new inputs that weren't in the list
            foreach (var node in newInputs)
            {
                if (!graph.ioNodes.Contains(node))
                {
                    graph.ioNodes.Add(node);
                }
            }

            // 3. Calculate Output
            var outputItem = GetGraphOutputItem(tile);
            if (outputNode.availableItem != outputItem)
            {
                outputNode.availableItem = outputItem;
                changed = true;
            }

            // Sync Inventory
            using (new InventoryBatch(tile.Inventory, this, "GraphUpdate"))
            {
                tile.Inventory.Clear();
                if (outputItem.IsValid)
                {
                    tile.Inventory.Add(outputItem);
                }
            }

            if (changed)
            {
                graph.NotifyGraphUpdated();
                EventBus.Publish(new GraphUpdated { Position = tile.CellPosition });
            }
        }

        private void UpdateTransportTile(TileDataGrid tileData, TransportTile tile)
        {
            var graph = tile.Graph;
            bool changed = false;

            var newInputs = new List<TileIONode>();
            var neighbors = tileData.GetNeighbors(tile.CellPosition);
            int inputIndex = 0;

            foreach (var neighbor in neighbors)
            {
                var outputs = GetOutputs(neighbor);
                foreach (var output in outputs)
                {
                    var existingNode = graph.ioNodes.Find(n => 
                        n.type == TileIOType.Input && 
                        n.sourceTilePosition == neighbor.CellPosition && 
                        n.index == inputIndex);

                    if (existingNode != null)
                    {
                        if (existingNode.availableItem != output)
                        {
                            existingNode.availableItem = output;
                            changed = true;
                        }
                        existingNode.sourceTileType = neighbor.Type;
                        newInputs.Add(existingNode);
                    }
                    else
                    {
                        var newNode = new TileIONode(TileIOType.Input, neighbor.CellPosition, neighbor.Type, output, inputIndex);
                        newInputs.Add(newNode);
                        changed = true;
                    }
                    inputIndex++;
                }
            }

            var nodesToRemove = graph.ioNodes.Where(n => n.type == TileIOType.Input && !newInputs.Contains(n)).ToList();
            if (nodesToRemove.Count > 0)
            {
                foreach (var node in nodesToRemove) graph.ioNodes.Remove(node);
                changed = true;
            }

            foreach (var node in newInputs)
            {
                if (!graph.ioNodes.Contains(node)) graph.ioNodes.Add(node);
            }

            if (changed)
            {
                graph.NotifyGraphUpdated();
            }
        }

        private List<ItemStack> GetOutputs(BaseTile tile)
        {
            if (tile is ResourceTile resourceTile)
            {
                var output = resourceTile.GetOutput();
                return output.IsValid ? new List<ItemStack> { output } : new List<ItemStack>();
            }
            else if (tile is ProductionTile productionTile)
            {
                return productionTile.GetOutputs();
            }
            else if (tile is TransportTile transportTile)
            {
                return transportTile.GetOutputs();
            }
            return new List<ItemStack>();
        }

        private ItemStack GetGraphOutputItem(ProductionTile tile)
        {
            var outputs = tile.GetOutputs();
            return outputs.Count > 0 ? outputs[0] : ItemStack.Empty;
        }
    }
}

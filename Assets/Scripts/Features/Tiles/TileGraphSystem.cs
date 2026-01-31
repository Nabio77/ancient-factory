using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Events;
using CarbonWorld.Features.Inventories;
using Drawing;
using CarbonWorld.Core.Types;

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
                // Draw arrows to Graph Tiles (Production/Transport)
                if (tile is IGraphTile graphTile)
                {
                    foreach (var node in graphTile.Graph.ioNodes)
                    {
                        if (node.type == TileIOType.Input && node.availableItem.IsValid)
                        {
                            var start = _worldMap.CellToWorld(node.sourceTilePosition);
                            var end = _worldMap.CellToWorld(tile.CellPosition);
                            
                            // Color based on source
                            Color color = Color.black;
                            if (node.sourceTileType == TileType.Resource) color = new Color(0.2f, 0.6f, 1f, 1f); // Blue-ish
                            else if (node.sourceTileType == TileType.Production) color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green-ish
                            else if (node.sourceTileType == TileType.Food) color = new Color(0.8f, 0.6f, 0.2f, 1f); // Orange-ish
                            else if (node.sourceTileType == TileType.Transport) color = new Color(0.2f, 0.8f, 0.2f, 1f); // Also Green-ish
                            
                            DrawConnectionArrow(draw, start, end, color);
                        }
                    }
                }
                
                // Draw arrows to Settlements
                if (tile is SettlementTile settlement)
                {
                    var neighbors = _tileData.GetNeighbors(settlement.CellPosition);
                    foreach (var neighbor in neighbors)
                    {
                        bool canSupply = false;
                        if (neighbor is ProductionTile prod)
                        {
                            canSupply = prod.GetPotentialOutputs().Any(o => settlement.Demands.Any(d => d.Item == o.Item));
                        }
                        else if (neighbor is TransportTile trans)
                        {
                            canSupply = trans.GetOutputs().Any(o => settlement.Demands.Any(d => d.Item == o.Item));
                        }

                        if (canSupply)
                        {
                            var start = _worldMap.CellToWorld(neighbor.CellPosition);
                            var end = _worldMap.CellToWorld(settlement.CellPosition);
                            DrawConnectionArrow(draw, start, end, new Color(1f, 0.6f, 0.2f, 1f)); // Orange
                        }
                    }
                }
            }
        }

        private void DrawConnectionArrow(CommandBuilder draw, Vector3 start, Vector3 end, Color color)
        {
            // Offset slightly to avoid Z-fighting or look better
            start.z = -0.5f;
            end.z = -0.5f;

            // Shorten the arrow slightly
            var dir = (end - start).normalized;
            var dist = Vector3.Distance(start, end);
            var margin = 0.35f;
            
            if (dist > margin * 2)
            {
                draw.Arrow(start + dir * margin, end - dir * margin, color);
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
            if (tile is ProductionTile || tile is FoodTile)
            {
                UpdateGraphTile(tileData, tile);
            }
            else if (tile is TransportTile transportTile)
            {
                UpdateTransportTile(tileData, transportTile);
            }
        }

        private void UpdateGraphTile(TileDataGrid tileData, BaseTile tile)
        {
            if (tile is not IGraphTile graphTile) return;
            var graph = graphTile.Graph;
            bool changed = false;

            // 1. Update Inputs (with deduplication by original source)
            var newInputs = new List<TileIONode>();
            var neighbors = tileData.GetNeighbors(tile.CellPosition);
            var seenOriginalSources = new HashSet<Vector3Int>();
            int inputIndex = 0;

            foreach (var neighbor in neighbors)
            {
                var outputs = GetOutputsWithSource(neighbor);
                foreach (var output in outputs)
                {
                    // Skip if we've already seen this original source (prevents doubling via transport)
                    if (!seenOriginalSources.Add(output.OriginalSource))
                        continue;

                    // Find existing or create new
                    var existingNode = graph.ioNodes.Find(n =>
                        n.type == TileIOType.Input &&
                        n.originalSourcePosition == output.OriginalSource);

                    if (existingNode != null)
                    {
                        // Update existing
                        if (existingNode.availableItem != output.Item)
                        {
                            existingNode.availableItem = output.Item;
                            changed = true;
                        }
                        existingNode.sourceTilePosition = neighbor.CellPosition;
                        existingNode.sourceTileType = neighbor.Type;
                        existingNode.index = inputIndex;
                        newInputs.Add(existingNode);
                    }
                    else
                    {
                        // Create new
                        var newNode = new TileIONode(TileIOType.Input, neighbor.CellPosition, neighbor.Type, output.Item, inputIndex, output.OriginalSource);
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

            // 2b. Update Core Output Node (Wireless)
            var coreOutputNode = graph.ioNodes.Find(n => n.type == TileIOType.Core);
            if (coreOutputNode == null)
            {
                coreOutputNode = new TileIONode(TileIOType.Core, tile.CellPosition, tile.Type, ItemStack.Empty, 0);
                graph.ioNodes.Add(coreOutputNode);
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

            // 3. Calculate potential output for UI visualization only
            // Actual output is managed by ProductionSystem via OutputBuffer
            var potentialOutput = GetGraphPotentialOutput(tile);
            if (outputNode.availableItem != potentialOutput)
            {
                outputNode.availableItem = potentialOutput;
                changed = true;
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
            var seenOriginalSources = new HashSet<Vector3Int>();
            int inputIndex = 0;

            foreach (var neighbor in neighbors)
            {
                var outputs = GetOutputsWithSource(neighbor);
                foreach (var output in outputs)
                {
                    // Skip if we've already seen this original source (prevents doubling in chains)
                    if (!seenOriginalSources.Add(output.OriginalSource))
                        continue;

                    var existingNode = graph.ioNodes.Find(n =>
                        n.type == TileIOType.Input &&
                        n.originalSourcePosition == output.OriginalSource);

                    if (existingNode != null)
                    {
                        if (existingNode.availableItem != output.Item)
                        {
                            existingNode.availableItem = output.Item;
                            changed = true;
                        }
                        existingNode.sourceTilePosition = neighbor.CellPosition;
                        existingNode.sourceTileType = neighbor.Type;
                        existingNode.index = inputIndex;
                        newInputs.Add(existingNode);
                    }
                    else
                    {
                        var newNode = new TileIONode(TileIOType.Input, neighbor.CellPosition, neighbor.Type, output.Item, inputIndex, output.OriginalSource);
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

            // Sync Inventory for icon display
            using (new InventoryBatch(tile.Inventory, this, "TransportGraphUpdate"))
            {
                tile.Inventory.Clear();
                var outputs = tile.GetOutputs();
                foreach (var output in outputs)
                {
                    if (output.IsValid)
                    {
                        tile.Inventory.Add(output);
                    }
                }
            }

            if (changed)
            {
                graph.NotifyGraphUpdated();
                EventBus.Publish(new GraphUpdated { Position = tile.CellPosition });
            }
        }

        private List<TileOutput> GetOutputsWithSource(BaseTile tile)
        {
            if (tile is ResourceTile resourceTile)
            {
                var output = resourceTile.GetOutput();
                if (output.IsValid)
                {
                    return new List<TileOutput> { new TileOutput(output, tile.CellPosition) };
                }
                return new List<TileOutput>();
            }
            else if (tile is ProductionTile || tile is FoodTile)
            {
                List<ItemStack> potentialOutputs = null;
                if (tile is ProductionTile p) potentialOutputs = p.GetPotentialOutputs();
                else if (tile is FoodTile f) potentialOutputs = f.GetPotentialOutputs();

                if (potentialOutputs != null)
                {
                    return potentialOutputs
                        .Where(o => o.IsValid)
                        .Select(o => new TileOutput(o, tile.CellPosition))
                        .ToList();
                }
            }
            else if (tile is TransportTile transportTile)
            {
                // Transport tiles preserve original source from their inputs
                return transportTile.GetOutputsWithSource();
            }
            return new List<TileOutput>();
        }

        private ItemStack GetGraphPotentialOutput(BaseTile tile)
        {
            List<ItemStack> outputs = null;
            if (tile is ProductionTile p) outputs = p.GetPotentialOutputs();
            else if (tile is FoodTile f) outputs = f.GetPotentialOutputs();

            return outputs != null && outputs.Count > 0 ? outputs[0] : ItemStack.Empty;
        }
    }
}

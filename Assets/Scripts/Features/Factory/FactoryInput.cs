using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AncientFactory.Core.Data;
using AncientFactory.Features.Tiles;
using AncientFactory.Features.WorldMap;

namespace AncientFactory.Features.Factory
{
    public class FactoryInput
    {
        private readonly TileDataGrid _tileData;

        public FactoryInput(TileDataGrid tileData)
        {
            _tileData = tileData;
        }

        public bool TryResolveInputs(IFactoryTile targetTile, BlueprintNode node, IEnumerable<ItemStack> inputs, bool simulateOnly)
        {
            foreach (var requiredInput in inputs)
            {
                int needed = requiredInput.Amount;
                int found = 0;

                var incomingConnections = targetTile.Graph.connections
                    .Where(c => c.toNodeId == node.id)
                    .ToList();

                foreach (var conn in incomingConnections)
                {
                    if (found >= needed) break;

                    int remaining = needed - found;

                    var ioNode = targetTile.Graph.GetIONode(conn.fromNodeId);
                    if (ioNode != null && ioNode.availableItem.Item == requiredInput.Item)
                    {
                        if (simulateOnly)
                        {
                            found += GetAvailableFromSource(ioNode);
                        }
                        else
                        {
                            found += PullFromSource(ioNode, remaining);
                        }
                    }
                    else
                    {
                        var sourceNode = targetTile.Graph.GetNode(conn.fromNodeId);
                        if (sourceNode != null && sourceNode.blueprint != null && sourceNode.blueprint.Output.Item == requiredInput.Item)
                        {
                            int available = targetTile.OutputBuffer.Get(requiredInput.Item);
                            int toTake = Mathf.Min(remaining, available);

                            found += toTake;

                            if (!simulateOnly && toTake > 0)
                            {
                                targetTile.OutputBuffer.Remove(requiredInput.Item, toTake);
                            }
                        }
                    }
                }

                if (found < needed) return false;
            }

            return true;
        }

        private int GetAvailableFromSource(TileIONode ioNode)
        {
            var sourceTile = _tileData.GetTile(ioNode.sourceTilePosition);
            if (sourceTile == null) return 0;

            var actualSourceTile = sourceTile;
            if (sourceTile is TransportTile)
            {
                actualSourceTile = _tileData.GetTile(ioNode.originalSourcePosition);
            }
            if (actualSourceTile == null) return 0;

            var item = ioNode.availableItem.Item;

            if (actualSourceTile is FactoryTile sourceFactory)
            {
                return sourceFactory.OutputBuffer.Get(item);
            }
            else if (actualSourceTile is ResourceTile resourceTile)
            {
                return resourceTile.IsDepleted ? 0 : resourceTile.GetOutputPerTick();
            }
            else
            {
                return actualSourceTile.Inventory.Get(item);
            }
        }

        private int PullFromSource(TileIONode ioNode, int maxAmount)
        {
            var sourceTile = _tileData.GetTile(ioNode.sourceTilePosition);
            if (sourceTile == null) return 0;

            var actualSourceTile = sourceTile;
            if (sourceTile is TransportTile)
            {
                actualSourceTile = _tileData.GetTile(ioNode.originalSourcePosition);
            }
            if (actualSourceTile == null) return 0;

            var item = ioNode.availableItem.Item;

            if (actualSourceTile is FactoryTile sourceFactory)
            {
                int available = sourceFactory.OutputBuffer.Get(item);
                int toPull = Mathf.Min(maxAmount, available);
                if (toPull > 0)
                {
                    sourceFactory.OutputBuffer.Remove(item, toPull);
                }
                return toPull;
            }
            else if (actualSourceTile is ResourceTile resourceTile)
            {
                if (!resourceTile.IsDepleted && resourceTile.ResourceItem == item)
                {
                    var output = resourceTile.GetOutput();
                    return output.IsValid ? Mathf.Min(output.Amount, maxAmount) : 0;
                }
                return 0;
            }
            else
            {
                int available = actualSourceTile.Inventory.Get(item);
                int toPull = Mathf.Min(maxAmount, available);
                if (toPull > 0)
                {
                    actualSourceTile.Inventory.Remove(item, toPull);
                }
                return toPull;
            }
        }
    }
}

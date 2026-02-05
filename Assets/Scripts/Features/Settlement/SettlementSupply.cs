using System.Linq;
using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.Inventories;
using CarbonWorld.Features.WorldMap;

namespace CarbonWorld.Features.Settlement
{
    public class SettlementSupply
    {
        private readonly TileDataGrid _tileData;

        public SettlementSupply(TileDataGrid tileData)
        {
            _tileData = tileData;
        }

        public void DistributeToAllSettlements()
        {
            foreach (var settlement in _tileData.SettlementTiles)
            {
                DistributeToSettlement(settlement);
            }
        }

        private void DistributeToSettlement(SettlementTile settlement)
        {
            var neighbors = _tileData.GetNeighbors(settlement.CellPosition);

            foreach (var neighbor in neighbors)
            {
                if (neighbor is IFactoryTile factory and not PowerTile)
                {
                    TransferItemsToSettlement(factory.OutputBuffer, settlement);
                }
                else if (neighbor is TransportTile sourceTrans)
                {
                    TransferItemsToSettlementFromTransport(sourceTrans, settlement);
                }
            }
        }

        private void TransferItemsToSettlement(Inventory sourceBuffer, SettlementTile settlement)
        {
            var items = sourceBuffer.GetAll().ToList();

            foreach (var stack in items)
            {
                if (!stack.IsValid)
                    continue;

                var demand = settlement.Demands.FirstOrDefault(d => d.Item == stack.Item);
                if (!demand.IsValid)
                    continue;

                int currentAmount = settlement.Inventory.Get(stack.Item);
                int needed = demand.Amount - currentAmount;
                if (needed <= 0)
                    continue;

                int toTransfer = Mathf.Min(needed, stack.Amount);
                sourceBuffer.Remove(stack.Item, toTransfer);
                settlement.Inventory.Add(stack.Item, toTransfer);
            }
        }

        private void TransferItemsToSettlementFromTransport(TransportTile transport, SettlementTile settlement)
        {
            foreach (var node in transport.Graph.ioNodes)
            {
                if (node.type != TileIOType.Input || !node.availableItem.IsValid)
                    continue;

                var item = node.availableItem.Item;

                var demand = settlement.Demands.FirstOrDefault(d => d.Item == item);
                if (!demand.IsValid) continue;

                int currentAmount = settlement.Inventory.Get(item);
                int needed = demand.Amount - currentAmount;
                if (needed <= 0) continue;

                var sourceTile = _tileData.GetTile(node.originalSourcePosition);
                if (sourceTile == null) continue;

                int toTransfer = 0;

                if (sourceTile is IFactoryTile factory)
                {
                    int available = factory.OutputBuffer.Get(item);
                    toTransfer = Mathf.Min(needed, Mathf.Min(node.availableItem.Amount, available));
                    if (toTransfer > 0)
                    {
                        factory.OutputBuffer.Remove(item, toTransfer);
                    }
                }
                else
                {
                    int available = sourceTile.Inventory.Get(item);
                    toTransfer = Mathf.Min(needed, Mathf.Min(node.availableItem.Amount, available));
                    if (toTransfer > 0)
                    {
                        sourceTile.Inventory.Remove(item, toTransfer);
                    }
                }

                if (toTransfer > 0)
                {
                    settlement.Inventory.Add(item, toTransfer);
                }
            }
        }
    }
}

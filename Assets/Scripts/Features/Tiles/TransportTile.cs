using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AncientFactory.Core.Data;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.Tiles
{
    public struct TileOutput
    {
        public ItemStack Item;
        public Vector3Int OriginalSource;

        public TileOutput(ItemStack item, Vector3Int originalSource)
        {
            Item = item;
            OriginalSource = originalSource;
        }
    }

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

        public List<ItemStack> GetOutputs()
        {
            // For a transport tile, all inputs are valid outputs
            return Graph.ioNodes
                .Where(n => n.type == TileIOType.Input && n.availableItem.IsValid)
                .Select(n => n.availableItem)
                .ToList();
        }

        public List<TileOutput> GetOutputsWithSource()
        {
            return Graph.ioNodes
                .Where(n => n.type == TileIOType.Input && n.availableItem.IsValid)
                .Select(n => new TileOutput(n.availableItem, n.originalSourcePosition))
                .ToList();
        }
    }
}

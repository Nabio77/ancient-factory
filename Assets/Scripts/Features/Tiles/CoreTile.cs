using UnityEngine;
using System.Collections.Generic;
using AncientFactory.Core.Types;
using AncientFactory.Core.Data;

namespace AncientFactory.Features.Tiles
{
    public class CoreTile : BaseTile
    {
        public long AccumulatedPoints { get; set; }
        public Dictionary<ItemDefinition, int> CollectedItems { get; } = new();

        public CoreTile(Vector3Int cellPosition, TileType type) : base(cellPosition, type)
        {
        }

        public void AddPoints(long amount, ItemDefinition itemSource = null, int quantity = 0)
        {
            AccumulatedPoints += amount;

            if (itemSource != null && quantity > 0)
            {
                if (!CollectedItems.ContainsKey(itemSource))
                {
                    CollectedItems[itemSource] = 0;
                }
                CollectedItems[itemSource] += quantity;
            }
        }
    }

}
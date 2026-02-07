using UnityEngine;
using System.Collections.Generic;
using AncientFactory.Core.Types;
using AncientFactory.Core.Data;
using AncientFactory.Core.Systems;

namespace AncientFactory.Features.Tiles
{


    public class CoreTile : BaseTile
    {
        // Points are still local to the specific Core tile (could be multiple in theory, or just one)
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

        public void AddWonderContribution(ItemDefinition item, int quantity)
        {
            if (WonderSystem.Instance != null)
            {
                WonderSystem.Instance.AddContribution(item, quantity, out int accepted);

                // If the Wonder accepted the item, we also give points for it.
                if (accepted > 0)
                {
                    AddPoints(item.TechPoints * accepted, item, accepted);
                }
            }
        }
    }

}
using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Features.Tiles
{
    public class ResourceTile : BaseTile
    {
        public ItemDefinition ResourceItem { get; set; }
        public ResourceQuality Quality { get; set; }

        public ResourceTile(Vector3Int cellPosition, ItemDefinition resourceItem, ResourceQuality quality = ResourceQuality.Normal)
            : base(cellPosition, TileType.Resource)
        {
            ResourceItem = resourceItem;
            Quality = quality;

            if (ResourceItem != null)
            {
                Inventory.Add(ResourceItem, 1);
            }
        }

        public int GetOutputPerTick()
        {
            return Quality switch
            {
                ResourceQuality.Impure => 1,
                ResourceQuality.Normal => 2,
                ResourceQuality.Pure => 3,
                _ => 1
            };
        }

        public ItemStack GetOutput()
        {
            return new ItemStack(ResourceItem, GetOutputPerTick());
        }
    }
}

using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Types;

namespace CarbonWorld.Features.Tiles
{
    public class ResourceTile : BaseTile
    {
        public ItemDefinition ResourceItem { get; set; }
        public int OutputPerTick { get; set; }

        public ResourceTile(Vector3Int cellPosition, ItemDefinition resourceItem, int outputPerTick)
            : base(cellPosition, TileType.Resource)
        {
            ResourceItem = resourceItem;
            OutputPerTick = outputPerTick;
        }

        public ItemStack GetOutput()
        {
            return new ItemStack(ResourceItem, OutputPerTick);
        }
    }
}

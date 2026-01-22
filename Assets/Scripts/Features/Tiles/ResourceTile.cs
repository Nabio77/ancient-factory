using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;

namespace CarbonWorld.Features.Tiles
{
    public class ResourceTile : Tile
    {
        [Title("Resource")]
        [SerializeField, Required]
        private ItemDefinition resourceItem;

        [SerializeField, Min(1)]
        private int outputPerTick = 1;

        public ItemDefinition ResourceItem => resourceItem;
        public int OutputPerTick => outputPerTick;

        public ItemStack GetOutput()
        {
            return new ItemStack(resourceItem, outputPerTick);
        }
    }
}

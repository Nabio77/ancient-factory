using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Features.Tiles
{
    public class ResourceTile : BaseTile
    {
        public ItemDefinition ResourceItem { get; set; }
        public ResourceQuality Quality { get; set; }
        
        public int CurrentAmount { get; private set; }
        public int MaxAmount { get; private set; }

        public bool IsDepleted => CurrentAmount <= 0;

        public ResourceTile(Vector3Int cellPosition, ItemDefinition resourceItem, ResourceQuality quality = ResourceQuality.Normal, int amount = 100)
            : base(cellPosition, TileType.Resource)
        {
            ResourceItem = resourceItem;
            Quality = quality;
            MaxAmount = amount;
            CurrentAmount = amount;

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
            if (IsDepleted) return ItemStack.Empty;

            int outputAmount = GetOutputPerTick();
            // Clamp output to remaining amount
            int actualOutput = Mathf.Min(outputAmount, CurrentAmount);
            
            CurrentAmount -= actualOutput;
            
            return new ItemStack(ResourceItem, actualOutput);
        }
    }
}

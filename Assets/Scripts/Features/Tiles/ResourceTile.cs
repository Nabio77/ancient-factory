using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.Grid;

namespace CarbonWorld.Features.Tiles
{
    public class ResourceTile : Tile
    {
        [Title("Resource")]
        [SerializeField]
        private ItemDefinition resourceItem;

        [SerializeField, Min(1)]
        private int outputPerTick = 1;

        [Title("Visuals")]
        [SerializeField, Required]
        private SpriteRenderer iconRenderer;

        public ItemDefinition ResourceItem
        {
            get => resourceItem;
            set
            {
                resourceItem = value;
                UpdateIcon();
            }
        }

        public int OutputPerTick { get => outputPerTick; set => outputPerTick = value; }

        public override void Initialize(HexCoord coord)
        {
            base.Initialize(coord);
            UpdateIcon();
        }

        private void UpdateIcon()
        {
            if (iconRenderer != null && resourceItem != null)
            {
                iconRenderer.sprite = resourceItem.Icon;
            }
        }

        public ItemStack GetOutput()
        {
            return new ItemStack(resourceItem, outputPerTick);
        }
    }
}

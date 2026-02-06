using UnityEngine;
using AncientFactory.Features.Inventories;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.Tiles
{
    public class BaseTile
    {
        public Vector3Int CellPosition { get; }
        public TileType Type { get; }
        public Inventory Inventory { get; } = new();
        public bool IsHovered { get; set; }
        public bool IsSelected { get; set; }

        public BaseTile(Vector3Int cellPosition, TileType type)
        {
            CellPosition = cellPosition;
            Type = type;
        }
    }
}

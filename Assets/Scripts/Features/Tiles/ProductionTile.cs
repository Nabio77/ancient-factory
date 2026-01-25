using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Types;

namespace CarbonWorld.Features.Tiles
{
    public class ProductionTile : BaseTile
    {
        public BlueprintGraph Graph { get; } = new();
        public bool IsPowered { get; set; }

        public ProductionTile(Vector3Int cellPosition)
            : base(cellPosition, TileType.Production)
        {
        }
    }
}

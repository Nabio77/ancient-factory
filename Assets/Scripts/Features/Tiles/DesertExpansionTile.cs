using UnityEngine;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.Tiles
{
    public class DesertExpansionTile : DisasterTile
    {
        public DesertExpansionTile(Vector3Int cellPosition, int spawnTick)
            : base(cellPosition, TileType.DesertExpansion, spawnTick)
        {
        }

        public override bool CanSpread => true;
    }
}

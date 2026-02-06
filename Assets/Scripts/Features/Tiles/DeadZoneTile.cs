using UnityEngine;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.Tiles
{
    public class DeadZoneTile : DisasterTile
    {
        public DeadZoneTile(Vector3Int cellPosition, int spawnTick)
            : base(cellPosition, TileType.DeadZone, spawnTick)
        {
        }
    }
}

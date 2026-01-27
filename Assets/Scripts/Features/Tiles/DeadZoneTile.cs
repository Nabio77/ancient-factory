using UnityEngine;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Features.Tiles
{
    public class DeadZoneTile : DisasterTile
    {
        public DeadZoneTile(Vector3Int cellPosition, int spawnTick)
            : base(cellPosition, TileType.DeadZone, spawnTick)
        {
        }
    }
}

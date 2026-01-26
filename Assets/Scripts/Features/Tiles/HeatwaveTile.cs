using UnityEngine;
using CarbonWorld.Types;

namespace CarbonWorld.Features.Tiles
{
    public class HeatwaveTile : DisasterTile
    {
        public HeatwaveTile(Vector3Int cellPosition, int spawnTick)
            : base(cellPosition, TileType.Heatwave, spawnTick)
        {
        }
    }
}

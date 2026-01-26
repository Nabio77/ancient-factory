using UnityEngine;
using CarbonWorld.Types;

namespace CarbonWorld.Features.Tiles
{
    public class FloodedTile : DisasterTile
    {
        public int FloodLevel { get; set; } = 1;

        public FloodedTile(Vector3Int cellPosition, int spawnTick)
            : base(cellPosition, TileType.Flooded, spawnTick)
        {
        }

        public override bool CanSpread => true;
    }
}

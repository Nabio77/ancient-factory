using UnityEngine;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Features.Tiles
{
    public class DisasterTile : BaseTile
    {
        public int SpawnTick { get; }
        public Vector3Int OriginalPosition { get; }

        public DisasterTile(Vector3Int cellPosition, TileType disasterType, int spawnTick)
            : base(cellPosition, disasterType)
        {
            SpawnTick = spawnTick;
            OriginalPosition = cellPosition;
        }

        public virtual bool CanSpread => false;
    }
}

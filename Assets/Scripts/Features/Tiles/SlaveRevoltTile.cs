using UnityEngine;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.Tiles
{
    public class SlaveRevoltTile : DisasterTile
    {
        public int RevoltStrength { get; set; }

        public SlaveRevoltTile(Vector3Int cellPosition, int spawnTick, int revoltStrength = 30)
            : base(cellPosition, TileType.SlaveRevolt, spawnTick)
        {
            RevoltStrength = revoltStrength;
        }

        public override bool CanSpread => false;
    }
}

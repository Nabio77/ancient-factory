using UnityEngine;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.Tiles
{
    public class PlagueTile : DisasterTile
    {
        public int InfectionLevel { get; set; } = 1;
        public int TicksUntilSpread { get; set; }

        public PlagueTile(Vector3Int cellPosition, int spawnTick, int ticksUntilSpread = 5)
            : base(cellPosition, TileType.Plague, spawnTick)
        {
            TicksUntilSpread = ticksUntilSpread;
        }

        public override bool CanSpread => true;
    }
}

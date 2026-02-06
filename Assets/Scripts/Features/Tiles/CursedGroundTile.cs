using UnityEngine;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.Tiles
{
    public class CursedGroundTile : DisasterTile
    {
        public int CurseLevel { get; set; } = 1;

        public CursedGroundTile(Vector3Int cellPosition, int spawnTick, int curseLevel = 1)
            : base(cellPosition, TileType.CursedGround, spawnTick)
        {
            CurseLevel = curseLevel;
        }

        public override bool CanSpread => true;
    }
}

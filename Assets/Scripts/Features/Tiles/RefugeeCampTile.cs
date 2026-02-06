using UnityEngine;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.Tiles
{
    public class RefugeeCampTile : DisasterTile
    {
        public int RefugeeCount { get; set; }
        public int FoodDrainPerTick { get; set; }

        public RefugeeCampTile(Vector3Int cellPosition, int spawnTick, int refugeeCount)
            : base(cellPosition, TileType.RefugeeCamp, spawnTick)
        {
            RefugeeCount = refugeeCount;
            FoodDrainPerTick = Mathf.Max(1, refugeeCount / 10);
        }
    }
}

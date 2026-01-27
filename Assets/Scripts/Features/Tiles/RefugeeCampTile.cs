using UnityEngine;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Features.Tiles
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

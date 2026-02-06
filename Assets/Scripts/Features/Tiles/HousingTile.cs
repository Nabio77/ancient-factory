using UnityEngine;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.Tiles
{
    public class HousingTile : BaseTile
    {
        public int Residents { get; set; } = 5;
        public int CommuteRadius { get; set; } = 1;
        public bool IsConnectedToSettlement { get; set; } = false;

        public HousingTile(Vector3Int cellPosition)
            : base(cellPosition, TileType.Housing)
        {
        }
    }
}

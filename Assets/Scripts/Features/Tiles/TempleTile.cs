using UnityEngine;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.Tiles
{
    public class TempleTile : BaseTile
    {
        public int DivineFavorGeneration { get; set; }
        public int InfluenceRadius { get; set; }

        public TempleTile(Vector3Int cellPosition, int favorGeneration = 10, int influenceRadius = 2)
            : base(cellPosition, TileType.Temple)
        {
            DivineFavorGeneration = favorGeneration;
            InfluenceRadius = influenceRadius;
        }

        public int GetDivineFavor()
        {
            return DivineFavorGeneration;
        }
    }
}

using UnityEngine;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.Tiles
{
    public class NatureTile : BaseTile
    {
        public int DivineFavorGeneration { get; set; }
        public bool IsHealthy { get; set; } = true;
        public bool IsSacredGrove { get; set; }

        public NatureTile(Vector3Int cellPosition, int divineFavorGeneration = 5, bool isSacredGrove = false)
            : base(cellPosition, TileType.Nature)
        {
            DivineFavorGeneration = divineFavorGeneration;
            IsSacredGrove = isSacredGrove;
        }

        public int GetDivineFavor()
        {
            if (!IsHealthy) return 0;
            return IsSacredGrove ? DivineFavorGeneration * 2 : DivineFavorGeneration;
        }
    }
}

using UnityEngine;
using CarbonWorld.Types;

namespace CarbonWorld.Features.Tiles
{
    public class NatureTile : BaseTile
    {
        public int CarbonAbsorptionRate { get; set; }
        public bool IsHealthy { get; set; } = true;

        public NatureTile(Vector3Int cellPosition, int carbonAbsorptionRate = 5)
            : base(cellPosition, TileType.Nature)
        {
            CarbonAbsorptionRate = carbonAbsorptionRate;
        }

        public int GetCarbonAbsorption()
        {
            return IsHealthy ? CarbonAbsorptionRate : 0;
        }
    }
}

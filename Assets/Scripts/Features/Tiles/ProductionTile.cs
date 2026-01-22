using UnityEngine;
using Sirenix.OdinInspector;

namespace CarbonWorld.Features.Tiles
{
    public class ProductionTile : Tile
    {
        [Title("Power")]
        [ShowInInspector, ReadOnly]
        private bool _isPowered;

        public bool IsPowered => _isPowered;

        public void SetPowered(bool powered)
        {
            _isPowered = powered;
        }
    }
}

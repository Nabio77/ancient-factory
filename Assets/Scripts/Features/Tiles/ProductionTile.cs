using UnityEngine;
using Sirenix.OdinInspector;

namespace CarbonWorld.Features.Tiles
{
    public class ProductionTile : Tile
    {
        [Title("Data")]
        [SerializeField]
        private CarbonWorld.Core.Data.BlueprintGraph graph = new();

        [Title("Power")]
        [ShowInInspector, ReadOnly]
        private bool _isPowered;

        public bool IsPowered => _isPowered;
        public CarbonWorld.Core.Data.BlueprintGraph Graph => graph;

        public void SetPowered(bool powered)
        {
            _isPowered = powered;
        }
    }
}

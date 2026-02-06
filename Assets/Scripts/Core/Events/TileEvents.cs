using AncientFactory.Features.Inventories;
using UnityEngine;

namespace AncientFactory.Core.Events
{
    public struct TileInventoryChanged : IEvent
    {
        public Vector3Int Position;
        public InventoryChangedArgs Changes;
    }

    public struct GraphUpdated : IEvent
    {
        public Vector3Int Position;
    }
}

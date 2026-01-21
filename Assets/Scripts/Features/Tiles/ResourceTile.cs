using UnityEngine;
using Sirenix.OdinInspector;

namespace CarbonWorld.Features.Tiles
{
    public enum ResourceType
    {
        Coal,
        Iron,
        Water,
        Stone,
        Wood,
        Fruit
    }

    public class ResourceTile : Tile
    {
        [Title("Resource")]
        [SerializeField]
        private ResourceType resourceType;

        public ResourceType ResourceType => resourceType;
    }
}

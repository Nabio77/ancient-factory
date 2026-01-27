using System;
using UnityEngine;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Core.Data
{
    public enum TileIOType
    {
        Input,
        Output
    }

    [Serializable]
    public class TileIONode
    {
        public string id;
        public TileIOType type;
        public Vector3Int sourceTilePosition;
        public TileType sourceTileType;
        public ItemStack availableItem;
        public int index;

        public TileIONode(TileIOType type, Vector3Int sourcePos, TileType tileType, ItemStack item, int index)
        {
            id = $"tile_io_{type.ToString().ToLower()}_{index}";
            this.type = type;
            sourceTilePosition = sourcePos;
            sourceTileType = tileType;
            availableItem = item;
            this.index = index;
        }
    }
}

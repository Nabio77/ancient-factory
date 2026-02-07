using System;
using UnityEngine;
using AncientFactory.Core.Types;

namespace AncientFactory.Core.Data
{
    public enum TileIOType
    {
        Input,
        Output,
        Core,
        Wonder
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
        public Vector3Int originalSourcePosition;

        public TileIONode(TileIOType type, Vector3Int sourcePos, TileType tileType, ItemStack item, int index, Vector3Int? originalSource = null)
        {
            id = $"tile_io_{type.ToString().ToLower()}_{index}";
            this.type = type;
            sourceTilePosition = sourcePos;
            sourceTileType = tileType;
            availableItem = item;
            this.index = index;
            originalSourcePosition = originalSource ?? sourcePos;
        }
    }
}

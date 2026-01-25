using System.Collections.Generic;
using UnityEngine;
using CarbonWorld.Features.Grid;

namespace CarbonWorld.Features.Tiles
{
    public class TileDataGrid
    {
        private readonly Dictionary<Vector3Int, BaseTile> _tiles = new();

        public void Add(Vector3Int pos, BaseTile tile)
        {
            _tiles[pos] = tile;
        }

        public void Remove(Vector3Int pos)
        {
            _tiles.Remove(pos);
        }

        public BaseTile GetTile(Vector3Int pos)
        {
            return _tiles.TryGetValue(pos, out var tile) ? tile : null;
        }

        public IEnumerable<BaseTile> GetAllTiles()
        {
            return _tiles.Values;
        }

        public IEnumerable<KeyValuePair<Vector3Int, BaseTile>> GetAllTilesWithPositions()
        {
            return _tiles;
        }

        public bool Contains(Vector3Int pos)
        {
            return _tiles.ContainsKey(pos);
        }

        public void Clear()
        {
            _tiles.Clear();
        }

        public int Count => _tiles.Count;

        public IEnumerable<Vector3Int> GetNeighborPositions(Vector3Int pos)
        {
            return HexUtils.GetNeighbors(pos);
        }

        public IEnumerable<BaseTile> GetNeighbors(Vector3Int pos)
        {
            foreach (var neighborPos in GetNeighborPositions(pos))
            {
                var tile = GetTile(neighborPos);
                if (tile != null)
                    yield return tile;
            }
        }
    }
}

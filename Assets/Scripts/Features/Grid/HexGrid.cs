using System.Collections.Generic;
using CarbonWorld.Features.Tiles;

namespace CarbonWorld.Features.Grid
{
    public class HexGrid
    {
        private readonly Dictionary<HexCoord, Tile> _tiles = new();

        public IReadOnlyDictionary<HexCoord, Tile> Tiles => _tiles;
        public int Count => _tiles.Count;

        public void Add(HexCoord coord, Tile tile)
        {
            _tiles[coord] = tile;
        }

        public void Remove(HexCoord coord)
        {
            _tiles.Remove(coord);
        }

        public void Clear()
        {
            _tiles.Clear();
        }

        public Tile GetTile(HexCoord coord)
        {
            return _tiles.TryGetValue(coord, out var tile) ? tile : null;
        }

        public bool Contains(HexCoord coord)
        {
            return _tiles.ContainsKey(coord);
        }
    }
}

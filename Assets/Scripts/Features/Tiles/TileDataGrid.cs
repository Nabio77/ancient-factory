using System.Collections.Generic;
using UnityEngine;
using AncientFactory.Features.Grid;

namespace AncientFactory.Features.Tiles
{
    public class TileDataGrid
    {
        private readonly Dictionary<Vector3Int, BaseTile> _tiles = new();

        // Optimized collections for system iteration
        public HashSet<IFactoryTile> FactoryTiles { get; } = new();
        public HashSet<PowerTile> PowerTiles { get; } = new();
        public HashSet<SettlementTile> SettlementTiles { get; } = new();

        public void Add(Vector3Int pos, BaseTile tile)
        {
            _tiles[pos] = tile;

            // Add to optimized collections
            if (tile is PowerTile powerTile) PowerTiles.Add(powerTile); // PowerTile is also IFactoryTile, check first or add to both if desired
            if (tile is IFactoryTile factoryTile) FactoryTiles.Add(factoryTile);
            if (tile is SettlementTile settlementTile) SettlementTiles.Add(settlementTile);
        }

        public void Remove(Vector3Int pos)
        {
            if (_tiles.TryGetValue(pos, out var tile))
            {
                if (tile is PowerTile powerTile) PowerTiles.Remove(powerTile);
                if (tile is IFactoryTile factoryTile) FactoryTiles.Remove(factoryTile);
                if (tile is SettlementTile settlementTile) SettlementTiles.Remove(settlementTile);

                _tiles.Remove(pos);
            }
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
            FactoryTiles.Clear();
            PowerTiles.Clear();
            SettlementTiles.Clear();
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

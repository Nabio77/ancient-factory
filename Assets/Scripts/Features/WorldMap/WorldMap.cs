using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Grid;
using CarbonWorld.Features.Tiles;

namespace CarbonWorld.Features.WorldMap
{
    public class WorldMap : MonoBehaviour
    {
        [Title("Configuration")]
        [SerializeField]
        private int rings = 5;

        [SerializeField]
        private int coreRadius = 1;

        [Title("Tile Prefabs")]
        [SerializeField, Required, AssetsOnly]
        private Tile coreTilePrefab;

        [SerializeField, Required, AssetsOnly]
        private Tile productionTilePrefab;

        private HexGrid _grid = new();

        public HexGrid Grid => _grid;

        [Button("Generate", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        public void Generate()
        {
            Clear();

            var coords = HexUtils.GetSpiral(HexCoord.Zero, rings);

            foreach (var coord in coords)
            {
                var prefab = GetPrefabForCoord(coord);
                if (prefab == null) continue;

                var worldPos = HexUtils.HexToWorld(coord);
                var tile = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                tile.Initialize(coord);
                tile.name = $"Tile {coord}";
                _grid.Add(coord, tile);
            }
        }

        [Button("Clear"), GUIColor(0.8f, 0.4f, 0.4f)]
        public void Clear()
        {
            _grid.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        private Tile GetPrefabForCoord(HexCoord coord)
        {
            int distance = HexUtils.Distance(HexCoord.Zero, coord);

            if (distance <= coreRadius)
                return coreTilePrefab;

            return productionTilePrefab;
        }
    }
}

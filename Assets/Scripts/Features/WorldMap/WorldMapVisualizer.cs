using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;
using AncientFactory.Core.Data;
using AncientFactory.Core.Types;
using AncientFactory.Features.Tiles;

namespace AncientFactory.Features.WorldMap
{
    public class WorldMapVisualizer : MonoBehaviour
    {
        [Title("Tilemap References")]
        [SerializeField, Required]
        private Tilemap tilemap;

        [SerializeField, Required]
        private Tilemap highlightTilemap;

        [Title("Tile Assets")]
        [SerializeField, Required]
        private TileBase coreTile;

        [SerializeField, Required]
        private TileBase resourceTile;

        [SerializeField, Required]
        private TileBase productionTile;

        [SerializeField, Required]
        private TileBase settlementTile;

        [SerializeField, Required]
        private TileBase powerTile;

        [SerializeField]
        private TileBase transportTile;

        [SerializeField]
        private TileBase natureTile;

        [SerializeField]
        private TileBase floodedTile;

        [SerializeField]
        private TileBase foodTile;

        [Title("Divine Tiles")]
        [SerializeField]
        private TileBase templeTile;

        [SerializeField]
        private TileBase plagueTile;

        [SerializeField]
        private TileBase slaveRevoltTile;

        [SerializeField]
        private TileBase cursedGroundTile;

        [SerializeField]
        private TileBase desertExpansionTile;

        [Title("Highlights")]
        [SerializeField]
        private TileBase hoverHighlightTile;

        [SerializeField]
        private TileBase selectedHighlightTile;

        [SerializeField]
        private TileBase powerRangeHighlightTile;

        [Title("Overrides")]
        [SerializeField]
        private List<ResourceVisualOverride> resourceVisualOverrides = new();

        public Tilemap Tilemap => tilemap;
        public Tilemap HighlightTilemap => highlightTilemap;
        public TileBase HoverHighlightTile => hoverHighlightTile;
        public TileBase SelectedHighlightTile => selectedHighlightTile;
        public TileBase PowerRangeHighlightTile => powerRangeHighlightTile;

        private void Awake()
        {
            EnsureOrdering();
        }

        private void EnsureOrdering()
        {
            if (tilemap == null || highlightTilemap == null) return;

            var mainRenderer = tilemap.GetComponent<TilemapRenderer>();
            var highlightRenderer = highlightTilemap.GetComponent<TilemapRenderer>();

            if (mainRenderer != null && highlightRenderer != null)
            {
                if (highlightRenderer.sortingOrder <= mainRenderer.sortingOrder)
                {
                    highlightRenderer.sortingOrder = mainRenderer.sortingOrder + 1;
                }
            }
        }

        public void Clear()
        {
            tilemap.ClearAllTiles();
            highlightTilemap.ClearAllTiles();
        }

        public void SetTile(Vector3Int position, TileType type, ItemDefinition item = null)
        {
            TileBase visual = GetVisualTile(type, item);
            tilemap.SetTile(position, visual);
        }

        public void RefreshAll(TileDataGrid tileData)
        {
            Clear();
            foreach (var tile in tileData.GetAllTiles())
            {
                ItemDefinition item = null;
                if (tile is ResourceTile rt) item = rt.ResourceItem;
                SetTile(tile.CellPosition, tile.Type, item);
            }
        }

        public TileBase GetVisualTile(TileType type, ItemDefinition item)
        {
            if (type == TileType.Resource && item != null)
            {
                var overrideRule = resourceVisualOverrides.Find(r => r.item == item);
                if (overrideRule.tile != null) return overrideRule.tile;
            }

            return type switch
            {
                TileType.Core => coreTile,
                TileType.Resource => resourceTile,
                TileType.Production => productionTile,
                TileType.Settlement => settlementTile,
                TileType.Power => powerTile,
                TileType.Nature => natureTile,
                TileType.Transport => transportTile,
                TileType.Food => foodTile,
                TileType.Temple => templeTile,
                TileType.Flooded => floodedTile,
                TileType.Plague => plagueTile,
                TileType.SlaveRevolt => slaveRevoltTile,
                TileType.CursedGround => cursedGroundTile,
                TileType.DesertExpansion => desertExpansionTile,
                _ => productionTile
            };
        }

        public Vector3 CellToWorld(Vector3Int cellPos)
        {
            return tilemap.GetCellCenterWorld(cellPos);
        }

        public Vector3Int WorldToCell(Vector3 worldPos)
        {
            return tilemap.WorldToCell(worldPos);
        }
    }

    // Moved from WorldMap.cs
    [Serializable]
    public struct ResourceVisualOverride
    {
        public ItemDefinition item;
        public TileBase tile;
    }
}

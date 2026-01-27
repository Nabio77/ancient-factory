using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.Inventories;

namespace CarbonWorld.Features.WorldMap
{
    public class TileIconOverlay : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [Title("Settings")]
        [SerializeField]
        private float iconScale = 0.5f;

        [SerializeField]
        private float zOffset = -0.1f;

        [SerializeField]
        private int sortingOrder = 10;

        [SerializeField]
        private string sortingLayer = "Default";

        private Dictionary<Vector3Int, SpriteRenderer> _iconRenderers = new();
        private Dictionary<Vector3Int, Action<InventoryChangedArgs>> _inventoryHandlers = new();
        private Transform _iconContainer;

        private void Awake()
        {
            _iconContainer = new GameObject("TileIcons").transform;
            _iconContainer.SetParent(transform);
        }

        private void OnEnable()
        {
            if (worldMap != null)
            {
                worldMap.OnMapGenerated += RefreshAllIcons;
                worldMap.OnTileChanged += OnTileChanged;
            }
        }

        private void OnDisable()
        {
            if (worldMap != null)
            {
                worldMap.OnMapGenerated -= RefreshAllIcons;
                worldMap.OnTileChanged -= OnTileChanged;
            }
            UnsubscribeAll();
        }

        private void Start()
        {
            RefreshAllIcons();
        }

        [Button("Refresh Icons")]
        public void RefreshAllIcons()
        {
            if (worldMap == null || worldMap.TileData == null) return;

            UnsubscribeAll();
            ClearAllIcons();

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                SubscribeToTile(tile);
                UpdateIconForTile(tile);
            }
        }

        private void OnTileChanged(Vector3Int position)
        {
            // Unsubscribe from old tile if exists
            UnsubscribeFromPosition(position);

            var tile = worldMap.TileData.GetTile(position);
            if (tile != null)
            {
                SubscribeToTile(tile);
                UpdateIconForTile(tile);
            }
            else
            {
                RemoveIcon(position);
            }
        }

        private void SubscribeToTile(BaseTile tile)
        {
            var position = tile.CellPosition;

            Action<InventoryChangedArgs> handler = _ => UpdateIconForTile(tile);
            tile.Inventory.InventoryChanged += handler;
            _inventoryHandlers[position] = handler;
        }

        private void UnsubscribeFromPosition(Vector3Int position)
        {
            if (_inventoryHandlers.TryGetValue(position, out var handler))
            {
                var tile = worldMap.TileData.GetTile(position);
                if (tile != null)
                {
                    tile.Inventory.InventoryChanged -= handler;
                }
                _inventoryHandlers.Remove(position);
            }
        }

        private void UnsubscribeAll()
        {
            foreach (var kvp in _inventoryHandlers)
            {
                var tile = worldMap.TileData.GetTile(kvp.Key);
                if (tile != null)
                {
                    tile.Inventory.InventoryChanged -= kvp.Value;
                }
            }
            _inventoryHandlers.Clear();
        }

        private void UpdateIconForTile(BaseTile tile)
        {
            if (tile == null) return;

            var icon = GetIconForTile(tile);
            var position = tile.CellPosition;

            if (icon == null)
            {
                RemoveIcon(position);
                return;
            }

            SetIcon(position, icon);
        }

        private Sprite GetIconForTile(BaseTile tile)
        {
            // For resource tiles, show the resource item icon directly
            if (tile is ResourceTile resourceTile && resourceTile.ResourceItem != null)
            {
                return resourceTile.ResourceItem.Icon;
            }

            // For production tiles, show the output item icon
            if (tile is ProductionTile productionTile)
            {
                var outputNode = productionTile.Graph.ioNodes
                    .FirstOrDefault(n => n.type == TileIOType.Output && n.availableItem.IsValid);
                if (outputNode != null)
                {
                    return outputNode.availableItem.Item?.Icon;
                }
            }

            // For other tiles, show the first item in inventory
            var firstItem = tile.Inventory.GetAll().FirstOrDefault();
            if (firstItem.IsValid)
            {
                return firstItem.Item?.Icon;
            }

            return null;
        }

        private void SetIcon(Vector3Int cellPosition, Sprite icon)
        {
            if (_iconRenderers.TryGetValue(cellPosition, out var renderer))
            {
                renderer.sprite = icon;
            }
            else
            {
                renderer = CreateIconRenderer(cellPosition);
                renderer.sprite = icon;
                _iconRenderers[cellPosition] = renderer;
            }

            renderer.gameObject.SetActive(true);
        }

        private void RemoveIcon(Vector3Int cellPosition)
        {
            if (_iconRenderers.TryGetValue(cellPosition, out var renderer))
            {
                renderer.gameObject.SetActive(false);
                renderer.sprite = null;
            }
        }

        private SpriteRenderer CreateIconRenderer(Vector3Int cellPosition)
        {
            var worldPos = worldMap.CellToWorld(cellPosition);
            worldPos.z = zOffset;

            var go = new GameObject($"Icon_{cellPosition}");
            go.transform.SetParent(_iconContainer);
            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one * iconScale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.sortingLayerName = sortingLayer;

            return renderer;
        }

        private void ClearAllIcons()
        {
            foreach (var renderer in _iconRenderers.Values)
            {
                if (renderer != null)
                {
                    Destroy(renderer.gameObject);
                }
            }
            _iconRenderers.Clear();
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
            ClearAllIcons();
        }
    }
}

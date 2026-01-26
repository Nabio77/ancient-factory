using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Tiles;

namespace CarbonWorld.Features.WorldMap
{
    public class TileSelector : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [Title("State")]
        [ShowInInspector, ReadOnly]
        private BaseTile _hoveredTile;

        [ShowInInspector, ReadOnly]
        private BaseTile _selectedTile;

        [ShowInInspector, ReadOnly]
        private Vector3Int _hoveredCell;

        [ShowInInspector, ReadOnly]
        private Vector3Int _selectedCell;

        public BaseTile HoveredTile => _hoveredTile;
        public BaseTile SelectedTile => _selectedTile;

        public event Action<BaseTile> OnTileHovered;
        public event Action OnTileHoverEnded;
        public event Action<BaseTile> OnTileSelected;
        public event Action OnTileDeselected;

        private Camera _camera;

        void Awake()
        {
            _camera = Camera.main;
        }

        void Update()
        {
            HandleHover();
            HandleClick();
        }

        private void HandleHover()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }

            // Convert screen position to world position (for 2D orthographic camera)
            Vector3 mouseScreenPos = mouse.position.ReadValue();
            mouseScreenPos.z = -_camera.transform.position.z;
            Vector3 worldPos = _camera.ScreenToWorldPoint(mouseScreenPos);

            // Convert world position to tilemap cell
            var cellPos = worldMap.WorldToCell(worldPos);

            // Check if this cell has a tile
            var tile = worldMap.TileData.GetTile(cellPos);

            if (tile != null)
            {
                if (tile != _hoveredTile)
                {
                    SetHoveredTile(tile, cellPos);
                }
            }
            else
            {
                ClearHover();
            }
        }

        private void HandleClick()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            if (_hoveredTile != null)
            {
                SelectTile(_hoveredTile, _hoveredCell);
            }
            else
            {
                Deselect();
            }
        }

        private void SetHoveredTile(BaseTile tile, Vector3Int cellPos)
        {
            // Clear previous hover highlight
            if (_hoveredTile != null)
            {
                _hoveredTile.IsHovered = false;
                // Only clear if not selected
                if (_hoveredCell != _selectedCell)
                {
                    worldMap.HighlightTilemap.SetTile(_hoveredCell, null);
                }
            }

            _hoveredTile = tile;
            _hoveredCell = cellPos;
            _hoveredTile.IsHovered = true;

            // Set hover highlight (only if not selected)
            bool isSelected = _selectedTile != null && _selectedCell == cellPos;
            if (!isSelected && worldMap.HoverHighlightTile != null)
            {
                worldMap.HighlightTilemap.SetTile(cellPos, worldMap.HoverHighlightTile);
            }

            OnTileHovered?.Invoke(tile);
        }

        private void ClearHover()
        {
            if (_hoveredTile != null)
            {
                _hoveredTile.IsHovered = false;
                // Only clear highlight if not selected
                bool isSelected = _selectedTile != null && _selectedCell == _hoveredCell;
                if (!isSelected)
                {
                    worldMap.HighlightTilemap.SetTile(_hoveredCell, null);
                }
                _hoveredTile = null;
                _hoveredCell = default;
                OnTileHoverEnded?.Invoke();
            }
        }

        public void SelectTile(BaseTile tile, Vector3Int cellPos)
        {
            if (_selectedTile == tile) return;

            // Clear previous selection highlight
            if (_selectedTile != null)
            {
                _selectedTile.IsSelected = false;
                worldMap.HighlightTilemap.SetTile(_selectedCell, null);
            }

            _selectedTile = tile;
            _selectedCell = cellPos;
            _selectedTile.IsSelected = true;

            // Set selection highlight
            if (worldMap.SelectedHighlightTile != null)
            {
                worldMap.HighlightTilemap.SetTile(cellPos, worldMap.SelectedHighlightTile);
            }

            OnTileSelected?.Invoke(tile);
        }

        public void Deselect()
        {
            if (_selectedTile != null)
            {
                _selectedTile.IsSelected = false;
                worldMap.HighlightTilemap.SetTile(_selectedCell, null);
                _selectedTile = null;
                _selectedCell = default;
                OnTileDeselected?.Invoke();
            }
        }
    }
}

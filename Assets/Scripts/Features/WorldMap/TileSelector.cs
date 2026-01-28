using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Core.Types;
using System.Collections.Generic;

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

        [Title("Settings")]
        [SerializeField, Tooltip("Offset applied to the raycast world position before converting to cell coordinates. Use this to fix visual misalignment.")]
        private Vector2 selectionOffset;

        private List<Vector3Int> _activeHighlights = new();

        public BaseTile HoveredTile => _hoveredTile;
        public BaseTile SelectedTile => _selectedTile;

        public event Action<BaseTile> OnTileHovered;
        public event Action OnTileHoverEnded;
        public event Action<BaseTile> OnTileSelected;
        public event Action OnTileDeselected;
        
        // Placement Mode
        public bool IsPlacementMode { get; set; }
        public TileType? PlacementTileType { get; set; }
        public event Action<BaseTile> OnPlacementClick;
        public event Action<Vector3Int> OnPlacementCellClick;

        [ShowInInspector, ReadOnly]
        private Vector3Int? _hoveredPlacementCell;

        public Vector3Int? HoveredPlacementCell => _hoveredPlacementCell;

        public bool IsInputBlocked { get; set; }

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
            if (IsInputBlocked)
            {
                ClearHover();
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;
            
            // ... (rest of method same)

            // Note: We might want to show a different hover cursor/highlight in placement mode later,
            // but for now, standard hover is fine.
            
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }

            // Simple ScreenToWorldPoint with Offset
            Vector3 mouseScreenPos = mouse.position.ReadValue();
            
            // Calculate distance from camera to map plane
            // Assuming orthographic camera aligned with Z axis
            float zDistance = worldMap.transform.position.z - _camera.transform.position.z;
            mouseScreenPos.z = Mathf.Abs(zDistance);
            
            Vector3 worldPos = _camera.ScreenToWorldPoint(mouseScreenPos);
            
            // Apply manual offset to correct for visual/logical mismatch
            worldPos += (Vector3)selectionOffset;

            // Convert world position to tilemap cell
            var cellPos = worldMap.WorldToCell(worldPos);

            // Check if this cell has a tile data
            var tile = worldMap.TileData.GetTile(cellPos);

            if (tile != null)
            {
                ClearPlacementHover();
                if (tile != _hoveredTile)
                {
                    SetHoveredTile(tile, cellPos);
                }
                return;
            }

            // In placement mode, check if this is a valid placement cell
            if (IsPlacementMode && worldMap.CanPlaceTile(cellPos))
            {
                ClearHover();
                SetHoveredPlacementCell(cellPos);
                return;
            }

            ClearHover();
            ClearPlacementHover();
        }

        private void HandleClick()
        {
            if (IsInputBlocked) return;

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            // Ignore click if over UI (Optional, but usually good practice. 
            // However, typical Unity UI blocking requires EventSystem check which isn't imported here.
            // Assuming UIToolkit blocks raycasts or handles this elsewhere.)

            if (_hoveredTile != null)
            {
                if (IsPlacementMode)
                {
                    OnPlacementClick?.Invoke(_hoveredTile);
                }
                else
                {
                    SelectTile(_hoveredTile, _hoveredCell);
                }
            }
            else if (IsPlacementMode && _hoveredPlacementCell.HasValue)
            {
                // Click on valid empty cell in placement mode
                OnPlacementCellClick?.Invoke(_hoveredPlacementCell.Value);
            }
            else
            {
                // Clicking empty space deselects
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

            UpdatePowerHighlights();
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

                UpdatePowerHighlights();
                OnTileHoverEnded?.Invoke();
            }
        }

        private void SetHoveredPlacementCell(Vector3Int cellPos)
        {
            if (_hoveredPlacementCell == cellPos) return;

            ClearPlacementHover();
            _hoveredPlacementCell = cellPos;

            // Show actual tile preview (not just highlight)
            if (PlacementTileType.HasValue)
            {
                var previewTile = worldMap.GetTileAsset(PlacementTileType.Value);
                if (previewTile != null)
                {
                    worldMap.Tilemap.SetTile(cellPos, previewTile);
                }
            }
        }

        private void ClearPlacementHover()
        {
            if (_hoveredPlacementCell.HasValue)
            {
                // Only clear if no real tile exists (don't clear if tile was actually placed)
                if (!worldMap.TileData.Contains(_hoveredPlacementCell.Value))
                {
                    worldMap.Tilemap.SetTile(_hoveredPlacementCell.Value, null);
                }
                _hoveredPlacementCell = null;
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

            UpdatePowerHighlights();
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
                
                UpdatePowerHighlights();
                OnTileDeselected?.Invoke();
            }
        }

        private void UpdatePowerHighlights()
        {
            // 1. Clear existing radius highlights
            foreach (var pos in _activeHighlights)
            {
                // Don't clear if it's the currently selected tile or hovered tile
                // (Though strictly speaking, radius highlights shouldn't be on the tile itself usually, 
                // but if they are, we must preserve the main highlights)
                if (pos == _selectedCell && _selectedTile != null) continue;
                if (pos == _hoveredCell && _hoveredTile != null) continue;

                worldMap.HighlightTilemap.SetTile(pos, null);
            }
            _activeHighlights.Clear();

            // 2. Determine source (Hover takes precedence for previewing)
            PowerTile source = null;
            if (_hoveredTile is PowerTile hoveredPower)
            {
                source = hoveredPower;
            }
            else if (_selectedTile is PowerTile selectedPower)
            {
                source = selectedPower;
            }

            if (source == null) return;

            // 3. Set new highlights
            // Ensure calculations are up to date
            source.CalculatePowerOutput();
            var affectedPositions = source.GetPoweredPositions();

            var highlightTile = worldMap.PowerRangeHighlightTile != null ? worldMap.PowerRangeHighlightTile : worldMap.HoverHighlightTile;

            foreach (var pos in affectedPositions)
            {
                // Skip the source tile itself (already highlighted by hover/select)
                if (pos == source.CellPosition) continue;

                // Skip if it conflicts with selection/hover (though usually we want to see the overlap, 
                // but we can't blend tiles easily. Let's prioritize the main state.)
                if (pos == _selectedCell && _selectedTile != null) continue;
                if (pos == _hoveredCell && _hoveredTile != null) continue;

                worldMap.HighlightTilemap.SetTile(pos, highlightTile);
                _activeHighlights.Add(pos);
            }
        }
    }
}

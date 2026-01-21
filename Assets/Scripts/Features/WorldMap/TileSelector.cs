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

        [Title("Settings")]
        [SerializeField]
        private LayerMask tileLayerMask = ~0;

        [Title("State")]
        [ShowInInspector, ReadOnly]
        private Tile _hoveredTile;

        [ShowInInspector, ReadOnly]
        private Tile _selectedTile;

        public Tile HoveredTile => _hoveredTile;
        public Tile SelectedTile => _selectedTile;

        public event Action<Tile> OnTileHovered;
        public event Action<Tile> OnTileSelected;
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
                Debug.LogWarning("TileSelector: Camera is null");
                return;
            }

            var ray = _camera.ScreenPointToRay(mouse.position.ReadValue());

            if (Physics.Raycast(ray, out var hit, 1000f, tileLayerMask))
            {
                var tile = hit.collider.GetComponentInParent<Tile>();
                if (tile != null && tile != _hoveredTile)
                {
                    SetHoveredTile(tile);
                }
                else if (tile == null)
                {
                    Debug.LogWarning($"Hit object has no Tile component: {hit.collider.gameObject.name}");
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
                SelectTile(_hoveredTile);
            }
            else
            {
                Deselect();
            }
        }

        private void SetHoveredTile(Tile tile)
        {
            if (_hoveredTile != null)
            {
                _hoveredTile.SetHovered(false);
            }

            _hoveredTile = tile;
            _hoveredTile.SetHovered(true);
            OnTileHovered?.Invoke(tile);
        }

        private void ClearHover()
        {
            if (_hoveredTile != null)
            {
                _hoveredTile.SetHovered(false);
                _hoveredTile = null;
            }
        }

        public void SelectTile(Tile tile)
        {
            if (_selectedTile == tile) return;

            if (_selectedTile != null)
            {
                _selectedTile.SetSelected(false);
            }

            _selectedTile = tile;
            _selectedTile.SetSelected(true);
            OnTileSelected?.Invoke(tile);
        }

        public void Deselect()
        {
            if (_selectedTile != null)
            {
                _selectedTile.SetSelected(false);
                _selectedTile = null;
                OnTileDeselected?.Invoke();
            }
        }
    }
}

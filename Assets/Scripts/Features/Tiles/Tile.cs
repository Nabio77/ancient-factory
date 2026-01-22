using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Grid;
using CarbonWorld.Features.Inventories;

namespace CarbonWorld.Features.Tiles
{
    public class Tile : MonoBehaviour
    {
        [Title("Highlight")]
        [SerializeField]
        private SpriteRenderer highlightSprite;

        [SerializeField]
        private Color hoverColor = new Color(1f, 1f, 1f, 0.3f);

        [SerializeField]
        private Color selectedColor = new Color(1f, 1f, 1f, 0.5f);

        private bool _isHovered;
        private bool _isSelected;

        private readonly Inventory _inventory = new();

        public HexCoord Coordinates { get; private set; }
        public bool IsHovered => _isHovered;
        public bool IsSelected => _isSelected;
        public Inventory Inventory => _inventory;

        public virtual void Initialize(HexCoord coord)
        {
            Coordinates = coord;
        }

        public virtual void SetHovered(bool hovered)
        {
            _isHovered = hovered;
            UpdateVisuals();
        }

        public virtual void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisuals();
        }

        protected virtual void UpdateVisuals()
        {
            if (highlightSprite == null) return;

            if (_isSelected)
            {
                highlightSprite.enabled = true;
                highlightSprite.color = selectedColor;
            }
            else if (_isHovered)
            {
                highlightSprite.enabled = true;
                highlightSprite.color = hoverColor;
            }
            else
            {
                highlightSprite.enabled = false;
            }
        }
    }
}

using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Grid;

namespace CarbonWorld.Features.Tiles
{
    public class Tile : MonoBehaviour
    {
        private bool _isHovered;
        private bool _isSelected;

        public HexCoord Coordinates { get; private set; }

        public virtual void Initialize(HexCoord coord)
        {
            Coordinates = coord;
        }

        public virtual void SetHovered(bool hovered)
        {
            _isHovered = hovered;
        }

        public virtual void SetSelected(bool selected)
        {
            _isSelected = selected;
        }
    }
}

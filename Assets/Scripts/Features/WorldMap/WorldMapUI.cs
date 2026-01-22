using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.Grid;
using CarbonWorld.Features.Tiles;

namespace CarbonWorld.Features.WorldMap
{
    public class WorldMapUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required]
        private UIDocument uiDocument;

        [SerializeField, Required]
        private TileSelector tileSelector;

        [SerializeField, Required]
        private WorldMap worldMap;

        private VisualElement _tileInfoPanel;
        private Label _tileType;
        private Label _tileCoords;
        private Label _tileNeighbors;

        private VisualElement _resourceInfo;
        private VisualElement _resourceIcon;
        private Label _resourceName;
        private Label _resourceTier;
        private Label _resourceOutput;

        void Awake()
        {
            var root = uiDocument.rootVisualElement;
            _tileInfoPanel = root.Q<VisualElement>("tile-info-panel");
            _tileType = root.Q<Label>("tile-type");
            _tileCoords = root.Q<Label>("tile-coords");
            _tileNeighbors = root.Q<Label>("tile-neighbors");

            _resourceInfo = root.Q<VisualElement>("resource-info");
            _resourceIcon = root.Q<VisualElement>("resource-icon");
            _resourceName = root.Q<Label>("resource-name");
            _resourceTier = root.Q<Label>("resource-tier");
            _resourceOutput = root.Q<Label>("resource-output");
        }

        void OnEnable()
        {
            tileSelector.OnTileSelected += OnTileSelected;
            tileSelector.OnTileDeselected += OnTileDeselected;
        }

        void OnDisable()
        {
            tileSelector.OnTileSelected -= OnTileSelected;
            tileSelector.OnTileDeselected -= OnTileDeselected;
        }

        private void OnTileSelected(Tile tile)
        {
            _tileType.text = tile.GetType().Name.Replace("Tile", " Tile");
            _tileCoords.text = $"({tile.Coordinates.Q}, {tile.Coordinates.R})";
            _tileNeighbors.text = GetNeighborsText(tile.Coordinates);

            if (tile is ResourceTile resourceTile)
            {
                ShowResourceInfo(resourceTile);
            }
            else
            {
                _resourceInfo.AddToClassList("hidden");
            }

            _tileInfoPanel.RemoveFromClassList("hidden");
        }

        private void OnTileDeselected()
        {
            _tileInfoPanel.AddToClassList("hidden");
        }

        private void ShowResourceInfo(ResourceTile tile)
        {
            var item = tile.ResourceItem;
            if (item == null)
            {
                _resourceInfo.AddToClassList("hidden");
                return;
            }

            _resourceName.text = item.ItemName;
            _resourceTier.text = $"Tier {item.Tier} - {item.Category}";
            _resourceOutput.text = tile.OutputPerTick.ToString();

            // Clear and set the icon
            _resourceIcon.style.backgroundImage = StyleKeyword.None;
            if (item.Icon != null)
            {
                _resourceIcon.style.backgroundImage = new StyleBackground(item.Icon);
            }

            _resourceInfo.RemoveFromClassList("hidden");
        }

        private string GetNeighborsText(HexCoord coord)
        {
            var neighbors = HexUtils.GetNeighbors(coord);
            var sb = new StringBuilder();

            foreach (var neighborCoord in neighbors)
            {
                var neighborTile = worldMap.Grid.GetTile(neighborCoord);
                if (neighborTile != null)
                {
                    var typeName = neighborTile.GetType().Name.Replace("Tile", "");
                    sb.AppendLine($"({neighborCoord.Q}, {neighborCoord.R}) - {typeName}");
                }
            }

            return sb.Length > 0 ? sb.ToString().TrimEnd() : "None";
        }
    }
}

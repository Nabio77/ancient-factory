using System.Text;
using System.Linq;
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

        private VisualElement _productionInfo;
        private Label _productionInputs;
        private Label _productionOutputs;

        private VisualElement _powerInfo;
        private Label _powerOutput;
        private Label _powerRadius;

        private VisualElement _transportInfo;
        private Label _transportOutputs;

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

            _productionInfo = root.Q<VisualElement>("production-info");
            _productionInputs = root.Q<Label>("production-inputs");
            _productionOutputs = root.Q<Label>("production-outputs");

            _powerInfo = root.Q<VisualElement>("power-info");
            _powerOutput = root.Q<Label>("power-output");
            _powerRadius = root.Q<Label>("power-radius");

            _transportInfo = root.Q<VisualElement>("transport-info");
            _transportOutputs = root.Q<Label>("transport-outputs");
        }

        void OnEnable()
        {
            tileSelector.OnTileSelected += OnTileSelected;
            tileSelector.OnTileDeselected += OnTileDeselected;
            tileSelector.OnTileHovered += OnTileHovered;
            tileSelector.OnTileHoverEnded += OnTileHoverEnded;
        }

        void OnDisable()
        {
            tileSelector.OnTileSelected -= OnTileSelected;
            tileSelector.OnTileDeselected -= OnTileDeselected;
            tileSelector.OnTileHovered -= OnTileHovered;
            tileSelector.OnTileHoverEnded -= OnTileHoverEnded;
        }

        private void OnTileHovered(BaseTile tile)
        {
            UpdateUI(tile);
        }

        private void OnTileHoverEnded()
        {
            if (tileSelector.SelectedTile != null)
            {
                UpdateUI(tileSelector.SelectedTile);
            }
            else
            {
                _tileInfoPanel.AddToClassList("hidden");
            }
        }

        private void OnTileSelected(BaseTile tile)
        {
            UpdateUI(tile);
        }

        private void OnTileDeselected()
        {
            if (tileSelector.HoveredTile != null)
            {
                UpdateUI(tileSelector.HoveredTile);
            }
            else
            {
                _tileInfoPanel.AddToClassList("hidden");
            }
        }

        private void UpdateUI(BaseTile tile)
        {
            _tileType.text = $"{tile.Type} Tile";
            _tileCoords.text = $"({tile.CellPosition.x}, {tile.CellPosition.y})";
            _tileNeighbors.text = GetNeighborsText(tile.CellPosition);

            if (tile is ResourceTile resourceTile)
            {
                ShowResourceInfo(resourceTile);
            }
            else
            {
                _resourceInfo?.AddToClassList("hidden");
            }

            if (tile is ProductionTile productionTile)
            {
                ShowProductionInfo(productionTile);
            }
            else
            {
                _productionInfo?.AddToClassList("hidden");
            }

            if (tile is PowerTile powerTile)
            {
                ShowPowerInfo(powerTile);
            }
            else
            {
                _powerInfo?.AddToClassList("hidden");
            }

            if (tile is TransportTile transportTile)
            {
                ShowTransportInfo(transportTile);
            }
            else
            {
                _transportInfo?.AddToClassList("hidden");
            }

            _tileInfoPanel.RemoveFromClassList("hidden");
        }

        private void ShowTransportInfo(TransportTile tile)
        {
            if (_transportInfo == null) return;

            // Ensure IO nodes are up to date
            if (worldMap != null && worldMap.GraphSystem != null)
            {
                worldMap.GraphSystem.UpdateTile(worldMap.TileData, tile);
            }

            var outputs = tile.GetOutputs()
                .Select(stack => $"{stack.Item.ItemName} (Tier {stack.Item.Tier})")
                .Distinct()
                .ToList();

            if (_transportOutputs != null)
            {
                _transportOutputs.text = outputs.Any() ? string.Join("\n", outputs) : "None";
            }

            _transportInfo.RemoveFromClassList("hidden");
        }

        private void ShowPowerInfo(PowerTile tile)
        {
            // Ensure calculation is up to date
            tile.CalculatePowerOutput();

            _powerOutput.text = tile.TotalPowerOutput.ToString();
            _powerRadius.text = tile.EffectiveRadius.ToString();

            _powerInfo.RemoveFromClassList("hidden");
        }

        private void ShowProductionInfo(ProductionTile tile)
        {
            // Ensure IO nodes are up to date
            if (worldMap != null && worldMap.GraphSystem != null)
            {
                worldMap.GraphSystem.UpdateTile(worldMap.TileData, tile);
            }

            var inputs = tile.Graph.ioNodes
                .Where(n => n.type == TileIOType.Input && n.availableItem.IsValid)
                .Select(n => $"{n.availableItem.Item.ItemName} (Tier {n.availableItem.Item.Tier})")
                .ToList();

            var outputs = tile.Graph.ioNodes
                .Where(n => n.type == TileIOType.Output && n.availableItem.IsValid)
                .Select(n => $"{n.availableItem.Item.ItemName} (Tier {n.availableItem.Item.Tier})")
                .ToList();

            _productionInputs.text = inputs.Any() ? string.Join("\n", inputs) : "None";
            _productionOutputs.text = outputs.Any() ? string.Join("\n", outputs) : "None";
            
            _productionInfo.RemoveFromClassList("hidden");
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

        private string GetNeighborsText(Vector3Int cellPos)
        {
            var neighbors = HexUtils.GetNeighbors(cellPos);
            var sb = new StringBuilder();

            foreach (var neighborPos in neighbors)
            {
                var neighborTile = worldMap.TileData.GetTile(neighborPos);
                if (neighborTile != null)
                {
                    sb.AppendLine($"({neighborPos.x}, {neighborPos.y}) - {neighborTile.Type}");
                }
            }

            return sb.Length > 0 ? sb.ToString().TrimEnd() : "None";
        }
    }
}

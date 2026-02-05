using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Systems;
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
        private Label _powerUsed;
        private Label _powerRadius;

        private VisualElement _transportInfo;
        private Label _transportOutputs;

        private VisualElement _settlementInfo;
        private Label _settlementStatus;
        private VisualElement _settlementDemandsContainer;

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
            _powerUsed = root.Q<Label>("power-used");
            _powerRadius = root.Q<Label>("power-radius");

            _transportInfo = root.Q<VisualElement>("transport-info");
            _transportOutputs = root.Q<Label>("transport-outputs");

            _settlementInfo = root.Q<VisualElement>("settlement-info");
            _settlementStatus = root.Q<Label>("settlement-status");
            _settlementDemandsContainer = root.Q<VisualElement>("settlement-demands");
        }

        void OnEnable()
        {
            tileSelector.OnTileSelected += OnTileSelected;
            tileSelector.OnTileDeselected += OnTileDeselected;
            tileSelector.OnTileHovered += OnTileHovered;
            tileSelector.OnTileHoverEnded += OnTileHoverEnded;

            if (InterfaceSystem.Instance != null)
                InterfaceSystem.Instance.OnStateChanged += OnInterfaceStateChanged;
        }

        void OnDisable()
        {
            tileSelector.OnTileSelected -= OnTileSelected;
            tileSelector.OnTileDeselected -= OnTileDeselected;
            tileSelector.OnTileHovered -= OnTileHovered;
            tileSelector.OnTileHoverEnded -= OnTileHoverEnded;

            if (InterfaceSystem.Instance != null)
                InterfaceSystem.Instance.OnStateChanged -= OnInterfaceStateChanged;
        }

        private void OnInterfaceStateChanged(InterfaceState newState)
        {
            if (newState != InterfaceState.Gameplay)
            {
                _tileInfoPanel.AddToClassList("hidden");
            }
        }

        void Update()
        {
            // Don't update UI when not in Gameplay state
            if (InterfaceSystem.Instance != null && !InterfaceSystem.Instance.IsState(InterfaceState.Gameplay))
                return;

            if (tileSelector.SelectedTile != null)
            {
                UpdateUI(tileSelector.SelectedTile);
            }
            else if (tileSelector.HoveredTile != null)
            {
                UpdateUI(tileSelector.HoveredTile);
            }
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

            if (tile is FactoryTile factoryTile)
            {
                ShowProductionInfo(factoryTile);
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

            if (tile is SettlementTile settlementTile)
            {
                // Handled by SettlementUI
                _settlementInfo?.AddToClassList("hidden");
            }
            else
            {
                _settlementInfo?.AddToClassList("hidden");
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

        private void ShowSettlementInfo(SettlementTile tile)
        {
            if (_settlementInfo == null) return;

            // Update status
            if (_settlementStatus != null)
            {
                _settlementStatus.text = tile.IsSatisfied ? "Satisfied" : "Needs Supplies";
                _settlementStatus.RemoveFromClassList("satisfied");
                _settlementStatus.RemoveFromClassList("unsatisfied");
                _settlementStatus.AddToClassList(tile.IsSatisfied ? "satisfied" : "unsatisfied");
            }

            // Update demands list
            if (_settlementDemandsContainer != null)
            {
                _settlementDemandsContainer.Clear();

                foreach (var demand in tile.Demands)
                {
                    if (!demand.IsValid) continue;

                    int current = tile.Inventory.Get(demand.Item);
                    int needed = demand.Amount;
                    bool isFulfilled = current >= needed;

                    var demandRow = new VisualElement();
                    demandRow.AddToClassList("demand-row");
                    if (isFulfilled)
                    {
                        demandRow.AddToClassList("fulfilled");
                    }

                    var nameLabel = new Label($"{demand.Item.ItemName} (T{demand.Item.Tier})");
                    nameLabel.AddToClassList("demand-name");

                    var progressLabel = new Label($"{current}/{needed}");
                    progressLabel.AddToClassList("demand-progress");

                    demandRow.Add(nameLabel);
                    demandRow.Add(progressLabel);
                    _settlementDemandsContainer.Add(demandRow);
                }
            }

            _settlementInfo.RemoveFromClassList("hidden");
        }

        private void ShowPowerInfo(PowerTile tile)
        {
            // Ensure calculation is up to date
            tile.CalculatePowerOutput();

            _powerOutput.text = tile.TotalPowerOutput.ToString();
            _powerUsed.text = tile.TotalPowerConsumption.ToString();
            _powerRadius.text = tile.EffectiveRadius.ToString();

            _powerInfo.RemoveFromClassList("hidden");
        }

        private void ShowProductionInfo(FactoryTile tile)
        {
            // Ensure IO nodes are up to date
            if (worldMap != null && worldMap.GraphSystem != null)
            {
                worldMap.GraphSystem.UpdateTile(worldMap.TileData, tile);
            }

            // Show power status
            var powerStatus = tile.IsPowered ? "POWERED" : "NO POWER";

            // Show available inputs from IO nodes
            var inputs = tile.Graph.ioNodes
                .Where(n => n.type == TileIOType.Input && n.availableItem.IsValid)
                .Select(n => $"{n.availableItem.Item.ItemName} x{n.availableItem.Amount}")
                .ToList();

            // Show potential outputs (from graph) and actual outputs (from buffer)
            var potentialOutputs = tile.Graph.ioNodes
                .Where(n => n.type == TileIOType.Output && n.availableItem.IsValid)
                .Select(n => $"[Potential] {n.availableItem.Item.ItemName}")
                .ToList();

            var actualOutputs = tile.OutputBuffer.GetAll()
                .Where(s => s.IsValid)
                .Select(s => $"[Ready] {s.Item.ItemName} x{s.Amount}")
                .ToList();

            // Show production states
            var productionStates = tile.GetAllProductionStates()
                .Select(s => $"{s.Status}: {s.Progress:P0}")
                .ToList();

            var inputText = new System.Text.StringBuilder();
            inputText.AppendLine($"Power: {powerStatus}");
            if (inputs.Any()) inputText.AppendLine(string.Join("\n", inputs));
            if (!inputs.Any()) inputText.AppendLine("No inputs");

            var outputText = new System.Text.StringBuilder();
            if (potentialOutputs.Any()) outputText.AppendLine(string.Join("\n", potentialOutputs));
            if (actualOutputs.Any()) outputText.AppendLine(string.Join("\n", actualOutputs));
            if (productionStates.Any()) outputText.AppendLine(string.Join("\n", productionStates));
            if (!potentialOutputs.Any() && !actualOutputs.Any()) outputText.AppendLine("None");

            _productionInputs.text = inputText.ToString().TrimEnd();
            _productionOutputs.text = outputText.ToString().TrimEnd();

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

            // Show quality, output rate, and current inventory
            // int currentStock = tile.Inventory.Get(item); // Inventory is output buffer
            _resourceOutput.text = $"{tile.Quality} ({tile.GetOutputPerTick()}/tick)\nReserves: {tile.CurrentAmount}/{tile.MaxAmount}";
            if (tile.IsDepleted)
            {
                _resourceOutput.style.color = new StyleColor(Color.red);
                _resourceOutput.text += " (DEPLETED)";
            }
            else
            {
                _resourceOutput.style.color = new StyleColor(Color.white); // Accessing default style might be better but hardcoded for now
            }

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

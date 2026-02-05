using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Types;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.WorldMap;
using CarbonWorld.Features.Inventories;

namespace CarbonWorld.Core.Systems
{
    public class FactorySystem : MonoBehaviour
    {
        public static FactorySystem Instance { get; private set; }

        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [Title("Settings")]
        [SerializeField]
        private float tickInterval = 1f;

        private float _tickTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (worldMap == null)
            {
                worldMap = FindFirstObjectByType<WorldMap>();
            }
        }

        private void Update()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickInterval)
            {
                _tickTimer = 0f;
                ProcessFactoryTick();
            }
        }

        [Button("Process Tick")]
        public void ProcessFactoryTick()
        {
            // Phase 0: Process power tile fuel consumption
            ProcessPowerTiles();

            // Phase 1: Collect inputs from adjacent tiles
            CollectInputs();

            // Phase 2: Process production for each factory tile
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is IFactoryTile and not PowerTile)
                {
                    ProcessFactoryTile(tile);
                }
                else if (tile is ResourceTile resourceTile)
                {
                    ProcessResourceTile(resourceTile);
                }
            }

            // Phase 3: Distribute outputs to settlements
            DistributeToSettlements();

            // Phase 4: Recalculate power grid
            PowerSystem.Instance?.RecalculatePower();
        }

        private void ProcessPowerTiles()
        {
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is PowerTile powerTile)
                {
                    // Collect fuel inputs for power tile
                    CollectInputsForTile(powerTile);

                    // Process each generator blueprint
                    foreach (var node in powerTile.Graph.nodes)
                    {
                        if (node.blueprint == null || !node.blueprint.IsPowerGenerator)
                            continue;

                        // Solar (no inputs) is always active
                        if (node.blueprint.Inputs.Count == 0)
                        {
                            powerTile.SetGeneratorActive(node.id, true);
                            continue;
                        }

                        // Check if we have fuel in buffer
                        bool hasFuel = true;
                        foreach (var input in node.blueprint.Inputs)
                        {
                            if (!powerTile.InputBuffer.Has(input.Item, input.Amount))
                            {
                                hasFuel = false;
                                break;
                            }
                        }

                        if (hasFuel)
                        {
                            // Consume fuel
                            foreach (var input in node.blueprint.Inputs)
                            {
                                powerTile.InputBuffer.Remove(input.Item, input.Amount);
                            }
                            powerTile.SetGeneratorActive(node.id, true);
                        }
                        else
                        {
                            powerTile.SetGeneratorActive(node.id, false);
                        }
                    }
                }
            }
        }

        [Button("Debug: Show All Production Status")]
        public void DebugShowProductionStatus()
        {
            Debug.Log("=== PRODUCTION SYSTEM DEBUG ===");

            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is ProductionTile productionTile)
                {
                    var pos = productionTile.CellPosition;
                    Debug.Log($"ProductionTile at {pos}:");
                    Debug.Log($"  - IsPowered: {productionTile.IsPowered}");
                    Debug.Log($"  - IO Nodes: {productionTile.Graph.ioNodes.Count}");
                    Debug.Log($"  - Blueprint Nodes: {productionTile.Graph.nodes.Count}");
                    Debug.Log($"  - Connections: {productionTile.Graph.connections.Count}");

                    foreach (var io in productionTile.Graph.ioNodes)
                    {
                        Debug.Log($"    IO: {io.type} - {io.id} - Item: {(io.availableItem.IsValid ? io.availableItem.Item.ItemName : "None")}");
                    }

                    Debug.Log($"  - InputBuffer: {productionTile.InputBuffer.TotalItemCount} items");
                    Debug.Log($"  - OutputBuffer: {productionTile.OutputBuffer.TotalItemCount} items");

                    foreach (var state in productionTile.GetAllProductionStates())
                    {
                        Debug.Log($"    State: {state.NodeId} - {state.Status} ({state.Progress:P0})");
                    }
                }
                else if (tile is FoodTile foodTile)
                {
                    var pos = foodTile.CellPosition;
                    Debug.Log($"FoodTile at {pos}:");
                    Debug.Log($"  - IsPowered: {foodTile.IsPowered}");
                    Debug.Log($"  - IO Nodes: {foodTile.Graph.ioNodes.Count}");
                    Debug.Log($"  - Blueprint Nodes: {foodTile.Graph.nodes.Count}");
                    Debug.Log($"  - Connections: {foodTile.Graph.connections.Count}");

                    foreach (var io in foodTile.Graph.ioNodes)
                    {
                        Debug.Log($"    IO: {io.type} - {io.id} - Item: {(io.availableItem.IsValid ? io.availableItem.Item.ItemName : "None")}");
                    }

                    Debug.Log($"  - InputBuffer: {foodTile.InputBuffer.TotalItemCount} items");
                    Debug.Log($"  - OutputBuffer: {foodTile.OutputBuffer.TotalItemCount} items");

                    foreach (var state in foodTile.GetAllProductionStates())
                    {
                        Debug.Log($"    State: {state.NodeId} - {state.Status} ({state.Progress:P0})");
                    }
                }
                else if (tile is ResourceTile resourceTile)
                {
                    var pos = resourceTile.CellPosition;
                    var output = resourceTile.GetOutput();
                    var stock = resourceTile.Inventory.Get(resourceTile.ResourceItem);
                    Debug.Log($"ResourceTile at {pos}: {resourceTile.Quality} {output.Item?.ItemName ?? "None"} - Stock: {stock}");
                }
            }
        }

        private void CollectInputs()
        {
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is IFactoryTile and not PowerTile)
                {
                    CollectInputsForTile(tile);
                }
            }
        }

        private void CollectInputsForTile(BaseTile tile)
        {
            if (tile is not IFactoryTile factoryTile) return;

            var inputBuffer = factoryTile.InputBuffer;
            if (inputBuffer == null) return;

            // For each input IO node that has a valid item
            foreach (var ioNode in factoryTile.Graph.ioNodes)
            {
                if (ioNode.type != TileIOType.Input || !ioNode.availableItem.IsValid)
                    continue;

                // Check if this input is connected to any blueprint node
                var isConnected = factoryTile.Graph.connections.Any(c => c.fromNodeId == ioNode.id);
                if (!isConnected)
                    continue;

                // Get the source tile
                var sourceTile = worldMap.TileData.GetTile(ioNode.sourceTilePosition);
                if (sourceTile == null)
                    continue;

                // If source is transport, we need to trace back to the original source to actually consume the item
                var actualSourceTile = sourceTile;
                if (sourceTile is TransportTile)
                {
                    actualSourceTile = worldMap.TileData.GetTile(ioNode.originalSourcePosition);
                }

                if (actualSourceTile == null)
                    continue;

                var item = ioNode.availableItem.Item;
                var requestedAmount = ioNode.availableItem.Amount;
                int consumedAmount = 0;

                // Check source based on tile type
                if (actualSourceTile is ProductionTile sourceProduction)
                {
                    // Consume from OutputBuffer for production tiles
                    int available = sourceProduction.OutputBuffer.Get(item);
                    consumedAmount = Mathf.Min(requestedAmount, available);
                    if (consumedAmount > 0)
                    {
                        sourceProduction.OutputBuffer.Remove(item, consumedAmount);
                    }
                }
                else if (actualSourceTile is FoodTile sourceFood)
                {
                    // Consume from OutputBuffer for food tiles
                    int available = sourceFood.OutputBuffer.Get(item);
                    consumedAmount = Mathf.Min(requestedAmount, available);
                    if (consumedAmount > 0)
                    {
                        sourceFood.OutputBuffer.Remove(item, consumedAmount);
                    }
                }
                else
                {
                    // Consume from Inventory for resource/other tiles
                    int available = actualSourceTile.Inventory.Get(item);
                    consumedAmount = Mathf.Min(requestedAmount, available);
                    if (consumedAmount > 0)
                    {
                        actualSourceTile.Inventory.Remove(item, consumedAmount);
                    }
                }

                if (consumedAmount > 0)
                {
                    // Add to input buffer
                    inputBuffer.Add(item, consumedAmount);
                }
            }
        }



        private void ProcessFactoryTile(BaseTile tile)
        {
            if (tile is not IFactoryTile factoryTile) return;

            if (!factoryTile.IsPowered)
                return;

            // Process each blueprint node
            foreach (var node in factoryTile.Graph.nodes)
            {
                if (node.blueprint == null || !node.blueprint.IsProducer)
                    continue;

                ProcessBlueprintNode(tile, factoryTile, node);
            }
        }

        private void ProcessResourceTile(ResourceTile tile)
        {
            if (tile.ResourceItem == null) return;

            int maxBuffer = 20;
            int currentCount = tile.Inventory.Get(tile.ResourceItem);

            if (currentCount < maxBuffer)
            {
                var output = tile.GetOutput();
                if (output.IsValid)
                {
                    tile.Inventory.Add(output.Item, output.Amount);
                }
            }
        }

        private void ProcessBlueprintNode(BaseTile tile, IFactoryTile factoryTile, BlueprintNode node)
        {
            var state = factoryTile.GetProductionState(node.id);
            if (state == null) return;
            var blueprint = node.blueprint;

            switch (state.Status)
            {
                case ProductionStatus.Idle:
                    TryStartProduction(tile, node, state);
                    break;

                case ProductionStatus.Producing:
                    UpdateProduction(state);
                    break;

                case ProductionStatus.OutputReady:
                    TryTransferOutput(tile, node, state);
                    break;
            }
        }

        private void TryStartProduction(BaseTile tile, BlueprintNode node, BlueprintProductionState state)
        {
            if (tile is not IFactoryTile factoryTile) return;
            var blueprint = node.blueprint;

            if (!AreInputsSatisfied(factoryTile, node))
                return;

            var inputBuffer = factoryTile.InputBuffer;
            if (inputBuffer == null) return;

            // Consume inputs from InputBuffer
            foreach (var input in blueprint.Inputs)
            {
                inputBuffer.Remove(input.Item, input.Amount);
            }

            // Start production
            state.Status = ProductionStatus.Producing;
            state.ElapsedTime = 0f;
            state.TotalTime = blueprint.ProductionTime;
            state.PendingOutput = blueprint.Output;
        }

        private bool AreInputsSatisfied(IFactoryTile factoryTile, BlueprintNode node)
        {
            var inputBuffer = factoryTile.InputBuffer;
            if (inputBuffer == null) return false;

            var blueprint = node.blueprint;

            // Find connections TO this node
            var incomingConnections = factoryTile.Graph.connections
                .Where(c => c.toNodeId == node.id)
                .ToList();

            foreach (var requiredInput in blueprint.Inputs)
            {
                bool inputFound = false;

                foreach (var conn in incomingConnections)
                {
                    // Check if connection is from an IO node
                    var ioNode = factoryTile.Graph.GetIONode(conn.fromNodeId);
                    if (ioNode != null)
                    {
                        if (ioNode.availableItem.Item == requiredInput.Item)
                        {
                            if (inputBuffer.Has(requiredInput.Item, requiredInput.Amount))
                            {
                                inputFound = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        var sourceNode = factoryTile.Graph.GetNode(conn.fromNodeId);
                        if (sourceNode != null)
                        {
                            if (sourceNode.blueprint.Output.Item == requiredInput.Item)
                            {
                                if (inputBuffer.Has(requiredInput.Item, requiredInput.Amount))
                                {
                                    inputFound = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!inputFound)
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateProduction(BlueprintProductionState state)
        {
            state.ElapsedTime += tickInterval;

            if (state.ElapsedTime >= state.TotalTime)
            {
                state.Status = ProductionStatus.OutputReady;
            }
        }

        private CoreTile _coreTile;

        private CoreTile GetCoreTile()
        {
            if (_coreTile == null)
            {
                _coreTile = worldMap.TileData.GetAllTiles().OfType<CoreTile>().FirstOrDefault();
            }
            return _coreTile;
        }

        private void TryTransferOutput(BaseTile tile, BlueprintNode node, BlueprintProductionState state)
        {
            if (tile is not IFactoryTile factoryTile) return;
            var outputBuffer = factoryTile.OutputBuffer;
            if (outputBuffer == null) return;

            // Check for connection to either Output or Core
            // I will update the query to find ANY valid connection (Output, Core, or another Machine)

            var connection = factoryTile.Graph.connections
                .FirstOrDefault(c => c.fromNodeId == node.id);

            if (connection != null && state.PendingOutput.IsValid)
            {
                var ioNode = factoryTile.Graph.GetIONode(connection.toNodeId);

                if (ioNode != null)
                {
                    if (ioNode.type == TileIOType.Output)
                    {
                        outputBuffer.Add(state.PendingOutput.Item, state.PendingOutput.Amount);
                        state.PendingOutput = ItemStack.Empty;
                        state.Status = ProductionStatus.Idle;
                    }
                    else if (ioNode.type == TileIOType.Core)
                    {
                        var core = GetCoreTile();
                        if (core != null)
                        {
                            // Use item points
                            int points = state.PendingOutput.Item.TechPoints * state.PendingOutput.Amount;
                            core.AddPoints(points, state.PendingOutput.Item, state.PendingOutput.Amount);
                        }
                        else
                        {
                            Debug.LogWarning("[FactorySystem] CoreTile not found!");
                        }

                        state.PendingOutput = ItemStack.Empty;
                        state.Status = ProductionStatus.Idle;
                    }
                }
                else
                {
                    var targetNode = factoryTile.Graph.GetNode(connection.toNodeId);
                    if (targetNode != null)
                    {
                        var targetInputBuffer = factoryTile.InputBuffer;
                        if (targetInputBuffer != null)
                        {
                            targetInputBuffer.Add(state.PendingOutput.Item, state.PendingOutput.Amount);
                            state.PendingOutput = ItemStack.Empty;
                            state.Status = ProductionStatus.Idle;
                        }
                    }
                }
            }
        }

        // --- DUPLICATED LOGIC FOR FOOD TILE (AS REQUESTED) ---

        // ----------------------------------------------------

        private void DistributeToSettlements()
        {
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is SettlementTile settlement)
                {
                    DistributeToSettlement(settlement);
                }
            }
        }

        private void DistributeToSettlement(SettlementTile settlement)
        {
            var neighbors = worldMap.TileData.GetNeighbors(settlement.CellPosition);

            foreach (var neighbor in neighbors)
            {
                if (neighbor is IFactoryTile factory and not PowerTile)
                {
                    TransferItemsToSettlement(factory.OutputBuffer, settlement);
                }
                else if (neighbor is TransportTile sourceTrans)
                {
                    TransferItemsToSettlementFromTransport(sourceTrans, settlement);
                }
            }
        }

        private void TransferItemsToSettlement(Inventory sourceBuffer, SettlementTile settlement)
        {
            // Get all items in the source buffer
            var items = sourceBuffer.GetAll().ToList();

            foreach (var stack in items)
            {
                if (!stack.IsValid)
                    continue;

                // Check if settlement has demand for this item
                var demand = settlement.Demands.FirstOrDefault(d => d.Item == stack.Item);
                if (!demand.IsValid)
                    continue;

                // Calculate how much is needed
                int currentAmount = settlement.Inventory.Get(stack.Item);
                int needed = demand.Amount - currentAmount;
                if (needed <= 0)
                    continue;

                // Transfer min(needed, available)
                int toTransfer = Mathf.Min(needed, stack.Amount);
                sourceBuffer.Remove(stack.Item, toTransfer);
                settlement.Inventory.Add(stack.Item, toTransfer);
            }
        }

        private void TransferItemsToSettlementFromTransport(TransportTile transport, SettlementTile settlement)
        {
            // For each input of the transport tile, check if it can supply the settlement
            foreach (var node in transport.Graph.ioNodes)
            {
                if (node.type != TileIOType.Input || !node.availableItem.IsValid)
                    continue;

                var item = node.availableItem.Item;

                // Check demand
                var demand = settlement.Demands.FirstOrDefault(d => d.Item == item);
                if (!demand.IsValid) continue;

                int currentAmount = settlement.Inventory.Get(item);
                int needed = demand.Amount - currentAmount;
                if (needed <= 0) continue;

                // Pull from original source
                var sourceTile = worldMap.TileData.GetTile(node.originalSourcePosition);
                if (sourceTile == null) continue;

                int toTransfer = 0;

                if (sourceTile is IFactoryTile factory)
                {
                    int available = factory.OutputBuffer.Get(item);
                    toTransfer = Mathf.Min(needed, Mathf.Min(node.availableItem.Amount, available));
                    if (toTransfer > 0)
                    {
                        factory.OutputBuffer.Remove(item, toTransfer);
                    }
                }
                else
                {
                    int available = sourceTile.Inventory.Get(item);
                    toTransfer = Mathf.Min(needed, Mathf.Min(node.availableItem.Amount, available));
                    if (toTransfer > 0)
                    {
                        sourceTile.Inventory.Remove(item, toTransfer);
                    }
                }

                if (toTransfer > 0)
                {
                    settlement.Inventory.Add(item, toTransfer);
                }
            }
        }
    }
}

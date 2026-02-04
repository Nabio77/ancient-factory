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
    public class ProductionSystem : MonoBehaviour
    {
        public static ProductionSystem Instance { get; private set; }

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
                ProcessProductionTick();
            }
        }

        [Button("Process Tick")]
        public void ProcessProductionTick()
        {
            // Phase 1: Collect inputs from adjacent tiles
            CollectInputs();

            // Phase 2: Process production for each tile
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is ProductionTile || tile is FoodTile)
                {
                    ProcessProductionTile(tile);
                }
                else if (tile is ResourceTile resourceTile)
                {
                    ProcessResourceTile(resourceTile);
                }
            }

            // Phase 3: Distribute outputs to settlements
            DistributeToSettlements();
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
                if (tile is ProductionTile || tile is FoodTile)
                {
                    CollectInputsForTile(tile);
                }
            }
        }

        private void CollectInputsForTile(BaseTile tile)
        {
            if (tile is not IGraphTile graphTile) return;

            Inventory inputBuffer = tile is ProductionTile p ? p.InputBuffer : (tile is FoodTile f ? f.InputBuffer : null);
            if (inputBuffer == null) return;

            // For each input IO node that has a valid item
            foreach (var ioNode in graphTile.Graph.ioNodes)
            {
                if (ioNode.type != TileIOType.Input || !ioNode.availableItem.IsValid)
                    continue;

                // Check if this input is connected to any blueprint node
                var isConnected = graphTile.Graph.connections.Any(c => c.fromNodeId == ioNode.id);
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



        private void ProcessProductionTile(BaseTile tile)
        {
            if (tile is not IGraphTile graphTile) return;

            bool isPowered = tile is ProductionTile p ? p.IsPowered : (tile is FoodTile f && f.IsPowered);

            if (!isPowered)
                return;

            // Process each blueprint node
            foreach (var node in graphTile.Graph.nodes)
            {
                if (node.blueprint == null || !node.blueprint.IsProducer)
                    continue;

                ProcessBlueprintNode(tile, node);
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

        private void ProcessBlueprintNode(BaseTile tile, BlueprintNode node)
        {
            BlueprintProductionState state = null;
            if (tile is ProductionTile p) state = p.GetProductionState(node.id);
            else if (tile is FoodTile f) state = f.GetProductionState(node.id);

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
            var blueprint = node.blueprint;

            if (!AreInputsSatisfied(tile, node))
                return;

            Inventory inputBuffer = tile is ProductionTile p ? p.InputBuffer : (tile is FoodTile f ? f.InputBuffer : null);
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

        private bool AreInputsSatisfied(BaseTile tile, BlueprintNode node)
        {
            if (tile is not IGraphTile graphTile) return false;
            Inventory inputBuffer = tile is ProductionTile p ? p.InputBuffer : (tile is FoodTile f ? f.InputBuffer : null);
            if (inputBuffer == null) return false;

            var blueprint = node.blueprint;

            // Find connections TO this node
            var incomingConnections = graphTile.Graph.connections
                .Where(c => c.toNodeId == node.id)
                .ToList();

            foreach (var requiredInput in blueprint.Inputs)
            {
                bool inputFound = false;

                foreach (var conn in incomingConnections)
                {
                    // Check if connection is from an IO node
                    var ioNode = graphTile.Graph.GetIONode(conn.fromNodeId);
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
                        var sourceNode = graphTile.Graph.GetNode(conn.fromNodeId);
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
            if (tile is not IGraphTile graphTile) return;
            Inventory outputBuffer = tile is ProductionTile p ? p.OutputBuffer : (tile is FoodTile f ? f.OutputBuffer : null);
            if (outputBuffer == null) return;

            // Check for connection to either Output or Core
            // I will update the query to find ANY valid connection (Output, Core, or another Machine)

            var connection = graphTile.Graph.connections
                .FirstOrDefault(c => c.fromNodeId == node.id);

            if (connection != null && state.PendingOutput.IsValid)
            {
                var ioNode = graphTile.Graph.GetIONode(connection.toNodeId);

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
                            Debug.LogWarning("[ProductionSystem] CoreTile not found!");
                        }

                        state.PendingOutput = ItemStack.Empty;
                        state.Status = ProductionStatus.Idle;
                    }
                }
                else
                {
                    var targetNode = graphTile.Graph.GetNode(connection.toNodeId);
                    if (targetNode != null)
                    {
                        Inventory targetInputBuffer = tile is ProductionTile prod ? prod.InputBuffer : (tile is FoodTile food ? food.InputBuffer : null);
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
                if (neighbor is ProductionTile sourceProd)
                {
                    TransferItemsToSettlement(sourceProd.OutputBuffer, settlement);
                }
                else if (neighbor is FoodTile sourceFood)
                {
                    TransferItemsToSettlement(sourceFood.OutputBuffer, settlement);
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

                Inventory outputBuffer = null;
                if (sourceTile is ProductionTile p) outputBuffer = p.OutputBuffer;
                else if (sourceTile is FoodTile f) outputBuffer = f.OutputBuffer;

                if (outputBuffer != null)
                {
                    int available = outputBuffer.Get(item);
                    toTransfer = Mathf.Min(needed, Mathf.Min(node.availableItem.Amount, available));
                    if (toTransfer > 0)
                    {
                        outputBuffer.Remove(item, toTransfer);
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

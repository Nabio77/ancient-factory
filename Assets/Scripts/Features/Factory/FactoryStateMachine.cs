using System.Linq;
using UnityEngine;
using AncientFactory.Core.Data;
using AncientFactory.Features.Tiles;
using AncientFactory.Features.WorldMap;

namespace AncientFactory.Features.Factory
{
    public class FactoryStateMachine
    {
        private readonly FactoryInput _factoryInput;
        private readonly TileDataGrid _tileData;
        private CoreTile _coreTile;

        public FactoryStateMachine(FactoryInput factoryInput, TileDataGrid tileData)
        {
            _factoryInput = factoryInput;
            _tileData = tileData;
        }

        public void ProcessNode(IFactoryTile factoryTile, BlueprintNode node, float tickInterval)
        {
            var state = factoryTile.GetProductionState(node.id);
            if (state == null) return;

            switch (state.Status)
            {
                case ProductionStatus.Idle:
                    TryStartProduction(factoryTile, node, state);
                    break;

                case ProductionStatus.Producing:
                    UpdateProduction(state, tickInterval);
                    break;

                case ProductionStatus.OutputReady:
                    TryTransferOutput(factoryTile, node, state);
                    break;
            }
        }

        private void TryStartProduction(IFactoryTile factoryTile, BlueprintNode node, BlueprintProductionState state)
        {
            var blueprint = node.blueprint;

            if (!_factoryInput.TryResolveInputs(factoryTile, node, blueprint.Inputs, simulateOnly: true))
                return;

            _factoryInput.TryResolveInputs(factoryTile, node, blueprint.Inputs, simulateOnly: false);

            state.Status = ProductionStatus.Producing;
            state.ElapsedTime = 0f;
            state.TotalTime = blueprint.ProductionTime;
            state.PendingOutput = blueprint.Output;
        }

        private void UpdateProduction(BlueprintProductionState state, float tickInterval)
        {
            state.ElapsedTime += tickInterval;

            if (state.ElapsedTime >= state.TotalTime)
            {
                state.Status = ProductionStatus.OutputReady;
            }
        }

        private void TryTransferOutput(IFactoryTile factoryTile, BlueprintNode node, BlueprintProductionState state)
        {
            var outputBuffer = factoryTile.OutputBuffer;
            if (outputBuffer == null) return;

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
                            int points = state.PendingOutput.Item.TechPoints * state.PendingOutput.Amount;
                            core.AddPoints(points, state.PendingOutput.Item, state.PendingOutput.Amount);
                        }
                        else
                        {
                            Debug.LogWarning("[FactoryStateMachine] CoreTile not found!");
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
                        factoryTile.OutputBuffer.Add(state.PendingOutput.Item, state.PendingOutput.Amount);
                        state.PendingOutput = ItemStack.Empty;
                        state.Status = ProductionStatus.Idle;
                    }
                }
            }
        }

        private CoreTile GetCoreTile()
        {
            _coreTile ??= _tileData.GetAllTiles().OfType<CoreTile>().FirstOrDefault();
            return _coreTile;
        }
    }
}

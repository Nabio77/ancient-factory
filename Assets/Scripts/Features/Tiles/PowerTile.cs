using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Types;
using CarbonWorld.Features.Grid;
using CarbonWorld.Features.Inventories;

namespace CarbonWorld.Features.Tiles
{
    public class PowerTile : BaseTile, IFactoryTile
    {
        public BlueprintGraph Graph { get; } = new();
        public bool HasOutput => false;
        public Func<BlueprintDefinition, bool> BlueprintFilter => b => b.IsPowerGenerator;

        // IFactoryTile implementation - power tiles consume fuel
        public bool IsPowered { get; set; } = true; // Power tiles are always "powered" (self-powered)
        public Inventory InputBuffer { get; } = new();
        public Inventory OutputBuffer { get; } = new(); // Not used, but required by interface

        // Production state tracking per generator node
        private readonly Dictionary<string, BlueprintProductionState> _productionStates = new();
        private readonly Dictionary<string, bool> _generatorActive = new();

        public int TotalPowerOutput { get; private set; }
        public int TotalPowerConsumption { get; private set; }
        public int EffectiveRadius { get; private set; }

        public PowerTile(Vector3Int cellPosition)
            : base(cellPosition, TileType.Power)
        {
        }

        public BlueprintProductionState GetProductionState(string nodeId)
        {
            if (!_productionStates.TryGetValue(nodeId, out var state))
            {
                state = new BlueprintProductionState(nodeId);
                _productionStates[nodeId] = state;
            }
            return state;
        }

        public IEnumerable<BlueprintProductionState> GetAllProductionStates()
        {
            return _productionStates.Values;
        }

        public List<ItemStack> GetPotentialOutputs()
        {
            // Power tiles don't output items
            return new List<ItemStack>();
        }

        public void UpdateIO(TileDataGrid tileData)
        {
            var existingInputs = Graph.ioNodes.Where(n => n.type == TileIOType.Input).ToList();
            var newInputs = new List<TileIONode>();

            var neighbors = tileData.GetNeighbors(CellPosition);
            int inputIndex = 0;

            foreach (var neighbor in neighbors)
            {
                // Power tiles only accept inputs from ResourceTiles and TransportTiles
                if (neighbor is ResourceTile resourceTile)
                {
                    var output = resourceTile.PeekOutput();
                    if (output.IsValid)
                    {
                        var existingNode = existingInputs.Find(n =>
                            n.sourceTilePosition == neighbor.CellPosition &&
                            n.availableItem.Item == output.Item);

                        if (existingNode != null)
                        {
                            // Update existing node, preserve ID
                            existingNode.availableItem = output;
                            existingNode.index = inputIndex;
                            newInputs.Add(existingNode);
                        }
                        else
                        {
                            // Create new node
                            var newNode = new TileIONode(TileIOType.Input, neighbor.CellPosition, neighbor.Type, output, inputIndex);
                            newInputs.Add(newNode);
                        }
                        inputIndex++;
                    }
                }
                else if (neighbor is TransportTile transportTile)
                {
                    var outputs = transportTile.GetOutputs();
                    foreach (var output in outputs)
                    {
                        var existingNode = existingInputs.Find(n =>
                            n.sourceTilePosition == neighbor.CellPosition &&
                            n.availableItem.Item == output.Item);

                        if (existingNode != null)
                        {
                            existingNode.availableItem = output;
                            existingNode.index = inputIndex;
                            newInputs.Add(existingNode);
                        }
                        else
                        {
                            var newNode = new TileIONode(TileIOType.Input, neighbor.CellPosition, neighbor.Type, output, inputIndex);
                            newInputs.Add(newNode);
                        }
                        inputIndex++;
                    }
                }
            }

            // Replace input nodes with new set (preserving IDs where possible)
            Graph.ioNodes.RemoveAll(n => n.type == TileIOType.Input);
            foreach (var node in newInputs)
            {
                if (!Graph.ioNodes.Contains(node))
                {
                    Graph.ioNodes.Add(node);
                }
            }
        }

        /// <summary>
        /// Sets whether a generator node is actively producing power.
        /// Called by FactorySystem after processing fuel consumption.
        /// </summary>
        public void SetGeneratorActive(string nodeId, bool active)
        {
            _generatorActive[nodeId] = active;
        }

        public bool IsGeneratorActive(string nodeId)
        {
            return _generatorActive.TryGetValue(nodeId, out var active) && active;
        }

        public void CalculatePowerOutput()
        {
            TotalPowerOutput = 0;
            TotalPowerConsumption = 0;

            foreach (var node in Graph.nodes)
            {
                if (node.blueprint == null || !node.blueprint.IsPowerGenerator)
                    continue;

                // Solar (no inputs) is always active
                // Fuel-based generators are active only if FactorySystem processed fuel
                bool isActive = node.blueprint.Inputs.Count == 0 || IsGeneratorActive(node.id);

                if (isActive)
                {
                    TotalPowerOutput += node.blueprint.PowerOutput;
                    TotalPowerConsumption += node.blueprint.PowerConsumption;
                }
            }

            // Calculate radius based on total power output
            EffectiveRadius = CalculateRadiusFromPower(TotalPowerOutput);
        }

        private int CalculateRadiusFromPower(int power)
        {
            if (power <= 0) return 0;
            if (power < 50) return 1;
            if (power < 150) return 2;
            if (power < 300) return 3;
            return 4;
        }

        public List<Vector3Int> GetPoweredPositions()
        {
            if (EffectiveRadius <= 0)
                return new List<Vector3Int>();

            return HexUtils.GetSpiral(CellPosition, EffectiveRadius);
        }
    }
}

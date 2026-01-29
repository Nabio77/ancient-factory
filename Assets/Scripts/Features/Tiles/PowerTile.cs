using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Types;
using CarbonWorld.Features.Grid;

namespace CarbonWorld.Features.Tiles
{
    public class PowerTile : BaseTile, IGraphTile
    {
        public BlueprintGraph Graph { get; } = new();
        public bool HasOutput => false;
        public Func<BlueprintDefinition, bool> BlueprintFilter => b => b.IsPowerGenerator;

        public int TotalPowerOutput { get; private set; }
        public int TotalPowerConsumption { get; private set; }
        public int EffectiveRadius { get; private set; }

        public PowerTile(Vector3Int cellPosition)
            : base(cellPosition, TileType.Power)
        {
        }

        public void UpdateIO(TileDataGrid tileData)
        {
            var oldIoNodes = Graph.ioNodes.ToList();
            Graph.ioNodes.Clear();

            var neighbors = tileData.GetNeighbors(CellPosition);
            int inputIndex = 0;

            foreach (var neighbor in neighbors)
            {
                // Power tiles only accept inputs from ResourceTiles, not ProductionTiles
                if (neighbor is ResourceTile resourceTile)
                {
                    var output = resourceTile.GetOutput();
                    if (output.IsValid)
                    {
                        AddInputNode(neighbor.CellPosition, neighbor.Type, output, inputIndex++);
                    }
                }
                else if (neighbor is TransportTile transportTile)
                {
                    var outputs = transportTile.GetOutputs();
                    foreach (var output in outputs)
                    {
                        AddInputNode(neighbor.CellPosition, neighbor.Type, output, inputIndex++);
                    }
                }
            }

            // Preserve IDs for existing nodes to maintain connections
            foreach (var newNode in Graph.ioNodes.Where(n => n.type == TileIOType.Input))
            {
                var match = oldIoNodes.Find(old =>
                    old.type == TileIOType.Input &&
                    old.sourceTilePosition == newNode.sourceTilePosition &&
                    old.availableItem.Item == newNode.availableItem.Item);

                if (match != null)
                {
                    newNode.id = match.id;
                }
            }

            // No output node for power tiles - power is distributed via radius, not item flow
        }

        private void AddInputNode(Vector3Int sourcePos, TileType type, ItemStack item, int index)
        {
            var node = new TileIONode(TileIOType.Input, sourcePos, type, item, index);
            Graph.ioNodes.Add(node);
        }

        public void CalculatePowerOutput()
        {
            TotalPowerOutput = 0;
            TotalPowerConsumption = 0;

            foreach (var node in Graph.nodes)
            {
                if (node.blueprint == null || !node.blueprint.IsPowerGenerator)
                    continue;

                bool hasRequiredInputs = CheckBlueprintInputsSatisfied(node);

                if (hasRequiredInputs)
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

        private bool CheckBlueprintInputsSatisfied(BlueprintNode node)
        {
            // If blueprint has no inputs (like Solar), always satisfied
            if (node.blueprint.Inputs.Count == 0)
                return true;

            // Check if all required inputs have connections from IO nodes
            var incomingConnections = Graph.connections
                .Where(c => c.toNodeId == node.id)
                .ToList();

            foreach (var requiredInput in node.blueprint.Inputs)
            {
                bool found = incomingConnections.Any(conn =>
                {
                    var ioNode = Graph.ioNodes.FirstOrDefault(io => io.id == conn.fromNodeId);
                    return ioNode != null && ioNode.availableItem.Item == requiredInput.Item;
                });

                if (!found)
                    return false;
            }

            return true;
        }

        public List<Vector3Int> GetPoweredPositions()
        {
            if (EffectiveRadius <= 0)
                return new List<Vector3Int>();

            return HexUtils.GetSpiral(CellPosition, EffectiveRadius);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Types;
using CarbonWorld.Features.Inventories;

namespace CarbonWorld.Features.Tiles
{
    public class ProductionTile : BaseTile, IGraphTile
    {
        public BlueprintGraph Graph { get; } = new();
        public bool IsPowered { get; set; }
        public bool HasOutput => true;
        public Func<BlueprintDefinition, bool> BlueprintFilter => b => b.IsProducer || b.IsLogistics;

        // Production state tracking per blueprint node
        private readonly Dictionary<string, BlueprintProductionState> _productionStates = new();

        // Separate buffers for production flow
        public Inventory InputBuffer { get; } = new();
        public Inventory OutputBuffer { get; } = new();

        public ProductionTile(Vector3Int cellPosition)
            : base(cellPosition, TileType.Production)
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
            var outputs = new List<ItemStack>();

            // Find connections that go to the output IO node
            var outputConnections = Graph.connections
                .Where(c => c.toNodeId != null && Graph.ioNodes.Any(io => io.id == c.toNodeId && io.type == TileIOType.Output))
                .ToList();

            foreach (var conn in outputConnections)
            {
                var sourceNode = Graph.GetNode(conn.fromNodeId);
                if (sourceNode?.blueprint != null && sourceNode.blueprint.Output.IsValid)
                {
                    outputs.Add(sourceNode.blueprint.Output);
                }
            }

            return outputs;
        }
    }
}

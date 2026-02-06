using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AncientFactory.Core.Data;
using AncientFactory.Core.Types;
using AncientFactory.Features.Inventories;

namespace AncientFactory.Features.Tiles
{
    public enum FactoryCategory
    {
        Production,
        Food
    }

    public class FactoryTile : BaseTile, IFactoryTile
    {
        public FactoryCategory Category { get; }
        public BlueprintGraph Graph { get; } = new();
        public bool IsPowered { get; set; }
        public bool HasOutput => true;

        public Func<BlueprintDefinition, bool> BlueprintFilter => Category switch
        {
            FactoryCategory.Food => b => b.Type == BlueprintType.FoodProcessor || b.IsLogistics,
            _ => b => b.IsProducer || b.IsLogistics
        };

        // Production state tracking per blueprint node
        private readonly Dictionary<string, BlueprintProductionState> _productionStates = new();

        // Output buffer for produced items
        public Inventory OutputBuffer { get; } = new();

        public FactoryTile(Vector3Int cellPosition, FactoryCategory category = FactoryCategory.Production)
            : base(cellPosition, category == FactoryCategory.Food ? TileType.Food : TileType.Production)
        {
            Category = category;
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

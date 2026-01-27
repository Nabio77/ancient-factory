using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Features.Tiles
{
    public class ProductionTile : BaseTile, IGraphTile
    {
        public BlueprintGraph Graph { get; } = new();
        public bool IsPowered { get; set; }
        public bool HasOutput => true;
        public Func<BlueprintDefinition, bool> BlueprintFilter => b => b.IsProducer || b.IsLogistics;

        public ProductionTile(Vector3Int cellPosition)
            : base(cellPosition, TileType.Production)
        {
        }

        public List<ItemStack> GetOutputs()
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

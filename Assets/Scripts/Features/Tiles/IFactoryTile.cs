using System.Collections.Generic;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.Inventories;

namespace CarbonWorld.Features.Tiles
{
    /// <summary>
    /// Interface for tiles that can process blueprints (factories).
    /// Combines graph capabilities with production buffers and state tracking.
    /// </summary>
    public interface IFactoryTile : IGraphTile
    {
        bool IsPowered { get; set; }
        Inventory OutputBuffer { get; }
        BlueprintProductionState GetProductionState(string nodeId);
        IEnumerable<BlueprintProductionState> GetAllProductionStates();
        List<ItemStack> GetPotentialOutputs();
    }
}

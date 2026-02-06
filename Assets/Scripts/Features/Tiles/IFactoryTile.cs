using System.Collections.Generic;
using AncientFactory.Core.Data;
using AncientFactory.Features.Inventories;

namespace AncientFactory.Features.Tiles
{
    /// <summary>
    /// Interface for tiles that can process blueprints (factories).
    /// Combines graph capabilities with production buffers and state tracking.
    /// </summary>
    public interface IFactoryTile : IGraphTile
    {
        bool HasWorkers { get; set; }
        Inventory OutputBuffer { get; }
        BlueprintProductionState GetProductionState(string nodeId);
        IEnumerable<BlueprintProductionState> GetAllProductionStates();
        List<ItemStack> GetPotentialOutputs();
    }
}

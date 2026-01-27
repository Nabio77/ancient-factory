using System;
using CarbonWorld.Core.Data;

namespace CarbonWorld.Features.Tiles
{
    public interface IGraphTile
    {
        BlueprintGraph Graph { get; }
        bool HasOutput { get; }
        Func<BlueprintDefinition, bool> BlueprintFilter { get; }
    }
}

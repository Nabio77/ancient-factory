using System;
using AncientFactory.Core.Data;

namespace AncientFactory.Features.Tiles
{
    public interface IGraphTile
    {
        BlueprintGraph Graph { get; }
        bool HasOutput { get; }
        Func<BlueprintDefinition, bool> BlueprintFilter { get; }
    }
}

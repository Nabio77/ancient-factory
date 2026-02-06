namespace AncientFactory.Core.Types
{
    public enum BlueprintType
    {
        // Production Types
        Forge,      // Metal smelting (ore → ingots)
        Kiln,       // Heat processing (clay → bricks, glass)
        Workshop,   // Basic crafting/woodworking
        Artisan,    // Complex skilled assembly

        // Utility Types
        Divider,    // Logistics - splitting
        Combiner,   // Logistics - merging

        // Power Generation
        Prana,      // Vital energy generation

        // Food Types
        Kitchen     // Food preparation/cooking
    }
}


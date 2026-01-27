namespace CarbonWorld.Core.Data
{
    public enum ChangeType
    {
        Added,
        Removed,
        Cleared
    }

    public readonly struct InventoryChange
    {
        public readonly ItemDefinition Item;
        public readonly int Amount;
        public readonly int PreviousAmount;
        public readonly int NewAmount;
        public readonly ChangeType Type;

        public InventoryChange(ItemDefinition item, int amount, int previousAmount, int newAmount, ChangeType type)
        {
            Item = item;
            Amount = amount;
            PreviousAmount = previousAmount;
            NewAmount = newAmount;
            Type = type;
        }
    }
}

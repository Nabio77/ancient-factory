using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CarbonWorld.Core.Data
{
    [Serializable]
    public struct ItemStack
    {
        [HorizontalGroup("Stack"), HideLabel]
        [SerializeField]
        private ItemDefinition item;

        [HorizontalGroup("Stack"), HideLabel]
        [SerializeField, Min(1)]
        private int amount;

        public ItemDefinition Item => item;
        public int Amount => amount;
        public bool IsValid => item != null && amount > 0;

        public ItemStack(ItemDefinition item, int amount)
        {
            this.item = item;
            this.amount = Mathf.Max(0, amount);
        }

        public ItemStack WithAmount(int newAmount)
        {
            return new ItemStack(item, newAmount);
        }

        public static ItemStack Empty => new(null, 0);

        public override string ToString()
        {
            return item != null ? $"{amount}x {item.ItemName}" : "Empty";
        }
    }
}

using System;
using System.Collections.Generic;
using CarbonWorld.Core.Data;

namespace CarbonWorld.Features.Inventories
{
    [Serializable]
    public class Inventory
    {
        private readonly Dictionary<ItemDefinition, int> _items = new();

        public int Get(ItemDefinition item)
        {
            if (item == null) return 0;
            return _items.GetValueOrDefault(item, 0);
        }

        public void Add(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return;
            _items[item] = Get(item) + amount;
        }

        public void Add(ItemStack stack)
        {
            if (stack.IsValid)
            {
                Add(stack.Item, stack.Amount);
            }
        }

        public bool Remove(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return true;

            int current = Get(item);
            if (current < amount) return false;

            int remaining = current - amount;
            if (remaining == 0)
            {
                _items.Remove(item);
            }
            else
            {
                _items[item] = remaining;
            }
            return true;
        }

        public bool Remove(ItemStack stack)
        {
            return stack.IsValid && Remove(stack.Item, stack.Amount);
        }

        public bool Has(ItemDefinition item, int amount)
        {
            return Get(item) >= amount;
        }

        public bool Has(ItemStack stack)
        {
            return !stack.IsValid || Has(stack.Item, stack.Amount);
        }

        public bool HasAll(IEnumerable<ItemStack> stacks)
        {
            foreach (var stack in stacks)
            {
                if (!Has(stack)) return false;
            }
            return true;
        }

        public void Clear()
        {
            _items.Clear();
        }

        public IEnumerable<ItemStack> GetAll()
        {
            foreach (var kvp in _items)
            {
                yield return new ItemStack(kvp.Key, kvp.Value);
            }
        }

        public int TotalItemCount
        {
            get
            {
                int total = 0;
                foreach (var amount in _items.Values)
                {
                    total += amount;
                }
                return total;
            }
        }

        public int UniqueItemCount => _items.Count;

        public bool IsEmpty => _items.Count == 0;
    }
}

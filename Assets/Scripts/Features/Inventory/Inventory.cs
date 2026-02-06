using System;
using System.Collections.Generic;
using AncientFactory.Core.Data;

namespace AncientFactory.Features.Inventories
{
    public readonly struct InventoryBatch : IDisposable
    {
        private readonly Inventory _inventory;

        public InventoryBatch(Inventory inventory, object source = null, string reason = null)
        {
            _inventory = inventory;
            _inventory.BeginBatch(source, reason);
        }

        public void Dispose()
        {
            _inventory.EndBatch();
        }
    }

    public class InventoryChangedArgs : EventArgs
    {
        public IReadOnlyList<InventoryChange> Changes { get; }
        public object Source { get; }
        public string Reason { get; }

        public InventoryChangedArgs(IReadOnlyList<InventoryChange> changes, object source = null, string reason = null)
        {
            Changes = changes;
            Source = source;
            Reason = reason;
        }

        public bool HasItemChanged(ItemDefinition item)
        {
            foreach (var change in Changes)
            {
                if (change.Item == item) return true;
            }
            return false;
        }
    }

    [Serializable]
    public class Inventory
    {
        public event Action<InventoryChangedArgs> InventoryChanged;

        private readonly Dictionary<ItemDefinition, int> _items = new();

        private bool _isBatching;
        private List<InventoryChange> _pendingChanges = new();
        private object _batchSource;
        private string _batchReason;

        public void BeginBatch(object source = null, string reason = null)
        {
            _isBatching = true;
            _batchSource = source;
            _batchReason = reason;
            _pendingChanges.Clear();
        }

        public void EndBatch()
        {
            if (!_isBatching) return;
            _isBatching = false;

            if (_pendingChanges.Count > 0)
            {
                var args = new InventoryChangedArgs(_pendingChanges.ToArray(), _batchSource, _batchReason);
                InventoryChanged?.Invoke(args);
            }
            _pendingChanges.Clear();
            _batchSource = null;
            _batchReason = null;
        }

        public int Get(ItemDefinition item)
        {
            if (item == null) return 0;
            return _items.GetValueOrDefault(item, 0);
        }

        public void Add(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return;

            int previous = Get(item);
            _items[item] = previous + amount;
            int newAmount = _items[item];

            var change = new InventoryChange(item, amount, previous, newAmount, ChangeType.Added);

            if (_isBatching)
            {
                _pendingChanges.Add(change);
            }
            else
            {
                InventoryChanged?.Invoke(new InventoryChangedArgs(new[] { change }));
            }
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
            int previous = current;

            if (remaining == 0)
            {
                _items.Remove(item);
            }
            else
            {
                _items[item] = remaining;
            }

            var change = new InventoryChange(item, amount, previous, remaining, ChangeType.Removed);

            if (_isBatching)
            {
                _pendingChanges.Add(change);
            }
            else
            {
                InventoryChanged?.Invoke(new InventoryChangedArgs(new[] { change }));
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
            if (_items.Count == 0) return;

            var changes = new List<InventoryChange>();
            foreach (var kvp in _items)
            {
                changes.Add(new InventoryChange(kvp.Key, kvp.Value, kvp.Value, 0, ChangeType.Cleared));
            }

            _items.Clear();

            if (_isBatching)
            {
                _pendingChanges.AddRange(changes);
            }
            else
            {
                InventoryChanged?.Invoke(new InventoryChangedArgs(changes));
            }
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

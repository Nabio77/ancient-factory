using UnityEngine;
using System.Collections.Generic;
using AncientFactory.Core.Data;
using AncientFactory.Features.Inventories;

namespace AncientFactory.Core.Systems
{
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }

        private readonly Inventory _inventory = new();

        public Inventory Inventory => _inventory;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public int Get(ItemDefinition item) => _inventory.Get(item);
        public void Add(ItemDefinition item, int amount) => _inventory.Add(item, amount);
        public void Add(ItemStack stack) => _inventory.Add(stack);
        public bool Remove(ItemDefinition item, int amount) => _inventory.Remove(item, amount);
        public bool Remove(ItemStack stack) => _inventory.Remove(stack);
        public bool Has(ItemDefinition item, int amount) => _inventory.Has(item, amount);
        public bool Has(ItemStack stack) => _inventory.Has(stack);
        public bool HasAll(IEnumerable<ItemStack> stacks) => _inventory.HasAll(stacks);
        public void Clear() => _inventory.Clear();
        public IEnumerable<ItemStack> GetAll() => _inventory.GetAll();

        public int TotalItemCount => _inventory.TotalItemCount;
        public int UniqueItemCount => _inventory.UniqueItemCount;
        public bool IsEmpty => _inventory.IsEmpty;
    }
}

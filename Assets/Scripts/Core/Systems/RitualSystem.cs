using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using AncientFactory.Core.Data;
using AncientFactory.Features.Inventories;

namespace AncientFactory.Core.Systems
{
    public class RitualSystem : MonoBehaviour
    {
        public static RitualSystem Instance { get; private set; }

        [Title("References")]
        [SerializeField, Required]
        private DivineDispleasureSystem displeasureSystem;

        [Title("Offering Settings")]
        [SerializeField, Tooltip("Displeasure reduction per gold offered")]
        private int goldOfferingValue = 20;

        [SerializeField, Tooltip("Displeasure reduction per food offered")]
        private int foodOfferingValue = 5;

        [Title("Festival Settings")]
        [SerializeField, Tooltip("Displeasure reduction from wine festival")]
        private int wineFestivalReduction = 50;

        [SerializeField, Tooltip("Wine required for festival")]
        private int wineFestivalCost = 5;

        [SerializeField, Tooltip("Displeasure reduction from grand feast")]
        private int feastReduction = 100;

        [SerializeField, Tooltip("Feasts required for grand celebration")]
        private int feastCost = 3;

        [Title("Cooldowns")]
        [SerializeField, Tooltip("Ticks between festivals")]
        private int festivalCooldown = 30;

        [ShowInInspector, ReadOnly]
        private int _ticksSinceLastFestival;

        // Events
        public event Action<string, int> OnOfferingMade; // type, reduction
        public event Action<string, int> OnFestivalHeld; // type, reduction

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (_ticksSinceLastFestival < festivalCooldown)
            {
                _ticksSinceLastFestival++;
            }
        }

        public bool CanHoldFestival => _ticksSinceLastFestival >= festivalCooldown;

        public bool MakeGoldOffering(Inventory inventory, ItemDefinition goldItem, int amount)
        {
            if (goldItem == null || amount <= 0) return false;

            var stack = new ItemStack(goldItem, amount);
            if (!inventory.Has(stack)) return false;

            inventory.Remove(stack);
            int reduction = amount * goldOfferingValue;
            displeasureSystem.RemoveDispleasure(reduction);

            OnOfferingMade?.Invoke("Gold", reduction);
            return true;
        }

        public bool MakeFoodOffering(Inventory inventory, ItemDefinition foodItem, int amount)
        {
            if (foodItem == null || amount <= 0) return false;

            var stack = new ItemStack(foodItem, amount);
            if (!inventory.Has(stack)) return false;

            inventory.Remove(stack);
            int reduction = amount * foodOfferingValue;
            displeasureSystem.RemoveDispleasure(reduction);

            OnOfferingMade?.Invoke("Food", reduction);
            return true;
        }

        public bool HoldWineFestival(Inventory inventory, ItemDefinition wineItem)
        {
            if (!CanHoldFestival) return false;
            if (wineItem == null) return false;

            var stack = new ItemStack(wineItem, wineFestivalCost);
            if (!inventory.Has(stack)) return false;

            inventory.Remove(stack);
            displeasureSystem.RemoveDispleasure(wineFestivalReduction);
            _ticksSinceLastFestival = 0;

            OnFestivalHeld?.Invoke("Wine Festival", wineFestivalReduction);
            return true;
        }

        public bool HoldGrandFeast(Inventory inventory, ItemDefinition feastItem)
        {
            if (!CanHoldFestival) return false;
            if (feastItem == null) return false;

            var stack = new ItemStack(feastItem, feastCost);
            if (!inventory.Has(stack)) return false;

            inventory.Remove(stack);
            displeasureSystem.RemoveDispleasure(feastReduction);
            _ticksSinceLastFestival = 0;

            OnFestivalHeld?.Invoke("Grand Feast", feastReduction);
            return true;
        }

        public int GetFestivalCooldownRemaining()
        {
            return Mathf.Max(0, festivalCooldown - _ticksSinceLastFestival);
        }

        [Button("Reset Cooldown")]
        public void ResetCooldown()
        {
            _ticksSinceLastFestival = festivalCooldown;
        }
    }
}

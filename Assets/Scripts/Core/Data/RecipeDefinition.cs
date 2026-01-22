using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Inventories;

namespace CarbonWorld.Core.Data
{
    [CreateAssetMenu(fileName = "Recipe_", menuName = "Carbon World/Recipe Definition")]
    public class RecipeDefinition : ScriptableObject
    {
        [Title("Recipe")]
        [SerializeField]
        private string recipeName;

        [SerializeField, TextArea(2, 3)]
        private string description;

        [Title("Inputs")]
        [SerializeField, ListDrawerSettings(ShowFoldout = false)]
        private List<ItemStack> inputs = new();

        [Title("Outputs")]
        [SerializeField, ListDrawerSettings(ShowFoldout = false)]
        private List<ItemStack> outputs = new();

        [Title("Production")]
        [SerializeField, Tooltip("Time in seconds to complete one production cycle")]
        private float productionTime = 1f;

        [SerializeField, Tooltip("Power required per tick while producing")]
        private int powerConsumption = 1;

        public string RecipeName => string.IsNullOrEmpty(recipeName) ? name : recipeName;
        public string Description => description;
        public IReadOnlyList<ItemStack> Inputs => inputs;
        public IReadOnlyList<ItemStack> Outputs => outputs;
        public float ProductionTime => productionTime;
        public int PowerConsumption => powerConsumption;

        public bool CanProduce(Inventory inventory)
        {
            foreach (var input in inputs)
            {
                if (!inventory.Has(input)) return false;
            }
            return true;
        }

        public void ConsumeInputs(Inventory inventory)
        {
            foreach (var input in inputs)
            {
                inventory.Remove(input);
            }
        }

        public void ProduceOutputs(Inventory inventory)
        {
            foreach (var output in outputs)
            {
                inventory.Add(output);
            }
        }

        public int GetOutputTier()
        {
            int maxTier = 0;
            foreach (var output in outputs)
            {
                if (output.Item != null && output.Item.Tier > maxTier)
                {
                    maxTier = output.Item.Tier;
                }
            }
            return maxTier;
        }
    }
}

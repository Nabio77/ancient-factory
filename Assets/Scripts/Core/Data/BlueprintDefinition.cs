using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Types;
using CarbonWorld.Features.Inventories;

namespace CarbonWorld.Core.Data
{
    [CreateAssetMenu(fileName = "Blueprint_", menuName = "Carbon World/Blueprint Definition")]
    public class BlueprintDefinition : ScriptableObject
    {
        [Title("Identity")]
        [SerializeField]
        private string blueprintName;

        [SerializeField, TextArea(2, 3)]
        private string description;

        [SerializeField, PreviewField(50)]
        private Sprite icon;

        [Title("Type")]
        [SerializeField]
        private BlueprintType type = BlueprintType.Constructor;

        [Title("Connection Points")]
        [SerializeField, Range(1, 4)]
        private int inputCount = 1;

        [SerializeField, Range(1, 4)]
        private int outputCount = 1;

        [Title("Production")]
        [SerializeField, ShowIf("IsProducer"), ListDrawerSettings(ShowFoldout = false)]
        private List<ItemStack> inputs = new();

        [SerializeField, ShowIf("IsProducer")]
        private ItemStack output;

        [SerializeField, ShowIf("IsProducer"), Tooltip("Time in seconds to complete one production cycle")]
        private float productionTime = 1f;

        [SerializeField, ShowIf("IsProducer"), Tooltip("Power required per tick while producing")]
        private int powerConsumption = 1;

        [Title("Power Generation")]
        [SerializeField, ShowIf("IsPowerGenerator"), Tooltip("Power produced per tick")]
        private int powerOutput = 0;

        [Title("Carbon")]
        [SerializeField, ShowIf("@IsProducer || IsPowerGenerator"), Tooltip("Carbon emitted per production cycle")]
        private int carbonEmission = 0;

        [Title("Acquisition")]
        [SerializeField, Tooltip("Available from the start of the game")]
        private bool isStarterCard = true;

        [SerializeField, HideIf("isStarterCard"), Tooltip("Cost to unlock in tech tree")]
        private int unlockCost = 0;

        public string BlueprintName => string.IsNullOrEmpty(blueprintName) ? name : blueprintName;
        public string Description => description;
        public Sprite Icon => icon;
        public BlueprintType Type => type;
        public bool IsStarterCard => isStarterCard;
        public int UnlockCost => unlockCost;

        public int InputCount => inputCount;
        public int OutputCount => outputCount;

        // Production Properties
        public IReadOnlyList<ItemStack> Inputs => inputs;
        public ItemStack Output => output;
        public float ProductionTime => productionTime;
        public int PowerConsumption => powerConsumption;

        public bool IsProducer => type == BlueprintType.Smelter || type == BlueprintType.Furnace || type == BlueprintType.Constructor || type == BlueprintType.Assembler || type == BlueprintType.FoodProcessor;
        public bool IsLogistics => type == BlueprintType.Splitter || type == BlueprintType.Merger;
        public bool IsPowerGenerator => type == BlueprintType.Power;

        // Power Generation Properties
        public int PowerOutput => powerOutput;

        // Carbon Properties
        public int CarbonEmission => carbonEmission;

        // Production Logic Helpers
        public bool CanProduce(Inventory inventory)
        {
            if (!IsProducer) return false;
            foreach (var input in inputs)
            {
                if (!inventory.Has(input)) return false;
            }
            return true;
        }

        public void ConsumeInputs(Inventory inventory)
        {
            if (!IsProducer) return;
            foreach (var input in inputs)
            {
                inventory.Remove(input);
            }
        }

        public void ProduceOutputs(Inventory inventory)
        {
            if (!IsProducer) return;
            if (output.IsValid)
            {
                inventory.Add(output);
            }
        }

        public int GetOutputTier()
        {
            return output.Item != null ? output.Item.Tier : 0;
        }
    }
}
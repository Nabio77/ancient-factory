using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using AncientFactory.Core.Types;
using AncientFactory.Features.Inventories;

namespace AncientFactory.Core.Data
{
    [CreateAssetMenu(fileName = "Blueprint_", menuName = "Ancient Factory/Blueprint Definition")]
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
        private BlueprintType type = BlueprintType.Workshop;

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

        [SerializeField, ShowIf("IsProducer"), Tooltip("Workers required to operate this facility")]
        private int workforceRequirement = 1;

        [Title("Divine Displeasure")]
        [SerializeField, ShowIf("IsProducer"), Tooltip("Base displeasure generated per production cycle")]
        private int divineDispleasure = 0;

        [SerializeField, ShowIf("IsProducer"), Tooltip("Source of displeasure affects the multiplier")]
        private DispleasureSource displeasureSource = DispleasureSource.Craftsmanship;

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
        public int WorkforceRequirement => workforceRequirement;

        public bool IsProducer => type == BlueprintType.Forge || type == BlueprintType.Kiln || type == BlueprintType.Workshop || type == BlueprintType.Artisan || type == BlueprintType.Kitchen;
        public bool IsLogistics => type == BlueprintType.Divider || type == BlueprintType.Combiner;

        // Divine Displeasure Properties
        public int BaseDivineDispleasure => divineDispleasure;
        public DispleasureSource DispleasureSource => displeasureSource;

        public float DispleasureMultiplier => displeasureSource switch
        {
            DispleasureSource.SlaveLabor => 2.0f,
            DispleasureSource.SacredEarth => 1.5f,
            DispleasureSource.Craftsmanship => 1.0f,
            _ => 1.0f
        };

        public int DivineDispleasure => Mathf.RoundToInt(divineDispleasure * DispleasureMultiplier);

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
using UnityEngine;
using Sirenix.OdinInspector;

namespace CarbonWorld.Core.Data
{
    [CreateAssetMenu(fileName = "Item_", menuName = "Carbon World/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Title("Identity")]
        [SerializeField]
        private string itemName;

        [SerializeField, TextArea(2, 4)]
        private string description;

        [SerializeField, PreviewField(50)]
        private Sprite icon;

        [Title("Classification")]
        [SerializeField, Range(0, 6)]
        private int tier;

        [SerializeField]
        private ItemTier category;

        [Title("Properties")]
        [SerializeField, Tooltip("Base value for economy calculations")]
        private int baseValue = 1;

        [SerializeField, Tooltip("Stack size limit in inventories (0 = unlimited)")]
        private int maxStackSize = 0;

        [Title("Food Properties")]
        [SerializeField]
        private bool isFood;

        [SerializeField, ShowIf("isFood"), Tooltip("Health provided per unit")]
        private int nutritionalValue = 1;

        public string ItemName => string.IsNullOrEmpty(itemName) ? name : itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public int Tier => tier;
        public ItemTier Category => category;
        public int BaseValue => baseValue;
        public int MaxStackSize => maxStackSize;
        public bool IsFood => isFood;
        public int NutritionalValue => nutritionalValue;

        public bool IsRawResource => tier == 0;
        public bool IsFinalProduct => tier == 6;
    }

    public enum ItemTier
    {
        Raw,            // T0: Coal, Iron Ore, etc.
        Basic,          // T1: Plates, Bricks, Planks
        Processed,      // T2: Steel, Wire, Concrete
        Component,      // T3: Circuits, Motors, Pipes
        Advanced,       // T4: Processors, Batteries
        Assembly,       // T5: Engines, Computer Units
        FinalProduct    // T6: Vehicles, Electronics
    }
}

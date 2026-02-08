using UnityEngine;
using Sirenix.OdinInspector;

namespace AncientFactory.Core.Data
{
    [CreateAssetMenu(fileName = "Item_", menuName = "Ancient Factory/Item Definition")]
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

        [SerializeField, Tooltip("Stack size limit in inventories (0 = unlimited)")]
        private int maxStackSize = 0;

        [SerializeField, Tooltip("Points awarded when sent to Core")]
        private int techPoints = 1;

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
        public int MaxStackSize => maxStackSize;
        public int TechPoints => techPoints;
        public bool IsFood => isFood;
        public int NutritionalValue => nutritionalValue;

        public bool IsRawResource => tier == 0;
        public bool IsFinalProduct => tier == 6;
    }

    public enum ItemTier
    {
        Harvest,        // T0: Raw materials - Ore, Clay, Wood
        Refined,        // T1: First processing - Ingots, Bricks, Planks
        Crafted,        // T2: Secondary processing - Bronze, Cement
        Artisan,        // T3: Skilled craftwork - Tools, Pottery
        Fine,           // T4: High-quality goods - Weapons, Armor
        Grand,          // T5: Large constructions - Chariot Frame
        Masterwork      // T6: Ultimate achievements - Statues, Chariots
    }
}

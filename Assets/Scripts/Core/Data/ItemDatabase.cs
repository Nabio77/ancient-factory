using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CarbonWorld.Core.Data
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Carbon World/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField, ListDrawerSettings(ShowFoldout = false)]
        private List<ItemDefinition> items = new();

        public IReadOnlyList<ItemDefinition> Items => items;

        public ItemDefinition GetByName(string itemName)
        {
            return items.FirstOrDefault(i => i.ItemName == itemName);
        }

        public IEnumerable<ItemDefinition> GetByTier(int tier)
        {
            return items.Where(i => i.Tier == tier);
        }

        public IEnumerable<ItemDefinition> GetByCategory(ItemTier category)
        {
            return items.Where(i => i.Category == category);
        }

        public IEnumerable<ItemDefinition> GetFinalProducts()
        {
            return items.Where(i => i.IsFinalProduct);
        }

        public IEnumerable<ItemDefinition> GetRawResources()
        {
            return items.Where(i => i.IsRawResource);
        }

        public IEnumerable<ItemDefinition> GetFoodItems()
        {
            return items.Where(i => i.IsFood);
        }

#if UNITY_EDITOR
        [Button("Auto-populate from project"), PropertyOrder(-1)]
        private void AutoPopulate()
        {
            items.Clear();
            var guids = UnityEditor.AssetDatabase.FindAssets("t:ItemDefinition");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item != null)
                {
                    items.Add(item);
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

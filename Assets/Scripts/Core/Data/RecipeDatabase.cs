using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CarbonWorld.Core.Data
{
    [CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Carbon World/Recipe Database")]
    public class RecipeDatabase : ScriptableObject
    {
        [SerializeField, ListDrawerSettings(ShowFoldout = false)]
        private List<RecipeDefinition> recipes = new();

        public IReadOnlyList<RecipeDefinition> Recipes => recipes;

        public RecipeDefinition GetByName(string recipeName)
        {
            return recipes.FirstOrDefault(r => r.RecipeName == recipeName);
        }

        public IEnumerable<RecipeDefinition> GetByOutputTier(int tier)
        {
            return recipes.Where(r => r.GetOutputTier() == tier);
        }

        public IEnumerable<RecipeDefinition> GetRecipesProducing(ItemDefinition item)
        {
            return recipes.Where(r => r.Outputs.Any(o => o.Item == item));
        }

        public IEnumerable<RecipeDefinition> GetRecipesRequiring(ItemDefinition item)
        {
            return recipes.Where(r => r.Inputs.Any(i => i.Item == item));
        }

#if UNITY_EDITOR
        [Button("Auto-populate from project"), PropertyOrder(-1)]
        private void AutoPopulate()
        {
            recipes.Clear();
            var guids = UnityEditor.AssetDatabase.FindAssets("t:RecipeDefinition");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var recipe = UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
                if (recipe != null)
                {
                    recipes.Add(recipe);
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

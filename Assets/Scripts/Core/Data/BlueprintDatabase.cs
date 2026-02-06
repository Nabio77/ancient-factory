using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using AncientFactory.Core.Types;

namespace AncientFactory.Core.Data
{
    [CreateAssetMenu(fileName = "BlueprintDatabase", menuName = "Carbon World/Blueprint Database")]
    public class BlueprintDatabase : ScriptableObject
    {
        [SerializeField, ListDrawerSettings(ShowFoldout = false)]
        private List<BlueprintDefinition> blueprints = new();

        public IReadOnlyList<BlueprintDefinition> Blueprints => blueprints;

        public BlueprintDefinition GetByName(string blueprintName)
        {
            return blueprints.FirstOrDefault(b => b.BlueprintName == blueprintName);
        }

        public IEnumerable<BlueprintDefinition> GetStarterBlueprints()
        {
            return blueprints.Where(b => b.IsStarterCard);
        }

        public IEnumerable<BlueprintDefinition> GetUnlockableBlueprints()
        {
            return blueprints.Where(b => !b.IsStarterCard);
        }

        public IEnumerable<BlueprintDefinition> GetByType(BlueprintType type)
        {
            return blueprints.Where(b => b.Type == type);
        }

#if UNITY_EDITOR
        [Button("Auto-populate from project"), PropertyOrder(-1)]
        private void AutoPopulate()
        {
            blueprints.Clear();
            var guids = UnityEditor.AssetDatabase.FindAssets("t:BlueprintDefinition");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var blueprint = UnityEditor.AssetDatabase.LoadAssetAtPath<BlueprintDefinition>(path);
                if (blueprint != null)
                {
                    blueprints.Add(blueprint);
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
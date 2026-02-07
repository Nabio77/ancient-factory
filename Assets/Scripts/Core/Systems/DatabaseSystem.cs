using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using AncientFactory.Core.Data;

namespace AncientFactory.Core.Systems
{
    public class DatabaseSystem : MonoBehaviour
    {
        public static DatabaseSystem Instance { get; private set; }

        [Title("Data Assets")]
        [SerializeField, ListDrawerSettings(ShowFoldout = false)]
        private List<ItemDefinition> items = new();

        [SerializeField, ListDrawerSettings(ShowFoldout = false)]
        private List<BlueprintDefinition> blueprints = new();

        [SerializeField, Required]
        private WonderDefinition wonderDefinition;

        private Dictionary<string, ItemDefinition> _itemLookup;
        private Dictionary<string, BlueprintDefinition> _blueprintLookup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildLookups();
        }

        private void BuildLookups()
        {
            _itemLookup = new Dictionary<string, ItemDefinition>();
            foreach (var item in items)
            {
                if (item != null && !_itemLookup.ContainsKey(item.name))
                {
                    _itemLookup.Add(item.name, item);
                }
            }

            _blueprintLookup = new Dictionary<string, BlueprintDefinition>();
            foreach (var bp in blueprints)
            {
                if (bp != null && !_blueprintLookup.ContainsKey(bp.name))
                {
                    _blueprintLookup.Add(bp.name, bp);
                }
            }
        }

        public ItemDefinition GetItem(string id)
        {
            if (_itemLookup == null) BuildLookups();
            _itemLookup.TryGetValue(id, out var item);
            return item;
        }

        public BlueprintDefinition GetBlueprint(string id)
        {
            if (_blueprintLookup == null) BuildLookups();
            _blueprintLookup.TryGetValue(id, out var bp);
            return bp;
        }

        public WonderDefinition WonderDefinition => wonderDefinition;

        public List<WonderStage> GetWonderStages()
        {
            return wonderDefinition != null ? wonderDefinition.Stages : new List<WonderStage>();
        }

        public IEnumerable<ItemDefinition> GetAllItems() => items;
        public IEnumerable<BlueprintDefinition> GetAllBlueprints() => blueprints;

#if UNITY_EDITOR
        [Button("Populate from Project"), PropertyOrder(-1)]
        private void PopulateFromProject()
        {
            items.Clear();
            var itemGuids = UnityEditor.AssetDatabase.FindAssets("t:ItemDefinition");
            foreach (var guid in itemGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            blueprints.Clear();
            var bpGuids = UnityEditor.AssetDatabase.FindAssets("t:BlueprintDefinition");
            foreach (var guid in bpGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var bp = UnityEditor.AssetDatabase.LoadAssetAtPath<BlueprintDefinition>(path);
                if (bp != null)
                {
                    blueprints.Add(bp);
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

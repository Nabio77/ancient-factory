using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CarbonWorld.Core.Data
{
    [CreateAssetMenu(fileName = "TechTreeGraph", menuName = "Carbon World/Tech Tree Graph")]
    public class TechTreeGraph : ScriptableObject
    {
        [SerializeField, ListDrawerSettings(ShowFoldout = false)]
        private List<TechTreeNodeData> nodes = new();

        public IReadOnlyList<TechTreeNodeData> Nodes => nodes;

#if UNITY_EDITOR
        public void Clear()
        {
            nodes.Clear();
        }

        public void AddNode(TechTreeNodeData node)
        {
            nodes.Add(node);
        }
#endif
    }

    [Serializable]
    public class TechTreeNodeData
    {
        public string guid;
        public Vector2 position;

        [SerializeField, ShowIf("@item == null"), InlineEditor]
        public BlueprintDefinition blueprint;

        [SerializeField, ShowIf("@blueprint == null"), InlineEditor]
        public ItemDefinition item;

        public List<string> prerequisites = new();

        public TechTreeNodeData()
        {
            guid = Guid.NewGuid().ToString();
        }

        public string Name => blueprint != null ? blueprint.BlueprintName : (item != null ? item.ItemName : "Empty Node");
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;


namespace AncientFactory.Core.Data
{
    [CreateAssetMenu(fileName = "TechTreeGraph", menuName = "Ancient Factory/Tech Tree Graph")]
    public class TechTreeGraph : ScriptableObject
    {
        [SerializeField, HideInInspector]
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

        [SerializeField]
        public BlueprintDefinition blueprint;

        [SerializeField]
        public ItemDefinition item;

        public List<string> prerequisites = new();

        public TechTreeNodeData()
        {
            guid = Guid.NewGuid().ToString();
        }

        public string Name => blueprint != null ? blueprint.BlueprintName : (item != null ? item.ItemName : "Empty Node");
    }
}

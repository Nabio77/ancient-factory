using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarbonWorld.Core.Data
{
    [Serializable]
    public class BlueprintGraph
    {
        public List<BlueprintNode> nodes = new();
        public List<BlueprintConnection> connections = new();

        // Helper to find a node by ID
        public BlueprintNode GetNode(string id)
        {
            return nodes.Find(n => n.id == id);
        }
    }

    [Serializable]
    public class BlueprintNode
    {
        public string id;
        public BlueprintDefinition blueprint;
        public Vector2 position;

        public BlueprintNode(BlueprintDefinition bp, Vector2 pos)
        {
            id = System.Guid.NewGuid().ToString();
            blueprint = bp;
            position = pos;
        }
    }

    [Serializable]
    public class BlueprintConnection
    {
        public string fromNodeId;
        public int fromPortIndex; // Output port index
        public string toNodeId;
        public int toPortIndex;   // Input port index

        public BlueprintConnection(string fromId, int fromIdx, string toId, int toIdx)
        {
            fromNodeId = fromId;
            fromPortIndex = fromIdx;
            toNodeId = toId;
            toPortIndex = toIdx;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarbonWorld.Core.Data
{
    [Serializable]
    public class BlueprintGraph
    {
        public event Action OnGraphUpdated;

        public List<BlueprintNode> nodes = new();
        public List<BlueprintConnection> connections = new();
        public List<TileIONode> ioNodes = new();

        public void NotifyGraphUpdated()
        {
            OnGraphUpdated?.Invoke();
        }

        // Helper to find a node by ID
        public BlueprintNode GetNode(string id)
        {
            return nodes.Find(n => n.id == id);
        }

        // Helper to find an IO node by ID
        public TileIONode GetIONode(string id)
        {
            return ioNodes.Find(n => n.id == id);
        }

        // Check if a node ID belongs to an IO node
        public bool IsIONode(string id)
        {
            return id != null && id.StartsWith("tile_io_");
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

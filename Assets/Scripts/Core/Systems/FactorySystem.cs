using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using AncientFactory.Core.Data;
using AncientFactory.Features.Tiles;
using AncientFactory.Features.WorldMap;
using AncientFactory.Features.Factory;
using AncientFactory.Features.Settlement;

namespace AncientFactory.Core.Systems
{
    public class FactorySystem : MonoBehaviour
    {
        public static FactorySystem Instance { get; private set; }

        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [Title("Settings")]
        [SerializeField]
        private float tickInterval = 1f;

        private float _tickTimer;

        private FactoryInput _factoryInput;
        private FactoryStateMachine _stateMachine;
        private SettlementSupply _settlementSupply;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (worldMap == null)
            {
                worldMap = FindFirstObjectByType<WorldMap>();
            }

            _factoryInput = new FactoryInput(worldMap.TileData);
            _stateMachine = new FactoryStateMachine(_factoryInput, worldMap.TileData);
            _settlementSupply = new SettlementSupply(worldMap.TileData);
        }

        private void Update()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickInterval)
            {
                _tickTimer = 0f;
                ProcessFactoryTick();
            }
        }

        [Button("Process Tick")]
        public void ProcessFactoryTick()
        {
            // ProcessPowerTiles(); // Removed in favor of WorkforceSystem
            ProcessFactoryTiles();
            _settlementSupply.DistributeToAllSettlements();
            // WorkforceSystem recalculates on its own schedule or map changes, 
            // but we could force a sync if needed. For now, we assume it's reactive.
        }

        private void ProcessFactoryTiles()
        {
            foreach (var tile in worldMap.TileData.FactoryTiles)
            {
                ProcessFactoryTile(tile as BaseTile);
            }
        }

        private void ProcessFactoryTile(BaseTile tile)
        {
            if (tile is not IFactoryTile factoryTile) return;

            // Updated from IsPowered -> HasWorkers
            if (!factoryTile.HasWorkers)
                return;

            foreach (var node in factoryTile.Graph.nodes)
            {
                if (node.blueprint == null || !node.blueprint.IsProducer)
                    continue;

                _stateMachine.ProcessNode(factoryTile, node, tickInterval);
            }
        }

        [Button("Debug: Show All Production Status")]
        public void DebugShowProductionStatus()
        {
            Debug.Log("=== PRODUCTION SYSTEM DEBUG ===");

            foreach (var tile in worldMap.TileData.FactoryTiles)
            {
                if (tile is not FactoryTile factoryTile) continue;

                var pos = factoryTile.CellPosition;
                string typeName = factoryTile.Category.ToString();

                Debug.Log($"FactoryTile ({typeName}) at {pos}:");
                Debug.Log($"  - HasWorkers: {factoryTile.HasWorkers}");
                Debug.Log($"  - IO Nodes: {factoryTile.Graph.ioNodes.Count}");
                Debug.Log($"  - Blueprint Nodes: {factoryTile.Graph.nodes.Count}");
                Debug.Log($"  - Connections: {factoryTile.Graph.connections.Count}");

                foreach (var io in factoryTile.Graph.ioNodes)
                {
                    Debug.Log($"    IO: {io.type} - {io.id} - Item: {(io.availableItem.IsValid ? io.availableItem.Item.ItemName : "None")}");
                }

                Debug.Log($"  - OutputBuffer: {factoryTile.OutputBuffer.TotalItemCount} items");

                foreach (var state in factoryTile.GetAllProductionStates())
                {
                    Debug.Log($"    State: {state.NodeId} - {state.Status} ({state.Progress:P0})");
                }
            }
        }
    }
}

using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.WorldMap;
using CarbonWorld.Features.Factory;
using CarbonWorld.Features.Settlement;

namespace CarbonWorld.Core.Systems
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
            ProcessPowerTiles();
            ProcessFactoryTiles();
            _settlementSupply.DistributeToAllSettlements();
            if (PowerSystem.Instance != null) PowerSystem.Instance.RecalculatePower();
        }

        private void ProcessPowerTiles()
        {
            foreach (var powerTile in worldMap.TileData.PowerTiles)
            {
                foreach (var node in powerTile.Graph.nodes)
                {
                    if (node.blueprint == null || !node.blueprint.IsPowerGenerator)
                        continue;

                    // Solar (no inputs) is always active
                    if (node.blueprint.Inputs.Count == 0)
                    {
                        powerTile.SetGeneratorActive(node.id, true);
                        continue;
                    }

                    // Check and consume fuel
                    if (_factoryInput.TryResolveInputs(powerTile, node, node.blueprint.Inputs, simulateOnly: true))
                    {
                        _factoryInput.TryResolveInputs(powerTile, node, node.blueprint.Inputs, simulateOnly: false);
                        powerTile.SetGeneratorActive(node.id, true);
                    }
                    else
                    {
                        powerTile.SetGeneratorActive(node.id, false);
                    }
                }
            }
        }

        private void ProcessFactoryTiles()
        {
            foreach (var tile in worldMap.TileData.FactoryTiles)
            {
                if (tile is PowerTile) continue;
                ProcessFactoryTile(tile as BaseTile);
            }
        }

        private void ProcessFactoryTile(BaseTile tile)
        {
            if (tile is not IFactoryTile factoryTile) return;

            if (!factoryTile.IsPowered)
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

            foreach (var factoryTile in worldMap.TileData.FactoryTiles)
            {
                var pos = (factoryTile as BaseTile).CellPosition;
                string typeName = (factoryTile as FactoryTile)?.Category.ToString() ?? "Unknown";

                Debug.Log($"FactoryTile ({typeName}) at {pos}:");
                Debug.Log($"  - IsPowered: {factoryTile.IsPowered}");
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

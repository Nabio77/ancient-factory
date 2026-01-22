using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Data;
using CarbonWorld.Features.Grid;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.WorldMap;

namespace CarbonWorld.Core.Systems
{
    public class ItemFlowSystem : MonoBehaviour
    {
        public static ItemFlowSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [Title("Settings")]
        [SerializeField]
        private float tickInterval = 1f;

        private float _tickTimer;

        void Update()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickInterval)
            {
                _tickTimer = 0f;
                ProcessTick();
            }
        }

        [Button("Process Tick")]
        private void ProcessTick()
        {
            ProcessResourceTiles();
        }

        private void ProcessResourceTiles()
        {
            foreach (var kvp in worldMap.Grid.GetAllTiles())
            {
                if (kvp.Value is ResourceTile resourceTile)
                {
                    DistributeResource(resourceTile);
                }
            }
        }

        private void DistributeResource(ResourceTile resourceTile)
        {
            var neighbors = HexUtils.GetNeighbors(resourceTile.Coordinates);
            var output = resourceTile.GetOutput();

            foreach (var neighborCoord in neighbors)
            {
                var neighborTile = worldMap.Grid.GetTile(neighborCoord);
                if (neighborTile == null) continue;

                // Resources flow to production tiles and core
                if (neighborTile is ProductionTile || neighborTile is CoreTile)
                {
                    neighborTile.Inventory.Add(output);
                }
            }
        }
    }
}

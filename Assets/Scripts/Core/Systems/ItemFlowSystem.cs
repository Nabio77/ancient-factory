using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.WorldMap;
using CarbonWorld.Types;

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
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is ResourceTile resourceTile)
                {
                    DistributeResource(resourceTile);
                }
            }
        }

        private void DistributeResource(ResourceTile resourceTile)
        {
            var output = resourceTile.GetOutput();

            foreach (var neighborTile in worldMap.TileData.GetNeighbors(resourceTile.CellPosition))
            {
                // Resources flow to production tiles and core
                if (neighborTile.Type == TileType.Production || neighborTile.Type == TileType.Core)
                {
                    neighborTile.Inventory.Add(output);
                }
            }
        }
    }
}

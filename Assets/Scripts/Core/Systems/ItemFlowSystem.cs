using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.WorldMap;
using CarbonWorld.Core.Types;
using CarbonWorld.Features.Inventories;

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

            if (worldMap == null)
            {
                worldMap = FindFirstObjectByType<WorldMap>();
            }
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
                    ReplenishResourceInventory(resourceTile);
                }
            }
        }

        private void ReplenishResourceInventory(ResourceTile resourceTile)
        {
            var output = resourceTile.GetOutput();
            if (!output.IsValid)
                return;

            using (new InventoryBatch(resourceTile.Inventory, this, "ResourceReplenish"))
            {
                resourceTile.Inventory.Add(output);
            }
        }
    }
}

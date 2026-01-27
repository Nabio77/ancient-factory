using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Features.WorldMap;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Core.Systems
{
    public class PowerDistributionSystem : MonoBehaviour
    {
        public static PowerDistributionSystem Instance { get; private set; }

        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        private HashSet<Vector3Int> _poweredPositions = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Recalculates power distribution for all power tiles.
        /// Should be called when power tile graphs change.
        /// </summary>
        [Button("Recalculate Power")]
        public void RecalculatePower()
        {
            _poweredPositions.Clear();

            // Phase 1: Calculate power output for all power tiles
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is PowerTile powerTile)
                {
                    powerTile.CalculatePowerOutput();
                }
            }

            // Phase 2: Collect all powered positions
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is PowerTile powerTile && powerTile.TotalPowerOutput > 0)
                {
                    foreach (var pos in powerTile.GetPoweredPositions())
                    {
                        _poweredPositions.Add(pos);
                    }
                }
            }

            // Phase 3: Update IsPowered on all production tiles
            foreach (var tile in worldMap.TileData.GetAllTiles())
            {
                if (tile is ProductionTile productionTile)
                {
                    productionTile.IsPowered = _poweredPositions.Contains(tile.CellPosition);
                }
            }
        }

        public bool IsPositionPowered(Vector3Int position)
        {
            return _poweredPositions.Contains(position);
        }

        public IReadOnlyCollection<Vector3Int> GetAllPoweredPositions()
        {
            return _poweredPositions;
        }
    }
}

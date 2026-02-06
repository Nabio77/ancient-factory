using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using AncientFactory.Core.Data;
using AncientFactory.Features.Tiles;
using AncientFactory.Features.WorldMap;

namespace AncientFactory.Core.Systems
{
    public class TechTreeSystem : MonoBehaviour
    {
        public static TechTreeSystem Instance { get; private set; }

        public event Action<BlueprintDefinition> OnBlueprintUnlocked;

        [Title("References")]
        [SerializeField, Required]
        private WorldMap worldMap;

        [Title("Data")]
        [SerializeField, Required]
        private TechTreeGraph techTreeGraph;

        private HashSet<string> _unlockedGuids = new(); // Track by GUID
        private CoreTile _coreTile;

        public TechTreeGraph Graph => techTreeGraph;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (worldMap == null)
            {
                worldMap = FindFirstObjectByType<WorldMap>();
            }

            // Auto-unlock item nodes (they are starting points)
            if (techTreeGraph != null)
            {
                foreach (var node in techTreeGraph.Nodes)
                {
                    if (node.item != null)
                    {
                        _unlockedGuids.Add(node.guid);
                    }
                }
            }
        }

        private CoreTile GetCoreTile()
        {
            if (_coreTile == null && worldMap != null)
            {
                _coreTile = worldMap.TileData.GetAllTiles().OfType<CoreTile>().FirstOrDefault();
            }
            return _coreTile;
        }

        public long GetAvailablePoints()
        {
            var core = GetCoreTile();
            return core?.AccumulatedPoints ?? 0;
        }

        public bool IsBlueprintUnlocked(BlueprintDefinition blueprint)
        {
            if (blueprint == null) return false;
            // Starters are always unlocked
            if (blueprint.IsStarterCard) return true;
            if (techTreeGraph == null) return false;

            // Find node for this blueprint
            var node = techTreeGraph.Nodes.FirstOrDefault(n => n.blueprint == blueprint);
            // If not in tree, it's not unlocked (unless starter)
            if (node == null) return false;

            return _unlockedGuids.Contains(node.guid);
        }

        public bool IsGuidUnlocked(string guid)
        {
            return _unlockedGuids.Contains(guid);
        }

        public bool CanUnlock(TechTreeNodeData node)
        {
            if (node == null) return false;

            // Items are auto-unlocked, cannot be "unlocked" again
            if (node.item != null) return false;

            if (node.blueprint == null) return false;
            if (IsBlueprintUnlocked(node.blueprint)) return false;

            // Check prerequisites
            foreach (var prereqGuid in node.prerequisites)
            {
                if (!_unlockedGuids.Contains(prereqGuid)) return false;
            }

            // Check points
            return GetAvailablePoints() >= node.blueprint.UnlockCost;
        }

        public bool TryUnlock(TechTreeNodeData node)
        {
            if (!CanUnlock(node)) return false;

            var core = GetCoreTile();
            if (core == null) return false;

            // Deduct points
            core.AccumulatedPoints -= node.blueprint.UnlockCost;

            // Mark as unlocked
            _unlockedGuids.Add(node.guid);

            OnBlueprintUnlocked?.Invoke(node.blueprint);
            Debug.Log($"[TechTreeSystem] Unlocked: {node.blueprint.BlueprintName}");

            // Show notification
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowNotification(
                    "Blueprint Unlocked",
                    $"{node.blueprint.BlueprintName} is now available!",
                    AncientFactory.Core.Types.NotificationType.Info
                );
            }

            return true;
        }
    }
}

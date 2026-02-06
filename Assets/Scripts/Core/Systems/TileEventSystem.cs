using System;
using System.Collections.Generic;
using UnityEngine;

namespace AncientFactory.Core.Systems
{
    public class TileEventSystem : MonoBehaviour
    {
        public static TileEventSystem Instance { get; private set; }

        public event Action<Vector3Int, TileChangeType> TileChanged;

        public enum TileChangeType
        {
            InventoryChanged,
            GraphUpdated,
            PowerChanged,
            Replaced
        }

        private readonly HashSet<(Vector3Int, TileChangeType)> _dirtyTiles = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void MarkDirty(Vector3Int position, TileChangeType type)
        {
            _dirtyTiles.Add((position, type));
        }

        void LateUpdate()
        {
            if (_dirtyTiles.Count == 0) return;

            foreach (var (pos, type) in _dirtyTiles)
            {
                TileChanged?.Invoke(pos, type);
            }
            _dirtyTiles.Clear();
        }
    }
}

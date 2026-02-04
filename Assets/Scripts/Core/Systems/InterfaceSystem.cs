using System;
using UnityEngine;
using Sirenix.OdinInspector;
using CarbonWorld.Features.WorldMap;

namespace CarbonWorld.Core.Systems
{
    public enum InterfaceState
    {
        Gameplay,
        ProductionEditor,
        TechTree,
        Menu
    }

    public class InterfaceSystem : MonoBehaviour
    {
        public static InterfaceSystem Instance { get; private set; }

        [Title("References")]
        [SerializeField, Required]
        private WorldMapCamera worldMapCamera;

        [SerializeField, Required]
        private TileSelector tileSelector;

        public event Action<InterfaceState> OnStateChanged;

        private InterfaceState _currentState = InterfaceState.Gameplay;
        public InterfaceState CurrentState => _currentState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetState(InterfaceState newState)
        {
            if (_currentState == newState) return;

            var oldState = _currentState;

            // When leaving Gameplay, save camera and block input
            if (oldState == InterfaceState.Gameplay && newState != InterfaceState.Gameplay)
            {
                worldMapCamera.SavePosition();
                worldMapCamera.InputEnabled = false;
                tileSelector.IsInputBlocked = true;
            }

            // When returning to Gameplay, restore camera and enable input
            if (oldState != InterfaceState.Gameplay && newState == InterfaceState.Gameplay)
            {
                worldMapCamera.RestorePosition();
                worldMapCamera.InputEnabled = true;
                tileSelector.IsInputBlocked = false;
            }

            _currentState = newState;

            Debug.Log($"[InterfaceSystem] State changed: {oldState} -> {newState}");
            OnStateChanged?.Invoke(newState);
        }

        public bool IsState(InterfaceState state) => _currentState == state;
    }
}

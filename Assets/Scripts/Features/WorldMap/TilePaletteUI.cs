using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Core.Types;
using CarbonWorld.Features.Production;

namespace CarbonWorld.Features.WorldMap
{
    public class TilePaletteUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required]
        private UIDocument uiDocument;

        [SerializeField, Required]
        private TileSelector tileSelector;

        [SerializeField, Required]
        private WorldMap worldMap;

        [SerializeField]
        private ProductionGraphEditor graphEditor;

        private VisualElement _root;
        private Button _btnPower;
        private Button _btnNature;
        private Button _btnTransport;
        private Button _btnProduction;

        private bool _isGraphEditorOpen;
        private TileType? _activeBrush = null;

        void Awake()
        {
            _root = uiDocument.rootVisualElement;
            
            var container = _root.Q<VisualElement>("palette-container");
            if (container != null)
            {
                container.RegisterCallback<MouseEnterEvent>(evt => 
                {
                    if (tileSelector != null) tileSelector.IsInputBlocked = true;
                });
                container.RegisterCallback<MouseLeaveEvent>(evt => 
                {
                    if (tileSelector != null) tileSelector.IsInputBlocked = false;
                });
            }

            _btnPower = _root.Q<Button>("btn-power");
            _btnNature = _root.Q<Button>("btn-nature");
            _btnTransport = _root.Q<Button>("btn-transport");
            _btnProduction = _root.Q<Button>("btn-production");

            if (_btnPower != null) _btnPower.clicked += () => ToggleBrush(TileType.Power);
            if (_btnNature != null) _btnNature.clicked += () => ToggleBrush(TileType.Nature);
            if (_btnTransport != null) _btnTransport.clicked += () => ToggleBrush(TileType.Transport);
            if (_btnProduction != null) _btnProduction.clicked += () => ToggleBrush(TileType.Production);
        }

        void OnEnable()
        {
            if (tileSelector != null)
            {
                tileSelector.OnPlacementClick += OnPlacementClick;
            }

            if (graphEditor != null)
            {
                graphEditor.OnEditorOpened += Hide;
                graphEditor.OnEditorClosed += Show;
            }

            RefreshVisibility();
        }

        void OnDisable()
        {
            if (tileSelector != null)
            {
                tileSelector.OnPlacementClick -= OnPlacementClick;
                tileSelector.IsInputBlocked = false;
            }

            if (graphEditor != null)
            {
                graphEditor.OnEditorOpened -= Hide;
                graphEditor.OnEditorClosed -= Show;
            }
        }

        private void Show()
        {
            _isGraphEditorOpen = false;
            RefreshVisibility();
        }

        private void Hide()
        {
            _isGraphEditorOpen = true;
            // Also cancel any active brush when opening editor (though technically editor opening usually implies brush is off)
            CancelBrush();
            if (tileSelector != null) tileSelector.IsInputBlocked = false;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (_root == null) return;
            bool shouldShow = !_isGraphEditorOpen;
            _root.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ToggleBrush(TileType type)
        {
            if (_activeBrush == type)
            {
                CancelBrush();
            }
            else
            {
                SetBrush(type);
            }
        }

        private void SetBrush(TileType type)
        {
            _activeBrush = type;
            tileSelector.IsPlacementMode = true;
            UpdateButtons();
        }

        private void CancelBrush()
        {
            _activeBrush = null;
            tileSelector.IsPlacementMode = false;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
             SetButtonState(_btnPower, TileType.Power);
             SetButtonState(_btnNature, TileType.Nature);
             SetButtonState(_btnTransport, TileType.Transport);
             SetButtonState(_btnProduction, TileType.Production);
        }

        private void SetButtonState(Button btn, TileType type)
        {
            if (btn == null) return;
            
            bool isActive = _activeBrush == type;
            if (isActive)
            {
                btn.AddToClassList("selected");
            }
            else
            {
                btn.RemoveFromClassList("selected");
            }
        }

        private void OnPlacementClick(BaseTile tile)
        {
            if (_activeBrush == null) return;
            if (tile == null) return;

            // Only allow replacing mutable tiles
            if (IsMutable(tile.Type))
            {
                // Don't replace if it's already that type
                if (tile.Type != _activeBrush.Value)
                {
                    worldMap.ReplaceTile(tile.CellPosition, _activeBrush.Value);
                    // Optional: Play sound or particle effect here
                }
            }
        }

        private bool IsMutable(TileType type)
        {
            return type == TileType.Production || 
                   type == TileType.Power || 
                   type == TileType.Nature || 
                   type == TileType.Transport;
        }
    }
}
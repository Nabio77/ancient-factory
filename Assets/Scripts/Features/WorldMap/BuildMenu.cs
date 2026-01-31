using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Core.Types;
using CarbonWorld.Features.Production;

namespace CarbonWorld.Features.WorldMap
{
    public class BuildMenu : MonoBehaviour
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
        private Button _btnFood;

        private bool _isGraphEditorOpen;
        private TileType? _activeBrush = null;

        void Awake()
        {
            _root = uiDocument.rootVisualElement;
            
            var container = _root.Q<VisualElement>("build-menu-container");
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
            _btnFood = _root.Q<Button>("btn-food");

            if (_btnPower != null) _btnPower.clicked += () => ToggleBrush(TileType.Power);
            if (_btnNature != null) _btnNature.clicked += () => ToggleBrush(TileType.Nature);
            if (_btnTransport != null) _btnTransport.clicked += () => ToggleBrush(TileType.Transport);
            if (_btnProduction != null) _btnProduction.clicked += () => ToggleBrush(TileType.Production);
            if (_btnFood != null) _btnFood.clicked += () => ToggleBrush(TileType.Food);
        }

        void OnEnable()
        {
            if (tileSelector != null)
            {
                tileSelector.OnPlacementClick += OnPlacementClick;
                tileSelector.OnPlacementCellClick += OnPlacementCellClick;
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
                tileSelector.OnPlacementCellClick -= OnPlacementCellClick;
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
            tileSelector.PlacementTileType = type;
            UpdateButtons();
        }

        private void CancelBrush()
        {
            _activeBrush = null;
            tileSelector.IsPlacementMode = false;
            tileSelector.PlacementTileType = null;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
             SetButtonState(_btnPower, TileType.Power);
             SetButtonState(_btnNature, TileType.Nature);
             SetButtonState(_btnTransport, TileType.Transport);
             SetButtonState(_btnProduction, TileType.Production);
             SetButtonState(_btnFood, TileType.Food);
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
                }
            }
        }

        private void OnPlacementCellClick(Vector3Int cellPos)
        {
            if (_activeBrush == null) return;

            // Place new tile on empty cell
            worldMap.AddTile(cellPos, _activeBrush.Value);
        }

        private bool IsMutable(TileType type)
        {
            return type == TileType.Production || 
                   type == TileType.Power || 
                   type == TileType.Nature || 
                   type == TileType.Transport ||
                   type == TileType.Food;
        }
    }
}
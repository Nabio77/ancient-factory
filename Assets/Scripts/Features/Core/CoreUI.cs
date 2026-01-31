using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using CarbonWorld.Features.Tiles;
using CarbonWorld.Core.Data;
using CarbonWorld.Core.Events;
using CarbonWorld.Features.WorldMap;

namespace CarbonWorld.UI
{
    public class CoreUI : MonoBehaviour
    {
        [SerializeField, Required]
        private WorldMap _worldMap;

        [Title("References")]
        [SerializeField, Required]
        private UIDocument document;

        [Title("UI Settings")]
        [SerializeField]
        private VisualTreeAsset listItemTemplate;

        private VisualElement _root;
        private Label _pointLabel;
        private VisualElement _historyContainer;
        private CoreTile _coreTile;

        void Start()
        {
            _root = document.rootVisualElement.Q<VisualElement>("root");
            // Ensure visible by default
            _root.style.display = DisplayStyle.Flex;

            _pointLabel = _root.Q<Label>("point-label");
            _historyContainer = _root.Q<VisualElement>("history-list");
            
            FindCoreTile();
        }

        void Update()
        {
            if (_coreTile == null)
            {
                FindCoreTile();
            }

            if (_coreTile != null)
            {
                UpdateUI();
            }
        }

        private void FindCoreTile()
        {
            if (_worldMap != null && _worldMap.TileData != null)
            {
                _coreTile = _worldMap.TileData.GetAllTiles().OfType<CoreTile>().FirstOrDefault();
                if (_coreTile != null)
                {
                     UpdateUI();
                     UpdateHistoryList();
                }
            }
        }

        private void UpdateUI()
        {
            if (_coreTile == null) return;
            if (_pointLabel != null)
            {
                _pointLabel.text = $"{_coreTile.AccumulatedPoints:N0} PTS";
            }
            
            UpdateHistoryList(); 
        }

        private void UpdateHistoryList()
        {
            if (_coreTile == null || _historyContainer == null) return;

            _historyContainer.Clear();

            var items = _coreTile.CollectedItems.OrderByDescending(kvp => kvp.Value);

            foreach (var kvp in items)
            {
                var item = kvp.Key;
                var count = kvp.Value;

                VisualElement entry;
                if (listItemTemplate != null)
                {
                    entry = listItemTemplate.Instantiate();
                }
                else
                {
                    // Minimal fallback
                    entry = new Label($"{item.ItemName}: {count}");
                    entry.AddToClassList("history-item");
                }
                
                // Populate data if template structure matches
                var icon = entry.Q("icon");
                if (icon != null) icon.style.backgroundImage = new StyleBackground(item.Icon);

                var name = entry.Q<Label>("name");
                if (name != null) name.text = item.ItemName;

                var countLbl = entry.Q<Label>("count");
                if (countLbl != null) countLbl.text = $"{count:N0}";

                _historyContainer.Add(entry);
            }
        }
    }
}

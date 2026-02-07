using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using AncientFactory.Features.Tiles;
using AncientFactory.Core.Data;
using AncientFactory.Core.Events;
using AncientFactory.Features.WorldMap;
using AncientFactory.Features.Factory;
using AncientFactory.Core.Systems;

namespace AncientFactory.UI
{
    public class CoreUI : MonoBehaviour
    {
        [SerializeField, Required]
        private WorldMap _worldMap;

        [Title("References")]
        [SerializeField, Required]
        private UIDocument document;

        [SerializeField]
        private FactoryGraphEditor graphEditor;

        [Title("UI Settings")]
        [SerializeField]
        private VisualTreeAsset listItemTemplate;

        private VisualElement _root;
        private Label _pointLabel;
        private Label _wonderStageLabel;
        private VisualElement _historyContainer; // Using this for Wonder Requirements now
        private CoreTile _coreTile;

        void Awake()
        {
            if (document == null) return;

            _root = document.rootVisualElement.Q<VisualElement>("root");
            // Ensure visible by default, unless editor manages it
            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
                _pointLabel = _root.Q<Label>("point-label");
                _wonderStageLabel = _root.Q<Label>("wonder-label"); // Assuming we might add this in UXML, or repurpose an existing label
                _historyContainer = _root.Q<VisualElement>("history-list");
            }
        }

        void OnEnable()
        {
            if (graphEditor != null)
            {
                graphEditor.OnEditorOpened += Hide;
                graphEditor.OnEditorClosed += Show;
            }
        }

        void OnDisable()
        {
            if (graphEditor != null)
            {
                graphEditor.OnEditorOpened -= Hide;
                graphEditor.OnEditorClosed -= Show;
            }

            if (WonderSystem.Instance != null)
            {
                WonderSystem.Instance.OnWonderProgressUpdated -= UpdateUI;
                WonderSystem.Instance.OnWonderStageChanged -= UpdateUI;
                WonderSystem.Instance.OnWonderCompleted -= UpdateUI;
            }
        }

        void Start()
        {
            FindCoreTile();
            if (WonderSystem.Instance != null)
            {
                WonderSystem.Instance.OnWonderProgressUpdated += UpdateUI;
                WonderSystem.Instance.OnWonderStageChanged += UpdateUI;
                WonderSystem.Instance.OnWonderCompleted += UpdateUI;
            }
            UpdateUI();
        }

        private void Show()
        {
            if (_root != null) _root.style.display = DisplayStyle.Flex;
        }

        private void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        void Update()
        {
            if (_coreTile == null)
            {
                FindCoreTile();
            }
        }

        private void FindCoreTile()
        {
            if (_worldMap != null && _worldMap.TileData != null)
            {
                var newTile = _worldMap.TileData.GetAllTiles().OfType<CoreTile>().FirstOrDefault();
                if (newTile != _coreTile)
                {
                    _coreTile = newTile;
                    UpdateUI();
                }
            }
        }

        private void UpdateUI()
        {
            // Update Points (Still from CoreTile)
            if (_coreTile != null && _pointLabel != null)
            {
                _pointLabel.text = $"{_coreTile.AccumulatedPoints:N0} PTS";
            }

            // Update Wonder (From WonderSystem)
            var wonder = WonderSystem.Instance;
            if (wonder == null) return;

            if (_wonderStageLabel != null)
            {
                if (wonder.IsWonderCompleted)
                    _wonderStageLabel.text = "Wonder Completed!";
                else if (wonder.CurrentStage.HasValue)
                    _wonderStageLabel.text = $"Project: {wonder.CurrentStage.Value.StageName}";
                else
                    _wonderStageLabel.text = "No Active Project";
            }

            UpdateWonderList();
        }

        private void UpdateWonderList()
        {
            if (_historyContainer == null || WonderSystem.Instance == null) return;

            _historyContainer.Clear();
            var wonder = WonderSystem.Instance;

            if (wonder.IsWonderCompleted)
            {
                var entry = new Label("Architecture Complete.");
                entry.AddToClassList("history-item");
                _historyContainer.Add(entry);
                return;
            }

            var stage = wonder.CurrentStage;
            if (stage.HasValue)
            {
                foreach (var req in stage.Value.Requirements)
                {
                    int current = wonder.StageProgress.ContainsKey(req.Item) ? wonder.StageProgress[req.Item] : 0;
                    int target = req.Amount;

                    VisualElement entry;
                    if (listItemTemplate != null)
                    {
                        entry = listItemTemplate.Instantiate();
                    }
                    else
                    {
                        // Minimal fallback
                        entry = new Label($"{req.Item.ItemName}: {current}/{target}");
                        entry.AddToClassList("history-item");
                    }

                    // Populate data
                    var icon = entry.Q("icon");
                    if (icon != null) icon.style.backgroundImage = new StyleBackground(req.Item.Icon);

                    var name = entry.Q<Label>("name");
                    if (name != null) name.text = req.Item.ItemName;

                    var countLbl = entry.Q<Label>("count");
                    if (countLbl != null)
                    {
                        countLbl.text = $"{current}/{target}";
                        if (current >= target) countLbl.style.color = new StyleColor(Color.green);
                    }

                    _historyContainer.Add(entry);
                }
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using AncientFactory.Core.Systems;
using AncientFactory.Core.Types;
using AncientFactory.Features.Factory;

namespace AncientFactory.Features.Divine
{
    public class DivineDispleasureUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required]
        private UIDocument uiDocument;

        [SerializeField, Required]
        private DivineDispleasureSystem displeasureSystem;

        [SerializeField]
        private FactoryGraphEditor graphEditor;

        private VisualElement _root;
        private Label _totalDispleasureLabel;
        private Label _netDispleasureLabel;
        private Label _generatedLabel;
        private Label _favorLabel;
        private VisualElement _thresholdIndicator;
        private Label _thresholdLabel;
        private VisualElement _progressBarFill;

        void Awake()
        {
            _root = uiDocument.rootVisualElement;
            _totalDispleasureLabel = _root.Q<Label>("total-displeasure");
            _netDispleasureLabel = _root.Q<Label>("net-displeasure");
            _generatedLabel = _root.Q<Label>("displeasure-generated");
            _favorLabel = _root.Q<Label>("favor-generated");
            _thresholdIndicator = _root.Q<VisualElement>("threshold-indicator");
            _thresholdLabel = _root.Q<Label>("threshold-name");
            _progressBarFill = _root.Q<VisualElement>("displeasure-progress-fill");
        }

        void OnEnable()
        {
            if (displeasureSystem != null)
            {
                displeasureSystem.OnDispleasureUpdated += OnDispleasureUpdated;
                displeasureSystem.OnDivineStateChanged += OnDivineStateChanged;
                RefreshUI();
            }

            if (graphEditor != null)
            {
                graphEditor.OnEditorOpened += Hide;
                graphEditor.OnEditorClosed += Show;
            }
        }

        void OnDisable()
        {
            if (displeasureSystem != null)
            {
                displeasureSystem.OnDispleasureUpdated -= OnDispleasureUpdated;
                displeasureSystem.OnDivineStateChanged -= OnDivineStateChanged;
            }

            if (graphEditor != null)
            {
                graphEditor.OnEditorOpened -= Hide;
                graphEditor.OnEditorClosed -= Show;
            }
        }

        private void Show()
        {
            _root.style.display = DisplayStyle.Flex;
        }

        private void Hide()
        {
            _root.style.display = DisplayStyle.None;
        }

        private void OnDispleasureUpdated(int total, int generated, int favor)
        {
            if (_totalDispleasureLabel != null)
                _totalDispleasureLabel.text = total.ToString();

            int net = generated - favor;
            if (_netDispleasureLabel != null)
            {
                _netDispleasureLabel.text = net >= 0 ? $"+{net}" : net.ToString();
                _netDispleasureLabel.RemoveFromClassList("positive");
                _netDispleasureLabel.RemoveFromClassList("negative");
                _netDispleasureLabel.AddToClassList(net >= 0 ? "positive" : "negative");
            }

            if (_generatedLabel != null)
                _generatedLabel.text = generated.ToString();

            if (_favorLabel != null)
                _favorLabel.text = favor.ToString();

            if (_progressBarFill != null)
            {
                float progress = Mathf.Clamp01(total / 500f);
                _progressBarFill.style.width = Length.Percent(progress * 100);
            }

            UpdateDivineDisplay(displeasureSystem.CurrentState);
        }

        private void OnDivineStateChanged(DivineFavorState state)
        {
            UpdateDivineDisplay(state);
        }

        private void RefreshUI()
        {
            if (_totalDispleasureLabel != null)
                _totalDispleasureLabel.text = displeasureSystem.TotalDispleasure.ToString();

            int net = displeasureSystem.NetDispleasurePerTick;
            if (_netDispleasureLabel != null)
            {
                _netDispleasureLabel.text = net >= 0 ? $"+{net}" : net.ToString();
                _netDispleasureLabel.RemoveFromClassList("positive");
                _netDispleasureLabel.RemoveFromClassList("negative");
                _netDispleasureLabel.AddToClassList(net >= 0 ? "positive" : "negative");
            }

            if (_generatedLabel != null)
                _generatedLabel.text = displeasureSystem.DispleasureGeneratedLastTick.ToString();

            if (_favorLabel != null)
                _favorLabel.text = displeasureSystem.FavorGeneratedLastTick.ToString();

            if (_progressBarFill != null)
            {
                float progress = Mathf.Clamp01(displeasureSystem.TotalDispleasure / 500f);
                _progressBarFill.style.width = Length.Percent(progress * 100);
            }

            UpdateDivineDisplay(displeasureSystem.CurrentState);
        }

        private void UpdateDivineDisplay(DivineFavorState state)
        {
            if (_thresholdIndicator == null) return;

            _thresholdIndicator.RemoveFromClassList("blessed");
            _thresholdIndicator.RemoveFromClassList("favored");
            _thresholdIndicator.RemoveFromClassList("tolerated");
            _thresholdIndicator.RemoveFromClassList("displeased");
            _thresholdIndicator.RemoveFromClassList("wrathful");
            _thresholdIndicator.RemoveFromClassList("forsaken");

            if (_thresholdLabel != null)
                _thresholdLabel.text = GetStateDisplayName(state);

            string stateClass = state switch
            {
                DivineFavorState.Blessed => "blessed",
                DivineFavorState.Favored => "favored",
                DivineFavorState.Tolerated => "tolerated",
                DivineFavorState.Displeased => "displeased",
                DivineFavorState.Wrathful => "wrathful",
                DivineFavorState.Forsaken => "forsaken",
                _ => "tolerated"
            };

            _thresholdIndicator.AddToClassList(stateClass);
        }

        private string GetStateDisplayName(DivineFavorState state)
        {
            return state switch
            {
                DivineFavorState.Blessed => "Blessed",
                DivineFavorState.Favored => "Favored",
                DivineFavorState.Tolerated => "Tolerated",
                DivineFavorState.Displeased => "Displeased",
                DivineFavorState.Wrathful => "Wrathful",
                DivineFavorState.Forsaken => "Forsaken",
                _ => "Unknown"
            };
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Systems;
using CarbonWorld.Features.Production;

namespace CarbonWorld.Features.Carbon
{
    public class CarbonUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required]
        private UIDocument uiDocument;

        [SerializeField, Required]
        private CarbonSystem carbonSystem;

        [SerializeField]
        private ProductionGraphEditor graphEditor;

        private VisualElement _root;
        private Label _totalCarbonLabel;
        private Label _netCarbonLabel;
        private Label _emittedLabel;
        private Label _absorbedLabel;
        private VisualElement _thresholdIndicator;
        private Label _thresholdLabel;
        private VisualElement _progressBarFill;

        void Awake()
        {
            _root = uiDocument.rootVisualElement;
            _totalCarbonLabel = _root.Q<Label>("total-carbon");
            _netCarbonLabel = _root.Q<Label>("net-carbon");
            _emittedLabel = _root.Q<Label>("carbon-emitted");
            _absorbedLabel = _root.Q<Label>("carbon-absorbed");
            _thresholdIndicator = _root.Q<VisualElement>("threshold-indicator");
            _thresholdLabel = _root.Q<Label>("threshold-name");
            _progressBarFill = _root.Q<VisualElement>("carbon-progress-fill");
        }

        void OnEnable()
        {
            if (carbonSystem != null)
            {
                carbonSystem.OnCarbonUpdated += OnCarbonUpdated;
                carbonSystem.OnClimateStateChanged += OnClimateStateChanged;
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
            if (carbonSystem != null)
            {
                carbonSystem.OnCarbonUpdated -= OnCarbonUpdated;
                carbonSystem.OnClimateStateChanged -= OnClimateStateChanged;
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

        private void OnCarbonUpdated(int total, int emitted, int absorbed)
        {
            RefreshUI();
        }

        private void OnClimateStateChanged(string state)
        {
            UpdateClimateDisplay(state);
        }

        private void RefreshUI()
        {
            _totalCarbonLabel.text = carbonSystem.TotalCarbon.ToString();

            int net = carbonSystem.NetCarbonPerTick;
            _netCarbonLabel.text = net >= 0 ? $"+{net}" : net.ToString();
            _netCarbonLabel.RemoveFromClassList("positive");
            _netCarbonLabel.RemoveFromClassList("negative");
            _netCarbonLabel.AddToClassList(net >= 0 ? "positive" : "negative");

            _emittedLabel.text = carbonSystem.CarbonEmittedLastTick.ToString();
            _absorbedLabel.text = carbonSystem.CarbonAbsorbedLastTick.ToString();

            if (_progressBarFill != null)
            {
                float progress = Mathf.Clamp01(carbonSystem.TotalCarbon / 500f);
                _progressBarFill.style.width = Length.Percent(progress * 100);
            }

            UpdateClimateDisplay(carbonSystem.CurrentClimateState);
        }

        private void UpdateClimateDisplay(string state)
        {
            _thresholdIndicator.RemoveFromClassList("safe");
            _thresholdIndicator.RemoveFromClassList("warning");
            _thresholdIndicator.RemoveFromClassList("danger");

            _thresholdLabel.text = state;

            switch (state)
            {
                case "Catastrophic":
                    _thresholdIndicator.AddToClassList("danger");
                    break;
                case "Critical":
                case "Warning":
                    _thresholdIndicator.AddToClassList("warning");
                    break;
                default:
                    _thresholdIndicator.AddToClassList("safe");
                    break;
            }
        }
    }
}

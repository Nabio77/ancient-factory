using System;
using System.Collections.Generic;
using UnityEngine;
using AncientFactory.Core.Data;
using Sirenix.OdinInspector;

namespace AncientFactory.Core.Systems
{
    public class WonderSystem : MonoBehaviour
    {
        public static WonderSystem Instance { get; private set; }

        public event Action OnWonderStageChanged;
        public event Action OnWonderProgressUpdated;
        public event Action OnWonderCompleted;

        [Title("Configuration")]
        [SerializeField, Required]
        private WonderDefinition wonderDefinition;

        // State
        public int CurrentStageIndex { get; private set; } = 0;
        public Dictionary<ItemDefinition, int> StageProgress { get; private set; } = new();

        public bool IsWonderCompleted => wonderDefinition != null && CurrentStageIndex >= wonderDefinition.Stages.Count;

        public WonderStage? CurrentStage
        {
            get
            {
                if (wonderDefinition == null || IsWonderCompleted || wonderDefinition.Stages.Count == 0)
                    return null;
                return wonderDefinition.Stages[CurrentStageIndex];
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Fallback: If created at runtime, try to get definition from DatabaseSystem
            if (wonderDefinition == null)
            {
                var db = AncientFactory.Core.Systems.DatabaseSystem.Instance;
                if (db != null)
                {
                    // Assuming DatabaseSystem exposes it or we can fetch it. 
                    // We added GetWonderStages(), but not the definition itself. 
                    // Let's rely on GetWonderStages logic, referencing the definition?
                    // Or better, let's just make DatabaseSystem expose the definition object or public getter.

                    // Actually, let's keep it simple: DatabaseSystem has it. 
                    // But we can't access private field.
                    // Let's modify DatabaseSystem to expose it, or just use Resources.Load if we had a path.
                    // But for now, let's assume we can change the field in DatabaseSystem to public getter.
                }
            }
        }

        public void AddContribution(ItemDefinition item, int quantity, out int acceptedAmount)
        {
            acceptedAmount = 0;

            if (item == null || quantity <= 0 || IsWonderCompleted || wonderDefinition == null)
            {
                return;
            }

            var stage = wonderDefinition.Stages[CurrentStageIndex];
            var req = stage.Requirements.Find(x => x.Item == item);

            if (req.IsValid)
            {
                int currentProgress = StageProgress.ContainsKey(item) ? StageProgress[item] : 0;
                int needed = req.Amount - currentProgress;

                if (needed > 0)
                {
                    int contribution = Mathf.Min(quantity, needed);

                    if (StageProgress.ContainsKey(item))
                        StageProgress[item] += contribution;
                    else
                        StageProgress[item] = contribution;

                    acceptedAmount = contribution;
                    OnWonderProgressUpdated?.Invoke();
                    CheckStageCompletion();
                }
            }
        }

        private void CheckStageCompletion()
        {
            if (IsWonderCompleted || wonderDefinition == null) return;

            var stage = wonderDefinition.Stages[CurrentStageIndex];
            bool complete = true;

            foreach (var req in stage.Requirements)
            {
                int current = StageProgress.ContainsKey(req.Item) ? StageProgress[req.Item] : 0;
                if (current < req.Amount)
                {
                    complete = false;
                    break;
                }
            }

            if (complete)
            {
                CurrentStageIndex++;
                StageProgress.Clear();
                OnWonderStageChanged?.Invoke();
                Debug.Log($"[WonderSystem] Wonder Stage Completed! Moving to Stage {CurrentStageIndex}");

                if (IsWonderCompleted)
                {
                    OnWonderCompleted?.Invoke();
                    Debug.Log("[WonderSystem] WONDER COMPLETED! VICTORY!");
                }
            }
        }

        // Debug method to force set definition if needed (e.g. from tests or editor scripts)
        public void SetDefinition(WonderDefinition def)
        {
            wonderDefinition = def;
        }

#if UNITY_EDITOR
        [Button("Debug: Complete Current Stage")]
        private void DebugCompleteStage()
        {
            if (CurrentStage.HasValue)
            {
                // Just force index increment
                CurrentStageIndex++;
                StageProgress.Clear();
                OnWonderStageChanged?.Invoke();
            }
        }
#endif
    }
}

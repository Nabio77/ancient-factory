using System;

namespace AncientFactory.Core.Data
{
    public enum ProductionStatus
    {
        Idle,           // Waiting for inputs or power
        Producing,      // Currently producing
        OutputReady     // Production complete, output waiting to be transferred
    }

    [Serializable]
    public class BlueprintProductionState
    {
        public string NodeId;
        public ProductionStatus Status;
        public float ElapsedTime;
        public float TotalTime;
        public ItemStack PendingOutput;

        public BlueprintProductionState(string nodeId)
        {
            NodeId = nodeId;
            Status = ProductionStatus.Idle;
            ElapsedTime = 0f;
            TotalTime = 0f;
            PendingOutput = ItemStack.Empty;
        }

        public float Progress => TotalTime > 0 ? ElapsedTime / TotalTime : 0f;

        public void Reset()
        {
            Status = ProductionStatus.Idle;
            ElapsedTime = 0f;
            TotalTime = 0f;
            PendingOutput = ItemStack.Empty;
        }
    }
}

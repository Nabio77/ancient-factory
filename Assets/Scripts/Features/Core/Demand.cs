using System;
using UnityEngine;
using CarbonWorld.Core.Data;

namespace CarbonWorld.Features.Core
{
    [Serializable]
    public class Demand
    {
        [SerializeField] private ItemDefinition _item;
        [SerializeField] private int _requiredAmount;
        [SerializeField] private int _currentAmount;
        [SerializeField] private DemandState _state;

        public ItemDefinition Item => _item;
        public int RequiredAmount => _requiredAmount;
        public int CurrentAmount => _currentAmount;
        public DemandState State => _state;
        public float Progress => _requiredAmount > 0 ? (float)_currentAmount / _requiredAmount : 0f;
        public bool IsFulfilled => _currentAmount >= _requiredAmount;
        public int RemainingAmount => Mathf.Max(0, _requiredAmount - _currentAmount);

        public Demand(ItemDefinition item, int requiredAmount)
        {
            _item = item;
            _requiredAmount = requiredAmount;
            _currentAmount = 0;
            _state = DemandState.Active;
        }

        public int Contribute(int amount)
        {
            if (_state != DemandState.Active) return 0;

            int toAdd = Mathf.Min(amount, RemainingAmount);
            _currentAmount += toAdd;

            if (IsFulfilled)
            {
                _state = DemandState.Fulfilled;
            }

            return toAdd;
        }

        public void Cancel()
        {
            _state = DemandState.Cancelled;
        }
    }

    public enum DemandState
    {
        Active,
        Fulfilled,
        Cancelled
    }
}

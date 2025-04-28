namespace Playground.Components
{
    using System;
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    public class BaseResource : IResource
    {
        private readonly float _baseRecoveryAmount;
        private readonly float _baseMaximumAmount;
        private readonly IRecoveryRule _recoveryRule;
        public BaseResource(Parameter parameter, ResourceType type, float recoveryAmount, float maximumAmount, IRecoveryRule rule)
        {
            _baseRecoveryAmount = recoveryAmount;
            _baseMaximumAmount = maximumAmount;
            Parameter = parameter;
            Type = type;
            _recoveryRule = rule;
            UpdateResources();
        }

        public event Action<float>? CurrentChanges, MaximumChanges;

        public Parameter Parameter { get; }
        public ResourceType Type { get; }
        public float Current { get; private set; }
        public float RecoveryAmount { get; set; }
        public float MaximumAmount { get; set; }

        public void HandleRecoveryEvent(RecoveryEventContext context)
        {
            if (_recoveryRule.ShouldRecover(context))
            {
                GD.Print($"ShouldRecover return true: {context.Type}");
                Recover();
            }
        }

        public bool IsEnough(int amountToSpend) => Current <= amountToSpend;

        public void OnSpend(int amount)
        {
            Current -= amount;
            CurrentChanges?.Invoke(amount);
        }

        public float GetBaseRecovery() => _baseRecoveryAmount;
        public float GetBaseMaximumAmount() => _baseMaximumAmount;

        private void UpdateResources()
        {
            MaximumAmount = _baseMaximumAmount;
            RecoveryAmount = _baseRecoveryAmount;
        }

        private void Recover()
        {
            Current += RecoveryAmount;
            if (Current > MaximumAmount)
                Current = MaximumAmount;
            GD.Print($"Resource: {GetType().Name} recovered: {RecoveryAmount}");
            CurrentChanges?.Invoke(Current);
        }
    }
}

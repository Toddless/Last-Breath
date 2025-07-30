namespace Playground.Components
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public abstract class BaseResource : IResource
    {
        private readonly float _baseRecoveryAmount, _baseMaximumAmount;
        private float _current, _maximum;

        protected BaseResource(float recoveryAmount, float maximumAmount, ResourceType resourceType)
        {
            _baseRecoveryAmount = recoveryAmount;
            _baseMaximumAmount = maximumAmount;
            MaximumAmount = _baseMaximumAmount;
            RecoveryAmount = _baseRecoveryAmount;
            ResourceType = resourceType;

        }

        public event Action<float>? CurrentChanges, MaximumChanges;

        public float RecoveryAmount { get; private set; }

        public float Current
        {
            get => _current;
            set
            {
                if (ObservableProperty.SetProperty(ref _current, value))
                    CurrentChanges?.Invoke(value);
            }
        }
        public float MaximumAmount
        {
            get => _maximum;
            set
            {
                if (ObservableProperty.SetProperty(ref _maximum, value))
                    MaximumChanges?.Invoke(value);
            }
        }

        public ResourceType ResourceType { get; }

        public void OnSpend(int amount)
        {
            // Raise event if not enough?? Return false or some kind of results?
            if (IsEnough(amount))
            {
                Current -= amount;
            }
        }

        public void Recover()
        {
            Current += RecoveryAmount;
            if (Current > MaximumAmount)
                Current = MaximumAmount;
        }

        public virtual void OnParameterChanges(object? sender, ModifiersChangedEventArgs args)
        {
            switch (args.Parameter)
            {
                case Parameter.ResourceMax:
                    MaximumAmount = Calculations.CalculateFloatValue(_baseMaximumAmount, args.Modifiers);
                    break;
                case Parameter.ResourceRecovery:
                    RecoveryAmount = Calculations.CalculateFloatValue(_baseRecoveryAmount, args.Modifiers);
                    break;
                default:
                    break;
            }
        }

        protected virtual void LoadData()
        {

        }

        private bool IsEnough(int amountToSpend) => Current <= amountToSpend;
    }
}

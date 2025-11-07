namespace Battle
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Components;

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
            set => _current = value;
        }
        public float MaximumAmount
        {
            get => _maximum;
            set => _maximum = value;
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

        public virtual void OnParameterChanges(object? sender, IModifiersChangedEventArgs args)
        {

        }
        private bool IsEnough(int amountToSpend) => Current <= amountToSpend;
    }
}

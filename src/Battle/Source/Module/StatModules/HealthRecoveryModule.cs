namespace Battle.Source.Module.StatModules
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Battle.Module;

    internal class HealthRecoveryModule : IParameterModule
    {
        private Func<float> _recovery;
        public Parameter Parameter => Parameter.HealthRecovery;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public HealthRecoveryModule(Func<float> recoveryValue)
        {
            _recovery = recoveryValue;
        }

        public float GetValue() => _recovery.Invoke();
    }
}

namespace Battle.Source.Module.StatModules
{
    using Core.Enums;
    using System;

    internal class HealthModule : Module
    {
        public HealthModule(Func<float> value) : base(value, Parameter.Health)
        {
        }
    }
}

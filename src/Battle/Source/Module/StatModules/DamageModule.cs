namespace Battle.Source.Module.StatModules
{
    using Core.Enums;
    using System;

    public class DamageModule : Module
    {
        public DamageModule(Func<float> value) : base(value, Parameter.Damage)
        {
        }
    }
}

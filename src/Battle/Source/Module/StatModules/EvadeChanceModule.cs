namespace Battle.Source.Module.StatModules
{
    using System;
    using Core.Enums;

    public class EvadeChanceModule : Module
    {
        public EvadeChanceModule(Func<float> value) : base(value, Parameter.Evade)
        {
        }
    }
}

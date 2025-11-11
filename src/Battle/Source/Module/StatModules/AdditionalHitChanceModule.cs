namespace Battle.Source.Module.StatModules
{
    using System;
    using Core.Enums;

    public class AdditionalHitChanceModule : Module
    {
        public AdditionalHitChanceModule(Func<float> value) : base(value, Parameter.AdditionalHitChance)
        {
        }
    }
}

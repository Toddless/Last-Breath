namespace Battle.Source.Module.StatModules
{
    using System;
    using Core.Enums;

    public class CritChanceModule : Module
    {
        public CritChanceModule(Func<float> value) : base(value, Parameter.CriticalChance){}
    }
}

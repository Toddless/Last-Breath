namespace Battle.Source.Module.StatModules
{
    using System;
    using Core.Enums;

    public class CritDamageModule : Module
    {
        public CritDamageModule(Func<float> criticalDamage) : base(criticalDamage, Parameter.CriticalDamage) { }
    }
}

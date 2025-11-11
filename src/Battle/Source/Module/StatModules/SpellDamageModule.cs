namespace Battle.Source.Module.StatModules
{
    using System;
    using Core.Enums;

    internal class SpellDamageModule : Module
    {
        public SpellDamageModule(Func<float> value) : base(value, Parameter.SpellDamage)
        {
        }
    }
}

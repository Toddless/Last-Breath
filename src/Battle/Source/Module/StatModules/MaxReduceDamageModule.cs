namespace Battle.Source.Module.StatModules
{
    using System;
    using Core.Enums;

    public class MaxReduceDamageModule : Module
    {
        public MaxReduceDamageModule(Func<float> value) : base(value, Parameter.MaxReduceDamage)
        {
        }
    }
}

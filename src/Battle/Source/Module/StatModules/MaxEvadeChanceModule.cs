namespace Battle.Source.Module.StatModules
{
    using System;
    using Core.Enums;

    public class MaxEvadeChanceModule(Func<float> value) : Module(value, parameter: Parameter.MaxEvadeChance)
    {
    }
}

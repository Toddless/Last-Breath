namespace Battle.Source.Module.StatModules
{
    using System;
    using Core.Enums;

    public class ArmorModule(Func<float> value) : Module(value, Parameter.Armor);
}

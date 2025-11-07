namespace Battle.Source.Module.StatModules
{
    using System;
    using Core.Enums;

    public class BarrierModule(Func<float> value) : Module(value, parameter: Parameter.Barrier)
    {

    }
}

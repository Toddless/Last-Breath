namespace Battle.Source.Module.StatModules
{
    using System;
    using Core.Enums;

    public class BlockChanceModule(Func<float> value) : Module(value, Parameter.BlockChance);
}

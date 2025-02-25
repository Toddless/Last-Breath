namespace Playground.Components.Interfaces
{
    using System;
    using Playground.Script.Enums;

    public interface ICondition
    {
        Func<bool> CheckCondition { get; set; }
        EffectType EffectNeeded { get; set; }
        float Weight { get; set; }
    }
}
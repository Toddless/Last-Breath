using System;
using Playground.Script.Enums;

namespace Playground.Components.Interfaces
{
    public interface ICondition
    {
        Func<bool> CheckCondition { get; set; }
        EffectType EffectNeeded { get; set; }
        float Weight { get; set; }
    }
}
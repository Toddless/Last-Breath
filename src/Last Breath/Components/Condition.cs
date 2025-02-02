namespace Playground.Components
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    public class Condition : ICondition
    {
        public Func<bool> CheckCondition { get; set; }

        public float Weight { get; set; }

        public EffectType EffectNeeded { get; set; }

        public Condition(Func<bool> condition, float weight, EffectType effect)
        {
            CheckCondition = condition;
            Weight = weight;
            EffectNeeded = effect;
        }
    }
}

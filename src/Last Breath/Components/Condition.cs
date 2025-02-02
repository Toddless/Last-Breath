namespace Playground.Components
{
    using System;
    using Playground.Script.Enums;

    public class Condition
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

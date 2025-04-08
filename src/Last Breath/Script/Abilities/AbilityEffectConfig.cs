namespace Playground.Script.Abilities
{
    using System;
    using System.Collections.Generic;
    using Playground.Script.Abilities.Interfaces;

    public class AbilityEffectConfig
    {
        public IEnumerable<IEffect> SelfTarget { get; set; } = [];
        public IEnumerable<IEffect> TargetEffects { get; set;} = [];
        public IEnumerable<IEffect> MultiEffects { get; set; } = [];
        public Func<ICharacter, IEnumerable<ICharacter>>? MultipleTargetsSelector { get; set; }
    }
}

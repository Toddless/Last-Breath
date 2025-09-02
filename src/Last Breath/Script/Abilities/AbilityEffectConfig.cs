namespace LastBreath.Script.Abilities
{
    using System;
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;

    public class AbilityEffectConfig
    {
        public IEnumerable<IEffect> SelfTarget { get; set; } = [];
        public IEnumerable<IEffect> TargetEffects { get; set; } = [];
        public IEnumerable<IEffect> MultiEffects { get; set; } = [];
        public Func<ICharacter, IEnumerable<ICharacter>>? MultipleTargetsSelector { get; set; }
    }
}

namespace LastBreath.Script.Abilities
{
    using System;
    using System.Collections.Generic;
    using LastBreath.Script;
    using LastBreath.Script.Abilities.Interfaces;

    public class AbilityEffectConfig
    {
        public IEnumerable<IEffect> SelfTarget { get; set; } = [];
        public IEnumerable<IEffect> TargetEffects { get; set; } = [];
        public IEnumerable<IEffect> MultiEffects { get; set; } = [];
        public Func<ICharacter, IEnumerable<ICharacter>>? MultipleTargetsSelector { get; set; }
    }
}

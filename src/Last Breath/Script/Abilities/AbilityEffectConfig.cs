namespace LastBreath.Script.Abilities
{
    using System;
    using System.Collections.Generic;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Entity;

    public class AbilityEffectConfig
    {
        public IEnumerable<IEffect> SelfTarget { get; set; } = [];
        public IEnumerable<IEffect> TargetEffects { get; set; } = [];
        public IEnumerable<IEffect> MultiEffects { get; set; } = [];
        public Func<IEntity, IEnumerable<IEntity>>? MultipleTargetsSelector { get; set; }
    }
}

namespace Battle.Source.Abilities.CriticalCalculation
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Effects;

    /// <summary>
    /// Self-cast ability. Applies N stacks of CritCalculationBuff.
    /// Each stack extends its own duration by 1 turn on a critical hit.
    /// </summary>
    public class CriticalCalculation(AbilityBaseData data) : Ability(data)
    {
        /// <summary>Critical chance one stack of the default buff is worth before effectiveness.</summary>
        private const float BaseCriticalChance = 0.15f;

        public int BuffStacks => (int)this[AbilityParameter.Stacks];
        public int BuffDuration => (int)this[AbilityParameter.Duration];

        /// <summary>How strongly the buff lands — the multiplier its value is read through.</summary>
        public float Effectiveness => this[AbilityParameter.Effectiveness];

        /// <summary>
        /// The buff the ability stacks on cast, built from the given duration, stack cap and
        /// effectiveness. Default is the crit-chance buff; the L3 "replace" upgrade swaps it for an
        /// additional-attack-chance buff. ExecuteInternal applies <see cref="BuffStacks"/> stacks of
        /// whatever this returns.
        ///
        /// Effectiveness is passed in rather than applied afterwards because each factory knows its own
        /// value and nothing outside knows which of them is seated: a swap that ignored the multiplier
        /// would silently switch the ability's effectiveness augments off along with the buff.
        /// </summary>
        public Func<int, int, float, IEffect> PrimaryBuffFactory { get; set; } =
            (duration, maxStacks, effectiveness) =>
                new CritCalculationBuff(duration, maxStacks, value: BaseCriticalChance * effectiveness);

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Stacks, 3);
            parameters.RegisterDefault(AbilityParameter.Duration, 1);
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new CriticalCalculation(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            var context = new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId };
            int stacks = BuffStacks;
            await PrimaryBuffFactory(BuffDuration, stacks, Effectiveness).ApplyStacks(context, stacks);
        }
    }
}

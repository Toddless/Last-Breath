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
        public int BuffStacks => (int)this[Parameters.Stacks];
        public int BuffDuration => (int)this[Parameters.Duration];

        /// <summary>
        /// The buff the ability stacks on cast, built from the given duration. Default is the crit-chance
        /// buff; the L3 "replace" upgrade swaps it for an additional-attack-chance buff.
        /// ExecuteInternal applies <see cref="BuffStacks"/> stacks of whatever this returns.
        /// </summary>
        public Func<int, int, IEffect> PrimaryBuffFactory { get; set; } =
            (duration, maxStacks) => new CritCalculationBuff(duration, maxStacks, value: 0.15f);

        public static class Parameters
        {
            public const string Stacks = nameof(Stacks);
            public const string Duration = nameof(Duration);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(Parameters.Stacks, 3);
            parameters.RegisterDefault(Parameters.Duration, 1);
        }

        public override IAbility Copy() => CopyUpgradesTo(new CriticalCalculation(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            var context = new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId };
            int stacks = BuffStacks;
            await PrimaryBuffFactory(BuffDuration, stacks).ApplyStacks(context, stacks);
        }
    }
}

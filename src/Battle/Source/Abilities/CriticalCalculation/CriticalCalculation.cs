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

        /// <summary>
        /// The buff the ability stacks on cast, built from the given duration and stack cap. Default is
        /// the crit-chance buff; the L3 "replace" upgrade swaps it for an additional-attack-chance buff.
        /// ExecuteInternal applies <see cref="BuffStacks"/> stacks of whatever this returns.
        ///
        /// A factory hands over the figure it was WRITTEN with and nothing else: how strongly that
        /// figure lands is the cast's business, and it reaches the buff through the context it is
        /// applied with. A factory that multiplied by hand would multiply twice, and a swapped-in one
        /// that forgot to would switch the ability's effectiveness off along with the buff.
        /// </summary>
        public Func<int, int, IEffect> PrimaryBuffFactory { get; set; } =
            (duration, maxStacks) => new CritCalculationBuff(duration, maxStacks, value: BaseCriticalChance);

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
            EffectApplyingContext context = Laying(owner);
            int stacks = BuffStacks;
            await PrimaryBuffFactory(BuffDuration, stacks).ApplyStacks(context, stacks);
        }
    }
}

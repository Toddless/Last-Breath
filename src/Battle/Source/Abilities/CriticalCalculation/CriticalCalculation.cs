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
    /// Each stack extends its own duration on a critical hit by the turns the record names.
    /// </summary>
    public class CriticalCalculation(AbilityBaseData data) : Ability(data)
    {
        public int BuffStacks => (int)this[AbilityParameter.Stacks];
        public int BuffDuration => (int)this[AbilityParameter.Duration];

        /// <summary>Critical chance one stack of the buff is worth before effectiveness.</summary>
        public float BuffCriticalChance => this[Parameters.CriticalChance];

        /// <summary>Turns one critical hit of the bearer adds to the buff this cast laid.</summary>
        public int AdditionalDuration => (int)this[Parameters.AdditionalDurationAmount];

        public static class Parameters
        {
            public const string CriticalChance = nameof(CriticalChance);
            public const string AdditionalDurationAmount = nameof(AdditionalDurationAmount);
        }

        /// <summary>
        /// The buff the ability stacks on cast, built from the given duration and stack cap. Default is
        /// the crit-chance buff, and nothing in the shipped catalog assigns another — the seam is what a
        /// replacing record would reach for, and every cast lays the default until one exists.
        ///
        /// A factory hands over the figures it was WRITTEN with and nothing else: how strongly they land
        /// is the cast's business, and it reaches the buff through the context it is applied with. A
        /// factory that multiplied by hand would multiply twice, and a swapped-in one that forgot to
        /// would switch the ability's effectiveness off along with the buff.
        /// </summary>
        public Func<int, int, IEffect> PrimaryBuffFactory
        {
            get => field ??= LayCritCalculationBuff;
            set => field = value;
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Stacks, 3);
            parameters.RegisterDefault(AbilityParameter.Duration, 1);
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            // The crit chance the buff is worth is the record's to name; the base is the neutral share
            // of nothing, so an ability built without the key grants no chance instead of a made-up one.
            parameters.RegisterDefault(Parameters.CriticalChance, 0f);
            parameters.RegisterDefault(Parameters.AdditionalDurationAmount, 1);
        }

        public override IAbility Copy() => CopyUpgradesTo(new CriticalCalculation(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            EffectApplyingContext context = Laying(owner);
            int stacks = BuffStacks;
            await PrimaryBuffFactory(BuffDuration, stacks).ApplyStacks(context, stacks);
        }

        /// <summary>The crit-chance buff as this cast's own numbers describe it: the chance one stack is
        /// written for and the turns one critical hit gives it back.</summary>
        private IEffect LayCritCalculationBuff(int duration, int maxStacks) =>
            new CritCalculationBuff(duration, maxStacks, BuffCriticalChance)
            {
                TurnsPerCritical = AdditionalDuration
            };
    }
}

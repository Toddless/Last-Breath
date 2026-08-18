namespace Battle.Source.Abilities.PoisonCoating
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Effects;

    /// <summary>
    /// Self-cast buff. For <see cref="Duration"/> turns, each of the caster's attacks
    /// applies a poison stack to the target.
    /// </summary>
    public class PoisonCoating(AbilityBaseData data) : Ability(data)
    {
        /// <summary>How long the coating buff itself holds on the caster — the common buff duration.</summary>
        public int Duration => (int)this[AbilityParameter.Duration];

        public int PoisonDuration => (int)this[AbilityParameter.PoisonDuration];
        public float PoisonDamagePercent => this[Parameters.PoisonMultiplier];

        public static class Parameters
        {
            public const string PoisonMultiplier = nameof(PoisonMultiplier);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            parameters.RegisterDefault(AbilityParameter.Duration, 3);
            parameters.RegisterAppliedDuration(AbilityParameter.PoisonDuration, 5);
            parameters.RegisterDefault(Parameters.PoisonMultiplier, 0.45f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new PoisonCoating(Data));

        protected override Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            // Apply the coating buff to the caster; PoisonCoatingEffect handles attack interception
            var coatingBuff = new PoisonCoatingEffect(
                duration: Duration,
                maxStacks: 1,
                poisonDuration: PoisonDuration,
                poisonDamagePercent: PoisonDamagePercent);

            coatingBuff.Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });

            return Task.CompletedTask;
        }
    }
}

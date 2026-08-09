namespace Battle.Source.Abilities.AresBlessing
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Effects;

    /// <summary>
    /// Self-buff: raises max health and health recovery for a few turns (one composite effect).
    /// L3 upgrades add extra cast effects (incoming damage reduction / turn-end heal / damage buff)
    /// through activation riders with deferred factories, so they follow the current duration.
    /// </summary>
    public class AresBlessing(AbilityBaseData data) : Ability(data)
    {
        public int Duration => (int)this[AbilityParameter.Duration];
        public float HealthBonus => this[Parameters.HealthBonus];
        public float RecoveryBonus => this[Parameters.RecoveryBonus];

        /// <summary>
        /// The blessing's two bonuses. They are VALUES an effectiveness would multiply and not the
        /// multiplier, so the ability could declare <see cref="AbilityParameter.Effectiveness"/> and
        /// read it through them — it deliberately does not yet, and the reason is one record:
        /// <c>Augment_Add_Effectiveness_Reduce_Stacks</c> pays for its raise in stacks and the blessing
        /// has none, so the bill would land nowhere and the raise would arrive free. The other records
        /// of the family charge nothing and would be welcome; declaring the concept here waits on that
        /// one having a bill this ability can carry.
        /// </summary>
        public static class Parameters
        {
            public const string HealthBonus = nameof(HealthBonus);
            public const string RecoveryBonus = nameof(RecoveryBonus);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Duration, 3);
            parameters.RegisterDefault(Parameters.HealthBonus, 0.3f);
            parameters.RegisterDefault(Parameters.RecoveryBonus, 0.3f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new AresBlessing(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) =>
            await new AresBlessingEffect(Duration, HealthBonus, RecoveryBonus)
                .Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });
    }
}

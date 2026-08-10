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

        /// <summary>The blessing's two bonuses — shares gained, scaled by the cast's effectiveness.</summary>
        public static class Parameters
        {
            public const string HealthBonus = nameof(HealthBonus);
            public const string RecoveryBonus = nameof(RecoveryBonus);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            parameters.RegisterDefault(AbilityParameter.Duration, 3);
            parameters.RegisterDefault(Parameters.HealthBonus, 0.3f);
            parameters.RegisterDefault(Parameters.RecoveryBonus, 0.3f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new AresBlessing(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) =>
            await new AresBlessingEffect(Duration, HealthBonus, RecoveryBonus).Apply(Laying(owner));
    }
}

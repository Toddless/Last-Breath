namespace Battle.Source.Abilities.TwinAssist
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Effects;

    /// <summary>
    /// Hidden twin-boss reaction: the brother wards the owner with a shield (the separate defense
    /// layer, not the barrier) that also regenerates a share of max health each turn while it
    /// holds. Never learnable: only the reactions driver casts it.
    /// </summary>
    public class TwinAssistShield(AbilityBaseData data) : Ability(data)
    {
        public float ShieldStrength => this[AbilityParameter.ShieldStrength];
        public float HealthRegenPercent => this[Parameters.HealthRegenPercent];

        public static class Parameters
        {
            public const string HealthRegenPercent = nameof(HealthRegenPercent);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.ShieldStrength, 500f);
            parameters.RegisterDefault(Parameters.HealthRegenPercent, 0.05f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new TwinAssistShield(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            var context = new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId };
            await new ShieldEffect(ShieldStrength, HealthRegenPercent).Apply(context);
        }
    }
}

namespace Battle.Source.Abilities.Sacrifice
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Effects;

    /// <summary>
    /// Sacrifices a share of CURRENT health; the next activated abilities (charges) deal extra PURE
    /// damage — every 100 health lost gives <c>RatePerHundred</c> of the cast's damage as the bonus.
    /// Cross-stance by design: the charge is an entity effect.
    /// </summary>
    public class Sacrifice(AbilityBaseData data) : Ability(data)
    {
        public float SacrificePercent => this[Parameters.SacrificePercent];
        public float RatePerHundred => this[Parameters.RatePerHundred];
        public int Charges => (int)this[AbilityParameter.Charges];
        public float HealFromEmpoweredDamage => this[AbilityParameter.HealFromEmpoweredDamage];

        public static class Parameters
        {
            public const string SacrificePercent = nameof(SacrificePercent);
            public const string RatePerHundred = nameof(RatePerHundred);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(Parameters.SacrificePercent, 0.15f);
            parameters.RegisterDefault(Parameters.RatePerHundred, 0.01f);
            parameters.RegisterDefault(AbilityParameter.Charges, 1);
            parameters.RegisterDefault(AbilityParameter.HealFromEmpoweredDamage, 0f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new Sacrifice(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            float sacrificed = owner.CurrentHealth * SacrificePercent;
            owner.ConsumeResource(Costs.Health, sacrificed);

            float bonus = sacrificed / 100f * RatePerHundred;
            await new SacrificeChargeEffect(Id, Charges, bonus, HealFromEmpoweredDamage)
                .Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });
        }
    }
}

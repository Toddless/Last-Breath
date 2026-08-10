namespace Battle.Source.Abilities.DarkShroud
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Effects;
    using Godot;

    /// <summary>
    /// Self-cast defensive ability. Applies LightStep evasion stacks and percentage health regeneration.
    /// </summary>
    public class DarkShroud(AbilityBaseData data) : Ability(data)
    {
        public float Duration => this[AbilityParameter.Duration];
        public float Stacks => this[AbilityParameter.Stacks];
        public float LightStepValue => this[Parameters.LightStepValue];
        public float HealthRegen => this[Parameters.HealthRegen];

        public static class Parameters
        {
            public const string HealthRegen = nameof(HealthRegen);
            public const string LightStepValue = nameof(LightStepValue);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Duration, 3);
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            parameters.RegisterDefault(AbilityParameter.Stacks, 3);
            parameters.RegisterDefault(Parameters.HealthRegen, 0.05f);
            parameters.RegisterDefault(Parameters.LightStepValue, 0.15f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new DarkShroud(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            EffectApplyingContext context = Laying(owner);
            await new LightStep(Mathf.RoundToInt(Duration), Mathf.RoundToInt(Stacks), LightStepValue)
                .ApplyStacks(context, Mathf.RoundToInt(Stacks));
            await new HealthRegenerationEffect(HealthRegen, Mathf.RoundToInt(Duration), 1).Apply(context);
        }
    }
}

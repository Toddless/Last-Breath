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
        public float Duration => this[Parameters.Duration];
        public float Effectiveness => this[Parameters.Effectiveness];
        public float Stacks => this[Parameters.Stacks];
        public float LightStepValue => this[Parameters.LightStepValue];
        public float HealthRegen => this[Parameters.HealthRegen];

        public static class Parameters
        {
            public const string Effectiveness = nameof(Effectiveness);
            public const string Duration = nameof(Duration);
            public const string Stacks = nameof(Stacks);
            public const string HealthRegen = nameof(HealthRegen);
            public const string LightStepValue = nameof(LightStepValue);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(Parameters.Duration, 3);
            parameters.RegisterDefault(Parameters.Effectiveness, 1f);
            parameters.RegisterDefault(Parameters.Stacks, 3);
            parameters.RegisterDefault(Parameters.HealthRegen, 0.05f);
            parameters.RegisterDefault(Parameters.LightStepValue, 0.15f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new DarkShroud(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            var context = new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId };
            await new LightStep(Mathf.RoundToInt(Duration), Mathf.RoundToInt(Stacks), LightStepValue * Effectiveness)
                .ApplyStacks(context, Mathf.RoundToInt(Stacks));
            await new HealthRegenerationEffect(HealthRegen * Effectiveness, Mathf.RoundToInt(Duration), 1).Apply(context);
        }
    }
}

namespace Battle.Source.Abilities.Modifiers
{
    using System;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;

    public class AbilityBuffPostActivationModifier(IEffect buff) : IActivationRider
    {
        public string Id => $"Ability_Apply_{buff.Id}_Post_Activation_Modifier";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(IAbilityActivationContext context) =>
            await buff
                .Copy()
                .Apply(new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = context.Caster });
    }
}

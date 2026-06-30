namespace Battle.Source.Abilities
{
    using System;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;

    public class AbilityBuffPostActivationModifier(IEffect buff) : IAbilityPostActivationModifier
    {
        public string Id => $"Ability_Apply_{buff.Id}_Post_Activation_Modifier";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(AbilityActivationContext context) =>
            await buff
                .Copy()
                .Apply(new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = context.Caster });
    }
}

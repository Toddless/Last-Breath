namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;

    public class AbilityBuffActivationRider(IEffect buff) : IActivationRider
    {
        public string Id => $"Ability_Apply_{buff.Id}_Activation_Rider";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(IAbilityActivationContext context) =>
            await buff
                .Copy()
                .Apply(new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = context.Caster });
    }
}

namespace Battle.Source.Abilities.Modifiers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;

    public class AbilityDebuffPostActivationModifier(IEffect debuff, bool applyOnSelf = false) : IActivationRider
    {
        public string Id => $"Ability_Apply_{debuff.Id}_Post_Activation_Modifier";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(IAbilityActivationContext context)
        {
            var copy = debuff.Copy();
            if (applyOnSelf)
            {
                await copy.Apply(new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = context.Caster });
                return;
            }

            foreach (var applyingContext in context.Targets
                         .Select(contextTarget =>
                             new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = contextTarget }))
            {
                var clone = debuff.Copy();
                await clone.Apply(applyingContext);
            }
        }
    }
}

namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;

    public class AbilityDebuffActivationRider(IEffect debuff, bool applyOnSelf = false) : IActivationRider
    {
        public string Id => $"Ability_Apply_{debuff.Id}_Activation_Rider";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(IAbilityActivationContext context)
        {
            var copy = debuff.Copy();
            if (applyOnSelf)
            {
                await copy.Apply(new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = context.Caster, Effectiveness = context.Ability.Effectiveness, Trace = context.Ability.Trace });
                return;
            }

            foreach (var applyingContext in context.Targets
                         .Select(contextTarget =>
                             new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = contextTarget, Effectiveness = context.Ability.Effectiveness, Trace = context.Ability.Trace }))
            {
                var clone = debuff.Copy();
                await clone.Apply(applyingContext);
            }
        }
    }
}

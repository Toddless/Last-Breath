namespace Battle.Source.Abilities
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;

    public class ApplyDebuffPostActivationModifier(IEffect debuff, bool applyOnSelf = false) : IAbilityPostActivationModifier
    {
        public string Id => $"Ability_Apply_{debuff.Id}_Post_Activation_Modifier";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public Task Apply(AbilityActivationContext context)
        {
            var copy = debuff.Copy();
            if (applyOnSelf)
            {
                copy.Apply(new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = context.Caster });
                return Task.CompletedTask;
            }

            foreach (var applyingContext in context.Targets.Select(contextTarget =>
                         new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = contextTarget }))
                copy.Apply(applyingContext);

            return Task.CompletedTask;
        }
    }
}

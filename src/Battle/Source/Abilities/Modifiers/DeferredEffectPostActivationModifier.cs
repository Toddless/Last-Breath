namespace Battle.Source.Abilities.Modifiers
{
    using System;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;

    /// <summary>
    /// Post-activation modifier that builds its effect fresh on each cast via <paramref name="effectFactory"/>,
    /// so the effect reflects the ability's current (post-upgrade) parameters instead of a value
    /// snapshot at apply time. Can buff the caster, debuff the targets, or both.
    /// A new effect instance is produced per application, so every target gets its own stack lifetime.
    /// </summary>
    public class DeferredEffectPostActivationModifier(
        string id,
        Func<IEffect> effectFactory,
        bool applyOnCaster = true,
        bool applyOnTargets = false) : IActivationRider
    {
        public string Id => id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(IAbilityActivationContext context)
        {
            if (applyOnCaster)
                await effectFactory().Apply(new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = context.Caster });

            if (!applyOnTargets) return;

            foreach (var target in context.Targets)
                await effectFactory().Apply(new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = target });
        }
    }
}

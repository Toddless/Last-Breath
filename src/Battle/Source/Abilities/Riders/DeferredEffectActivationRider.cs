namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;

    /// <summary>
    /// Post-activation modifier that builds its effect fresh on each cast via <paramref name="effectFactory"/>,
    /// so the effect reflects the ability's current (post-upgrade) parameters instead of a value
    /// snapshot at apply time. Can buff the caster, debuff the targets, or both.
    /// A new effect instance is produced per application, so every target gets its own stack lifetime.
    /// </summary>
    public class DeferredEffectActivationRider(
        string id,
        Func<IEffect?> effectFactory,
        bool applyOnCaster = true,
        bool applyOnTargets = false) : IActivationRider
    {
        public string Id => id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(IAbilityActivationContext context)
        {
            // Every instance built is an instance used: a factory may roll, and one built only to be
            // thrown away would burn the roll. A factory that came back with nothing has already
            // reported why, so the rider is not the place to discover it.
            if (applyOnCaster)
            {
                IEffect? onCaster = effectFactory();
                if (onCaster == null) return;

                await onCaster.Apply(new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = context.Caster, Effectiveness = context.Ability.Effectiveness, Trace = context.Ability.Trace });
            }

            if (!applyOnTargets) return;

            foreach (var target in context.Targets)
            {
                IEffect? onTarget = effectFactory();
                if (onTarget == null) return;

                await onTarget.Apply(new EffectApplyingContext { Caster = context.Caster, Source = InstanceId, Target = target, Effectiveness = context.Ability.Effectiveness, Trace = context.Ability.Trace });
            }
        }
    }
}

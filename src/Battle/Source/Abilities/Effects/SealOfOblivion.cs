namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class SealOfOblivion(
        string id,
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.Cursed)
        : Effect(id, duration, maxStacks, statusEffect)
    {
        public override void Apply(EffectApplyingContext context)
        {
            base.Apply(context);
            var target = context.Target;
        }
    }
}

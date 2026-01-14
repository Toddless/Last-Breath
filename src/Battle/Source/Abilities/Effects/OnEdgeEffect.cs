namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class OnEdgeEffect(
        int duration,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_On_Edge", duration, maxStacks: 1, statusEffect)
    {
        public override void Apply(EffectApplyingContext context)
        {
            base.Apply(context);
        }

        public override IEffect Clone() => new OnEdgeEffect(Duration, Status);
    }
}

namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class OnEdgeEffect(
        string id,
        int duration,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, maxStacks: 1, statusEffect)
    {

        public override void Apply(EffectApplyingContext context)
        {

            base.Apply(context);
        }

        public override IEffect Clone() => new OnEdgeEffect(Id, Duration, Status);
    }
}

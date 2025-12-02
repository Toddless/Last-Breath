namespace Battle.TestData.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class OnEdgeEffect(
        string id,
        int duration,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, stacks: 1, statusEffect)
    {

        public override void OnApply(EffectApplyingContext context)
        {

            base.OnApply(context);
        }

        public override IEffect Clone() => new OnEdgeEffect(Id, Duration, Status);
    }
}

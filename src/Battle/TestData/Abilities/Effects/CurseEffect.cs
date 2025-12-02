namespace Battle.TestData.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class CurseEffect(int duration, int stacks, StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Curse_Effect", duration, stacks, statusEffect)
    {
        public override void OnApply(EffectApplyingContext context)
        {
        }

        public override IEffect Clone() => new CurseEffect
        (
            Duration, MaxStacks, Status
        );
    }
}

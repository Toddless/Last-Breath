namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class CurseEffect(int duration, int maxStacks, StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Curse", duration, maxStacks, statusEffect)
    {
        public override void Apply(EffectApplyingContext context)
        {
        }

        public override IEffect Clone() => new CurseEffect
        (
            Duration, MaxMaxStacks, Status
        );
    }
}

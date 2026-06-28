namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class CurseEffect(
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Curse", duration, maxStacks, statusEffect)
    {
        public override Task Apply(EffectApplyingContext context)
        {
            return Task.CompletedTask;
        }

        public override IEffect Copy() => new CurseEffect
        (
            Duration, MaxStacks, Status
        );
    }
}

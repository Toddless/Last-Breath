namespace Battle.Source.Abilities.Effects
{
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class OnEdgeEffect(
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_On_Edge", duration, maxStacks, statusEffect)
    {
        public override async Task Apply(EffectApplyingContext context)
        {
           await  base.Apply(context);
        }

        public override IEffect Clone() => new OnEdgeEffect(Duration, MaxStacks, Status);
    }
}

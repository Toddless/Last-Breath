namespace Battle.Source.Abilities.Effects
{
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class SealOfOblivion(
        string id,
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.Cursed)
        : Effect(id, duration, maxStacks, statusEffect)
    {
        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            var target = context.Target;
        }

        public override IEffect Clone() => new SealOfOblivion(Id, Duration, MaxStacks, Status);
    }
}

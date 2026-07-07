namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Enums;

    public class DarkShroudEffect(
        string id,
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.Regeneration)
        : Effect(id, duration, maxStacks, statusEffect)

    {

        public override Task Apply(EffectApplyingContext context)
        {

            return base.Apply(context);
        }


        public override void Remove()
        {
            base.Remove();
        }



        public override IEffect Copy() => new DarkShroudEffect(Id, Duration, MaxStacks, Status);
    }
}

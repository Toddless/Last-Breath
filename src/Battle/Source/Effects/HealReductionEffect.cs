namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Modifiers.Context;

    /// <summary>Debuff: reduces all incoming healing on the target by <c>reduceBy</c> (0..1).</summary>
    public class HealReductionEffect(int duration, int maxStacks, float reduceBy)
        : Effect(id: "Effect_Heal_Reduction", duration, maxStacks)
    {
        public override bool IsHarmful => true;

        private IHealModifier? _modifier;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            _modifier = new HealReductionContextModifier(reduceBy);
            Target?.ModifierHandler.Add(_modifier);
        }

        public override void Remove()
        {
            if (_modifier != null) Target?.ModifierHandler.Remove(_modifier);
            base.Remove();
        }

        public override IEffect Copy() => new HealReductionEffect(Duration, MaxStacks, reduceBy);
    }
}

namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class ConfusionEffect(int duration)
        : Effect(id: "Effect", duration, maxStacks: 1, statusEffect: StatusEffects.Confused)
    {
        public override void Apply(EffectApplyingContext context)
        {
            base.Apply(context);
            var target = context.Target;
            var chooser = new ChooseRandomTarget();
            target.TargetChooser = chooser;
        }

        public override void Remove()
        {
            base.Remove();
            Owner?.TargetChooser = null;
        }

        public override IEffect Clone() => new ConfusionEffect(Duration);
    }
}

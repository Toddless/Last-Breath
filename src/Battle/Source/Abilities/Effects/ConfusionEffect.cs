namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;

    public class ConfusionEffect(string id, int duration)
        : Effect(id, duration, stacks: 1, statusEffect: StatusEffects.Confused)
    {
        public override void Apply(EffectApplyingContext context)
        {
            base.Apply(context);
            var target = context.Target;
            var chooser = new ChooseRandomTarget();
            target.TargetChooser = chooser;
        }

        public override void Remove(IEntity source)
        {
            base.Remove(source);
            source.TargetChooser = null;
        }

        public override IEffect Clone() => new ConfusionEffect(Id, Duration);
    }
}

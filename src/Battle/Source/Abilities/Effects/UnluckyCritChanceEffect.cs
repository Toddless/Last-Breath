namespace Battle.Source.Abilities.Effects
{
    using Battle.Source.Decorators;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Entity;

    public class UnluckyCritChanceEffect(
        string id,
        int duration,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, stacks: 1, statusEffect)
    {
        private readonly EntityParameterModuleDecorator _unluckyCritChanceDecorator = new UnluckyChanceDecorator(DecoratorPriority.Strong, EntityParameter.CriticalChance);

        public override void Apply(EffectApplyingContext context)
        {
            base.Apply(context);
            context.Target.Parameters.AddModuleDecorator(_unluckyCritChanceDecorator);
        }

        public override void Remove(IEntity source)
        {
            base.Remove(source);
            source.Parameters.RemoveModuleDecorator(_unluckyCritChanceDecorator.Id, _unluckyCritChanceDecorator.Parameter);
        }

        public override IEffect Clone() => new UnluckyCritChanceEffect(Id, Duration, Status);
    }
}

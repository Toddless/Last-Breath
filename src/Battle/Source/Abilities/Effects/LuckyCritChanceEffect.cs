namespace Battle.Source.Abilities.Effects
{
    using Battle.Source.Decorators;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Entity;

    public class LuckyCritChanceEffect(
        string id,
        int duration,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, stacks: 1, statusEffect)
    {
        private readonly EntityParameterModuleDecorator _luckyCritChanceDecorator = new LuckyChanceDecorator(DecoratorPriority.Strong, EntityParameter.CriticalChance);

        public override void Apply(EffectApplyingContext context)
        {
            context.Target.Parameters.AddModuleDecorator(_luckyCritChanceDecorator);
            base.Apply(context);
        }

        public override void Remove(IEntity source)
        {
            source.Parameters.RemoveModuleDecorator(_luckyCritChanceDecorator.Id, _luckyCritChanceDecorator.Parameter);
            base.Remove(source);
        }

        public override IEffect Clone() => new LuckyCritChanceEffect(Id, Duration, Status);
    }
}

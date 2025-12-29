namespace Battle.Source.Abilities.Effects
{
    using Battle.Source.Decorators;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components.Decorator;

    public class LuckyCritChanceEffect(
        int duration,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Lucky_Crit_Chance", duration, maxStacks: 1, statusEffect)
    {
        private readonly EntityParameterModuleDecorator _luckyCritChanceDecorator = new LuckyChanceDecorator(DecoratorPriority.Strong, EntityParameter.CriticalChance);

        public override void Apply(EffectApplyingContext context)
        {
            context.Target.Parameters.AddModuleDecorator(_luckyCritChanceDecorator);
            base.Apply(context);
        }

        public override void Remove()
        {
            Owner?.Parameters.RemoveModuleDecorator(_luckyCritChanceDecorator.Id, _luckyCritChanceDecorator.Parameter);
            base.Remove();
        }

        public override IEffect Clone() => new LuckyCritChanceEffect(Duration, Status);
    }
}

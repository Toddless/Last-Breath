namespace Battle.Source.Abilities.Effects
{
    using Battle.Source.Decorators;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components.Decorator;

    public class UnluckyCritChanceEffect(
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Unlucky_Critical_Chance", duration, maxStacks, statusEffect)
    {
        private readonly EntityParameterModuleDecorator _unluckyCritChanceDecorator = new UnluckyChanceDecorator(DecoratorPriority.Strong, EntityParameter.CriticalChance);

        public override void Apply(EffectApplyingContext context)
        {
            base.Apply(context);
            context.Target.Parameters.AddModuleDecorator(_unluckyCritChanceDecorator);
        }

        public override void Remove()
        {
            Owner?.Parameters.RemoveModuleDecorator(_unluckyCritChanceDecorator.Id, _unluckyCritChanceDecorator.Parameter);
            base.Remove();
        }

        public override IEffect Clone() => new UnluckyCritChanceEffect(Duration, MaxMaxStacks, Status);
    }
}

namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Battle.Source.Decorators;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components.Decorator;

    public class LuckyCritChanceEffect(
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Lucky_Crit_Chance", duration, maxStacks, statusEffect)
    {
        private readonly EntityParameterModuleDecorator _luckyCritChanceDecorator = new LuckyChanceDecorator(Priority.Strong, EntityParameter.CriticalChance);

        public override async Task Apply(EffectApplyingContext context)
        {
            context.Target.Parameters.AddModuleDecorator(_luckyCritChanceDecorator);
            await base.Apply(context);
        }

        public override void Remove()
        {
            Target?.Parameters.RemoveModuleDecorator(_luckyCritChanceDecorator.Id, _luckyCritChanceDecorator.Parameter);
            base.Remove();
        }

        public override IEffect Copy() => new LuckyCritChanceEffect(Duration, MaxStacks, Status);
    }
}

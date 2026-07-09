namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Components.Decorator;
    using Core.Enums;

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

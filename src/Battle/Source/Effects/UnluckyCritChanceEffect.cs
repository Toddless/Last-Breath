namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Components.Decorator;
    using Core.Enums;
    using Decorators;

    public class UnluckyCritChanceEffect(
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Unlucky_Critical_Chance", duration, maxStacks, statusEffect)
    {
        private readonly EntityParameterModuleDecorator _unluckyCritChanceDecorator = new UnluckyChanceDecorator(Priority.Strong, EntityParameter.CriticalChance);

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            context.Target.Parameters.AddModuleDecorator(_unluckyCritChanceDecorator);
        }

        public override void Remove()
        {
            Target?.Parameters.RemoveModuleDecorator(_unluckyCritChanceDecorator.Id, _unluckyCritChanceDecorator.Parameter);
            base.Remove();
        }

        public override IEffect Copy() => new UnluckyCritChanceEffect(Duration, MaxStacks, Status);
    }
}

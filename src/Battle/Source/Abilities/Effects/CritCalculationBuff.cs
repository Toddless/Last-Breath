namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Battle.Source.Decorators;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components.Decorator;

    /// <summary>
    /// Buff that raises critical chance (lucky roll mechanic) and extends its own duration
    /// by 1 turn each time the bearer scores a critical hit.
    /// </summary>
    public class CritCalculationBuff(
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Crit_Calculation_Buff", duration, maxStacks, statusEffect)
    {
        private readonly EntityParameterModuleDecorator _decorator =
            new LuckyChanceDecorator(DecoratorPriority.Strong, EntityParameter.CriticalChance);

        public override async Task Apply(EffectApplyingContext context)
        {
            context.Target.Parameters.AddModuleDecorator(_decorator);
            await base.Apply(context);
        }

        public override void AfterAttack(IAttackContext context)
        {
            // Extend this stack's duration by 1 when a critical hit is scored
            if (context.IsCritical) Duration++;
        }

        public override void Remove()
        {
            AppliedTo?.Parameters.RemoveModuleDecorator(_decorator.Id, _decorator.Parameter);
            base.Remove();
        }

        public override IEffect Clone() => new CritCalculationBuff(Duration, MaxStacks, Status);
    }
}

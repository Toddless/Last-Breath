namespace Core.Interfaces.Abilities
{
    using Enums;
    using Battle;
    using Entity;

    public interface IEffect : IIdentifiable, IDisplayable
    {
        StatusEffects Status { get; set; }
        int Duration { get; set; }
        int MaxStacks { get; set; }
        object? Source { get; }

        void Apply(EffectApplyingContext context);
        void Remove(IEntity source);
        void TurnStart(IEntity source);
        void TurnEnd(IEntity source);
        void BeforeAttack(IEntity source, IAttackContext context);
        void AfterAttack(IEntity source, IAttackContext context);
        bool IsStronger(IEffect otherEffect);
        IEffect Clone();
    }
}

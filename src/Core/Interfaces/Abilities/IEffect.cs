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

        void OnApply(EffectApplyingContext context);
        void OnTurnEnd(IEntity target);
        void OnTurnStart(IEntity target);
        void OnBeforeAttack(IEntity target, IAttackContext context);
        void OnAfterAttack(IEntity target, IAttackContext context);
        bool IsStronger(IEffect otherEffect);
        void Remove(IEntity target);
        IEffect Clone();
    }
}

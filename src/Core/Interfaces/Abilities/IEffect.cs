namespace Core.Interfaces.Abilities
{
    using Enums;
    using Battle;
    using System;
    using Entity;

    public interface IEffect : IIdentifiable, IDisplayable
    {
        IEntity? Owner { get; }
        StatusEffects Status { get; set; }
        int Duration { get; set; }
        int MaxMaxStacks { get; set; }
        string Source { get; }

        event Action<int>? DurationChanged;

        void Apply(EffectApplyingContext context);
        void Remove();
        void TurnStart();
        void TurnEnd();
        void BeforeAttack(IAttackContext context);
        void AfterAttack(IAttackContext context);
        bool IsStronger(IEffect otherEffect);
        IEffect Clone();
    }
}

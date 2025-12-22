namespace Core.Interfaces.Abilities
{
    using Enums;
    using Battle;
    using System;

    public interface IEffect : IIdentifiable, IDisplayable
    {
        StatusEffects Status { get; set; }
        int Duration { get; set; }
        int MaxStacks { get; set; }
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

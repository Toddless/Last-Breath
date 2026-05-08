namespace Core.Interfaces.Abilities
{
    using Enums;
    using Battle;
    using System;
    using Entity;
    using System.Threading.Tasks;

    public interface IEffect : IIdentifiable, IDisplayable
    {
        IEntity? Target { get; }
        StatusEffects Status { get; set; }
        int Duration { get; set; }
        int MaxStacks { get; set; }
        string Source { get; }

        event Action<int>? DurationChanged;

        Task Apply(EffectApplyingContext context);
        void OnStackChanged(int currentStack);
        void Remove();
        void TurnStart();
        void TurnEnd();
        void BeforeAttack(IAttackContext context);
        void AfterAttack(IAttackContext context);
        bool IsStronger(IEffect otherEffect);
        IEffect Copy();
    }
}

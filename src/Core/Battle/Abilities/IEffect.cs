namespace Core.Battle.Abilities
{
    using System;
    using System.Threading.Tasks;
    using Enums;
    using Entity;
    using Interfaces;

    public interface IEffect : IIdentifiable, IDisplayable
    {
        IFightable? Target { get; }
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
        bool IsStronger(IEffect otherEffect);
        IEffect Copy();
    }
}

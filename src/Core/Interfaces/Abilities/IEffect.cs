namespace Core.Interfaces.Abilities
{
    using System;
    using System.Threading.Tasks;
    using Entity;
    using Enums;

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
        bool IsStronger(IEffect otherEffect);
        IEffect Copy();
    }
}

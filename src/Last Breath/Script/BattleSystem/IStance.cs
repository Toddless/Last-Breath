namespace Playground.Script.BattleSystem
{
    using Playground.Components.Interfaces;
    using System;
    using Playground.Script;

    public interface IStance
    {
        IResource Resource { get; }

        event Action<float>? CurrentResourceChanges, MaximumResourceChanges;
        void OnAttack(ICharacter target);
        void OnReceiveAttack(AttackContext context);
        void OnActivate();
        void OnDeactivate();
    }
}

namespace Playground.Script.BattleSystem
{
    using Playground.Components.Interfaces;
    using System;
    using Playground.Script;
    using Playground.Components;

    public interface IStance
    {
        IResource Resource { get; }
        FloatModuleDecoratorManager FloatDecoratorManager { get; }
        ActionModuleDecoratorManager ActionDecoratorManager { get; }

        event Action<float>? CurrentResourceChanges, MaximumResourceChanges;
        void OnAttack(ICharacter target);
        void OnReceiveAttack(AttackContext context);
        void OnActivate();
        void OnDeactivate();
        bool IsChainAttack();
    }
}

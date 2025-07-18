namespace Playground.Script.BattleSystem
{
    using Playground.Components.Interfaces;
    using System;
    using Playground.Script;
    using Playground.Components;

    public interface IStance
    {
        IResource Resource { get; }
        StatModuleDecoratorManager StatDecoratorManager { get; }
        ActionModuleDecoratorManager ActionDecoratorManager { get; }
        SkillModuleDecoratorManager SkillDecoratorManager { get; }

        event Action<float>? CurrentResourceChanges, MaximumResourceChanges;
        void OnAttack(ICharacter target);
        void OnReceiveAttack(AttackContext context);
        void OnActivate();
        void OnDeactivate();
        bool IsChainAttack();
    }
}

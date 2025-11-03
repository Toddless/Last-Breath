namespace Core.Interfaces.Battle
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Components;
    using Core.Interfaces.Entity;

    public interface IStance
    {
        IResource Resource { get; }
        Stance StanceType { get; }
        IModuleManager<StatModule, IStatModule, StatModuleDecorator> StatDecoratorManager { get; }
        IModuleManager<ActionModule, IActionModule<IEntity>, ActionModuleDecorator> ActionDecoratorManager { get; }
        IModuleManager<SkillType, ISkillModule, SkillModuleDecorator> SkillDecoratorManager { get; }
        IStanceSkillComponent StanceSkillComponent { get; }

        event Action<float>? CurrentResourceChanges, MaximumResourceChanges;
        void OnAttack(IEntity target);
        void OnReceiveAttack(IAttackContext context);
        void OnActivate();
        void OnDeactivate();
        bool IsChainAttack();
    }
}

namespace LastBreath.Script.BattleSystem
{
    using System;
    using LastBreath.Components.Interfaces;
    using LastBreath.Script;
    using LastBreath.Components;
    using LastBreath.Script.BattleSystem.Decorators;
    using Core.Enums;
    using Core.Interfaces.Battle.Module;

    public interface IStance
    {
        IResource Resource { get; }
        Stance StanceType { get; }  
        ModuleManager<StatModule, IStatModule, StatModuleDecorator> StatDecoratorManager { get; }
        ModuleManager<ActionModule, IActionModule<ICharacter>, ActionModuleDecorator> ActionDecoratorManager { get; }
        ModuleManager<SkillType, ISkillModule, SkillModuleDecorator> SkillDecoratorManager { get; }
        StanceSkillComponent StanceSkillManager { get; }

        event Action<float>? CurrentResourceChanges, MaximumResourceChanges;
        void OnAttack(ICharacter target);
        void OnReceiveAttack(AttackContext context);
        void OnActivate();
        void OnDeactivate();
        bool IsChainAttack();
    }
}

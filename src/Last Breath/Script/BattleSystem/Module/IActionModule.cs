namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public interface IActionModule<T>
    {
        ActionModule SkillType { get; }
        DecoratorPriority Priority { get; }

        void PerformModuleAction(T parameter);
    }
}

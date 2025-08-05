namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;

    public interface IActionModule<T>
    {
        ActionModule SkillType { get; }
        DecoratorPriority Priority { get; }

        void PerformModuleAction(T parameter);
    }
}

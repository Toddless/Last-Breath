namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public interface IStatModule
    {
        StatModule ModuleType { get; }
        DecoratorPriority Priority { get; }

        float GetValue();
    }
}

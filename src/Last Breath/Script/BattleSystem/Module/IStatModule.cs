namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public interface IStatModule
    {
        StatModule Type { get; }
        DecoratorPriority Priority { get; }

        float GetValue();
    }
}

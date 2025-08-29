namespace Core.Interfaces.Battle.Module
{
    using Core.Enums;

    public interface IStatModule
    {
        StatModule SkillType { get; }
        DecoratorPriority Priority { get; }

        float GetValue();
    }
}

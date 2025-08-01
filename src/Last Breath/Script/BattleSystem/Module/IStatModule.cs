namespace LastBreath.Script.BattleSystem.Module
{
    using Contracts.Enums;

    public interface IStatModule
    {
        StatModule SkillType { get; }
        DecoratorPriority Priority { get; }

        float GetValue();
    }
}

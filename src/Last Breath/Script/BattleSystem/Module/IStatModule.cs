namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;

    public interface IStatModule
    {
        StatModule SkillType { get; }
        DecoratorPriority Priority { get; }

        float GetValue();
    }
}

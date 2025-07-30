namespace LastBreath.Script.BattleSystem.Module
{
    using LastBreath.Script.Enums;

    public interface IStatModule
    {
        StatModule SkillType { get; }
        DecoratorPriority Priority { get; }

        float GetValue();
    }
}

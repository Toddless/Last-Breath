namespace LastBreath.Script.Abilities.Interfaces
{
    using LastBreath.Script.BattleSystem;
    using LastBreath.Script.Enums;

    public interface IStanceSkill : ISkill
    {
        Stance RequiredStance { get; }
        void Activate(IStance stance);
        void Deactivate(IStance stance);
    }
}

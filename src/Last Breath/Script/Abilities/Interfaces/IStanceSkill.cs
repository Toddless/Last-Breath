namespace LastBreath.Script.Abilities.Interfaces
{
    using Core.Enums;
    using LastBreath.Script.BattleSystem;

    public interface IStanceSkill : ISkill
    {
        Stance RequiredStance { get; }
        void Activate(IStance stance);
        void Deactivate(IStance stance);
    }
}

namespace Core.Interfaces.Battle
{
    using Core.Enums;
    using Core.Interfaces.Skills;

    public interface IStanceSkill : ISkill
    {
        Stance RequiredStance { get; }
        void Activate(IStance stance);
        void Deactivate(IStance stance);
    }
}

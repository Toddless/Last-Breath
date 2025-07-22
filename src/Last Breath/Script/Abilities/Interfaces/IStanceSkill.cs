namespace Playground.Script.Abilities.Interfaces
{
    using Playground.Script.BattleSystem;
    using Playground.Script.Enums;

    public interface IStanceSkill : ISkill
    {
        Stance RequiredStance { get; }
        void Activate(IStance stance);

        void Deactivate(IStance stance);
    }
}

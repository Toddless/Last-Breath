namespace LastBreath.Script.Abilities.Interfaces
{
    using Core.Interfaces.Skills;
    using LastBreath.Script;

    public interface ITargetSkill : ISkill
    {
        void Activate(ICharacter target);
        void Deactivate(ICharacter target);
    }
}

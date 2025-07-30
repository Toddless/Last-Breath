namespace LastBreath.Script.Abilities.Interfaces
{
    using LastBreath.Script;

    public interface ITargetSkill : ISkill
    {
        void Activate(ICharacter target);
        void Deactivate(ICharacter target);
    }
}

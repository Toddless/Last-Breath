namespace Core.Interfaces.Skills
{
    using Core.Interfaces;

    public interface ITargetSkill : ISkill
    {
        void Activate(ICharacter target);
        void Deactivate(ICharacter target);
    }
}

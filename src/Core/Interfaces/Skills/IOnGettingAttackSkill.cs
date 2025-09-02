namespace Core.Interfaces.Skills
{
    using Core.Interfaces;

    public interface IOnGettingAttackSkill : ISkill
    {
        void Activate(ICharacter target);
    }
}

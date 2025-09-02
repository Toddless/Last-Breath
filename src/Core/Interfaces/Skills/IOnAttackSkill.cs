namespace Core.Interfaces.Skills
{
    using Core.Interfaces;

    public interface IOnAttackSkill : ISkill
    {
        void Activate(ICharacter target);
    }
}

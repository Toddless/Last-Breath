namespace Core.Interfaces.Skills
{
    using Core.Interfaces.Battle;

    public interface IPreAttackSkill : ISkill
    {
        void Activate(IAttackContext context);
    }
}

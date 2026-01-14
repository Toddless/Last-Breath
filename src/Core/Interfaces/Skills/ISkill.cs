namespace Core.Interfaces.Skills
{
    using Entity;

    public interface ISkill : IIdentifiable, IDisplayable
    {
        void Attach(IEntity owner);
        void Detach(IEntity owner);

        ISkill Copy();
        bool IsStronger(ISkill skill);
    }
}

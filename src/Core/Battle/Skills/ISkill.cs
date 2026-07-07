namespace Core.Battle.Skills
{
    using Entity;
    using Interfaces;

    public interface ISkill : IIdentifiable, IDisplayable
    {
        void Attach(IFightable owner);
        void Detach(IFightable owner);

        ISkill Copy();
        bool IsStronger(ISkill skill);
    }
}

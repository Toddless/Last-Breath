namespace Core.Interfaces.Skills
{
    using Entity;

    public interface ISkill : IIdentifiable, IDisplayable
    {
        void Attach(IFightable owner);
        void Detach(IFightable owner);

        ISkill Copy();
        bool IsStronger(ISkill skill);
    }
}

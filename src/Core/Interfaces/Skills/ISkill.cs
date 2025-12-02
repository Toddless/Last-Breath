namespace Core.Interfaces.Skills
{
    using Enums;
    using Entity;

    public interface ISkill : IIdentifiable, IDisplayable
    {
        SkillType Type { get; }

        void Attach(IEntity owner);
        void Detach(IEntity owner);

        ISkill Copy();
        bool IsStronger(ISkill skill);
    }
}

namespace Core.Interfaces.Skills
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public interface ISkill : IIdentifiable, IDisplayable
    {
        SkillType Type { get; }

        void Attach(IEntity owner);
        void Detach();

        ISkill? Copy();
    }
}

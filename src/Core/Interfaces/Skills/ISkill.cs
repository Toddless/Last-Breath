namespace Core.Interfaces.Skills
{
    using Core.Enums;

    public interface ISkill : IIdentifiable, IDisplayable
    {
        SkillType Type { get; }

        void Attach(ICharacter owner);
        void Detach();

        ISkill? Copy();
    }
}

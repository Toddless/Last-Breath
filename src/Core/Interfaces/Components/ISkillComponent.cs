namespace Core.Interfaces.Components
{
    using Enums;
    using Skills;
    using System.Collections.Generic;

    public interface ISkillComponent<T> where T : ISkill
    {
        void AddSkill(T skill);
        List<T> GetSkills(SkillType type);
        void RemoveSkill(T skill);
    }
}

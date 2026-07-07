namespace Core.Components
{
    using System.Collections.Generic;
    using Battle.Skills;
    using Enums;

    public interface ISkillComponent<T> where T : ISkill
    {
        void AddSkill(T skill);
        List<T> GetSkills(SkillType type);
        void RemoveSkill(T skill);
    }
}

namespace Core.Interfaces.Components
{
    using System.Collections.Generic;
    using Enums;
    using Skills;

    public interface IBaseSkillComponent<T> where T : ISkill
    {
        void AddSkill(T skill);
        List<T> GetSkills(SkillType type);
        void RemoveSkill(T skill);
    }
}

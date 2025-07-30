namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    public abstract class BaseSkillComponent<T>
        where T : ISkill
    {
        protected readonly Dictionary<SkillType, List<T>> Skills;

        protected BaseSkillComponent()
        {
            Skills = Enum.GetValues<SkillType>().ToDictionary(type => type, _ => new List<T>());
        }

        public virtual void AddSkill(T skill)
        {
            if (!Skills.TryGetValue(skill.Type, out var skills))
            {
                skills = [];
                Skills.Add(skill.Type, skills);
            }

            if (AlreadyHaveThisSkill(skills, skill))
            {
                // TODO: Log. Event?
                return;
            }
            ActivateSkill(skill);
            skills.Add(skill);
        }

        public virtual void RemoveSkill(T skill)
        {
            DeactivateSkill(skill);
            if (!Skills[skill.Type].Remove(skill))
            {
                //TODO: Log
            }
        }

        public virtual List<T> GetSkills(SkillType type)
        {
            if (!Skills.TryGetValue(type, out var skills))
            {
                return [];
            }
            return skills;
        }

        /// <summary>
        /// Activate permanent passive skill.
        /// </summary>
        /// <param name="skill"></param>
        protected abstract void ActivateSkill(T skill);
        /// <summary>
        /// Deactivate permanent passive skill.
        /// </summary>
        /// <param name="skill"></param>
        protected abstract void DeactivateSkill(T skill);

        protected bool AlreadyHaveThisSkill(List<T> skills, T newSkill) => skills.Any(skill => skill.Id == newSkill.Id);
    }
}

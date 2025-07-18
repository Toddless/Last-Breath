namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    public class SkillsComponent
    {
        private readonly ICharacter _owner;
        private readonly Dictionary<SkillType, List<ISkill>> _skills;

        public SkillsComponent(ICharacter owner)
        {
            _owner = owner;
            _skills = Enum.GetValues<SkillType>().ToDictionary(type => type, _ => new List<ISkill>());
        }

        public void AddSkill(ISkill skill)
        {
            // do not add same skill twice
            if (!_skills.TryGetValue(skill.Type, out var skills))
            {
                skills = [];
                _skills.Add(skill.Type, skills);
            }

            if (AlreadyHaveThisSkill(skills, skill))
            {
                // TODO: Log. Event?
                return;
            }
            ActivateTargetSkill(skill);
            skills.Add(skill);
            //TODO RaiseEvent?
        }

        public void RemoveSkill(ISkill skill)
        {
            DeactivateTargetSkill(skill);
            if (!_skills[skill.Type].Remove(skill))
            {
                //TODO: Log
            }
        }

        public List<ISkill> GetSkills(SkillType type)
        {
            if (!_skills.TryGetValue(type, out var skills))
            {
                return [];
            }
            return skills;
        }

        private void ActivateTargetSkill(ISkill skill)
        {
            if (skill.Type != SkillType.AlwaysActive || skill is not ITargetSkill targetSkill) return;
            targetSkill.Activate(_owner);
        }

        private void DeactivateTargetSkill(ISkill skill)
        {
            if (skill.Type != SkillType.AlwaysActive || skill is not ITargetSkill targetSkill) return;
            targetSkill.Deactivate(_owner);
        }

        private bool AlreadyHaveThisSkill(List<ISkill> skills, ISkill skill) => skills.Any(skill => skill.Type == skill.Type);
    }
}

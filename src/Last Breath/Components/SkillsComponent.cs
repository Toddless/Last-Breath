namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using Playground.Script;
    using Playground.Script.Abilities.Skills;
    using Playground.Script.Enums;

    public class SkillsComponent
    {
        private readonly ICharacter _owner;
        private Dictionary<SkillType, List<ISkill>> _skills = [];

        public event Action<ISkill>? AlreadyExist;

        public SkillsComponent(ICharacter owner)
        {
            _owner = owner;
            // должен реагировать в зависимости от SkillType
        }

        public void AddSkill(ISkill skill)
        {
            if (!_skills.TryGetValue(skill.SkillType, out List<ISkill>? skills))
            {
                skills = [];
            }

            if (skills.Contains(skill))
            {
                AlreadyExist?.Invoke(skill);
                return;
            }

            skill.OnObtaining(_owner);
            _skills[skill.SkillType] = skills;
        }

        public void RemoveSkill(ISkill skill)
        {
            if (!_skills.TryGetValue(skill.SkillType, out List<ISkill>? skills))
            {
                // TODO: log
                return;
            }

            if (!skills.Contains(skill))
            {
                // TODO: Log
                return;
            }
            skill.OnLoss();

            skills.Remove(skill);
        }

    }
}

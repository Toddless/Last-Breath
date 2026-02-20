namespace Battle.Source.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Components;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public class PassiveSkillsComponent(IEntity owner) : IPassiveSkillsComponent
    {
        private readonly Dictionary<string, ISkill> _skills = new();

        public IReadOnlyList<ISkill> Skills => _skills.Values.ToList();

        public event Action<ISkill>? SkillAdded;
        public event Action<ISkill>? SkillDeleted;

        public void AddSkill(ISkill skill)
        {
            if (_skills.TryGetValue(skill.Id, out var existingSkill))
            {
                if (existingSkill.IsStronger(skill)) return;

                existingSkill.Detach(owner);
                _skills.Remove(existingSkill.Id);
            }

            _skills[skill.Id] = skill;
            skill.Attach(owner);
            SkillAdded?.Invoke(skill);
        }

        public void RemoveSkill(string id)
        {
            if (!_skills.TryGetValue(id, out var skill)) return;

            skill.Detach(owner);
            _skills.Remove(id);
            SkillDeleted?.Invoke(skill);
        }

        public ISkill? GetSkill(string id) => _skills.GetValueOrDefault(id);
    }
}

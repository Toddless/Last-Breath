namespace Core.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Skills;
    using Entity;

    public class PassiveSkillsComponent(IFightable owner) : IPassiveSkillsComponent
    {
        private readonly Dictionary<string, ISkill> _skills = new();

        public IReadOnlyList<ISkill> Skills => _skills.Values.ToList();
        public bool IsSuppressed { get; private set; }

        public event Action<ISkill>? SkillAdded;
        public event Action<ISkill>? SkillDeleted;
        public event Action<bool>? SuppressionChanged;

        public void AddSkill(ISkill skill)
        {
            if (_skills.TryGetValue(skill.Id, out var existingSkill))
            {
                if (existingSkill.IsStronger(skill)) return;

                DetachIfActive(existingSkill);
                _skills.Remove(existingSkill.Id);
            }

            _skills[skill.Id] = skill;
            AttachIfActive(skill);
            SkillAdded?.Invoke(skill);
        }

        public void RemoveSkill(string id)
        {
            if (!_skills.TryGetValue(id, out var skill)) return;

            DetachIfActive(skill);
            _skills.Remove(id);
            SkillDeleted?.Invoke(skill);
        }

        public void Suppress()
        {
            if (IsSuppressed) return;
            IsSuppressed = true;
            foreach (ISkill skill in _skills.Values)
                skill.Detach(owner);
            SuppressionChanged?.Invoke(true);
        }

        public void Resume()
        {
            if (!IsSuppressed) return;
            IsSuppressed = false;
            foreach (ISkill skill in _skills.Values)
                skill.Attach(owner);
            SuppressionChanged?.Invoke(false);
        }

        public ISkill? GetSkill(string id) => _skills.GetValueOrDefault(id);

        private void AttachIfActive(ISkill skill)
        {
            if (!IsSuppressed) skill.Attach(owner);
        }

        private void DetachIfActive(ISkill skill)
        {
            if (!IsSuppressed) skill.Detach(owner);
        }
    }
}

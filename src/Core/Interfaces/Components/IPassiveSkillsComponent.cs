namespace Core.Interfaces.Components
{
    using System;
    using Skills;
    using System.Collections.Generic;

    public interface IPassiveSkillsComponent
    {
        IReadOnlyList<ISkill> Skills { get; }

        event Action<ISkill>? SkillAdded;
        event Action<ISkill>? SkillDeleted;

        void AddSkill(ISkill skill);
        void RemoveSkill(string id);

        ISkill? GetSkill(string id);
    }
}

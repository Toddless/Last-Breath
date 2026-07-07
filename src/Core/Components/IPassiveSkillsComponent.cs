namespace Core.Components
{
    using System;
    using System.Collections.Generic;
    using Battle.Skills;

    public interface IPassiveSkillsComponent
    {
        IReadOnlyList<ISkill> Skills { get; }

        /// <summary>While suppressed, no passive skill is attached; skills stay learned and reattach on <see cref="Resume"/>.</summary>
        bool IsSuppressed { get; }

        event Action<ISkill>? SkillAdded;
        event Action<ISkill>? SkillDeleted;

        /// <summary>Raised with <c>true</c> on <see cref="Suppress"/> and <c>false</c> on <see cref="Resume"/>.
        /// External passive owners (stances) listen to it to detach/reattach their own skill lists.</summary>
        event Action<bool>? SuppressionChanged;

        void AddSkill(ISkill skill);
        void RemoveSkill(string id);
        void Suppress();
        void Resume();

        ISkill? GetSkill(string id);
    }
}

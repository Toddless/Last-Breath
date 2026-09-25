namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using Battle.Skills;

    public interface IPassiveSkillsComponent
    {
        IReadOnlyList<ISkill> Skills { get; }

        /// <summary>While suppressed, no passive skill is attached; skills stay learned and reattach on <see cref="Resume"/>.</summary>
        bool IsSuppressed { get; }

        /// <summary>Raised when a skill takes the active slot of its Id, and its counterpart when one
        /// leaves it — a version displaced by a stronger registration is reported as deleted.</summary>
        event Action<ISkill>? SkillAdded;
        event Action<ISkill>? SkillDeleted;

        /// <summary>Raised with <c>true</c> on <see cref="Suppress"/> and <c>false</c> on <see cref="Resume"/>.
        /// External passive owners (stances) listen to it to detach/reattach their own skill lists.</summary>
        event Action<bool>? SuppressionChanged;

        /// <summary>Registers one source's instance of a skill. Registrations of the same Id coexist;
        /// only the strongest of them is attached to the owner.</summary>
        void AddSkill(ISkill skill);

        /// <summary>Takes back the instance the caller registered — never every skill sharing its Id.
        /// A source may only remove what it added, so unequipping an item leaves a passive-tree node's
        /// contribution of the same Id in effect.</summary>
        void RemoveSkill(ISkill skill);

        void Suppress();
        void Resume();

        /// <summary>The registration of the Id that is currently in effect, or null if no source grants it.</summary>
        ISkill? GetSkill(string id);
    }
}

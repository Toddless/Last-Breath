namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Skills;
    using Entity;

    /// <summary>
    /// Passive skills of an entity. One skill Id can be handed out by several independent sources at once
    /// (an item grant, a passive-tree node, an NPC kit): every registration is kept, but only the strongest
    /// registration of an Id is attached to the owner. A source gives back the very instance it registered,
    /// so unequipping an item never takes another source's contribution with it — the next strongest
    /// registration simply takes over.
    /// </summary>
    public class PassiveSkillsComponent(IFightable owner) : IPassiveSkillsComponent
    {
        private readonly Dictionary<string, List<ISkill>> _registrations = new();
        private readonly Dictionary<string, ISkill> _active = new();

        public IReadOnlyList<ISkill> Skills => _active.Values.ToList();
        public bool IsSuppressed { get; private set; }

        public event Action<ISkill>? SkillAdded;
        public event Action<ISkill>? SkillDeleted;
        public event Action<bool>? SuppressionChanged;

        public void AddSkill(ISkill skill)
        {
            if (!_registrations.TryGetValue(skill.Id, out List<ISkill>? registrations))
                _registrations[skill.Id] = registrations = [];

            if (registrations.Exists(registration => ReferenceEquals(registration, skill))) return;

            registrations.Add(skill);
            RefreshActive(skill.Id);
        }

        public void RemoveSkill(ISkill skill)
        {
            if (!_registrations.TryGetValue(skill.Id, out List<ISkill>? registrations)) return;

            int index = registrations.FindIndex(registration => ReferenceEquals(registration, skill));
            if (index < 0) return;

            registrations.RemoveAt(index);
            if (registrations.Count == 0) _registrations.Remove(skill.Id);
            RefreshActive(skill.Id);
        }

        public void Suppress()
        {
            if (IsSuppressed) return;

            IReadOnlyList<ISkill> attached = Skills;
            IsSuppressed = true;
            foreach (ISkill skill in attached)
                skill.Detach(owner);
            SuppressionChanged?.Invoke(true);
        }

        public void Resume()
        {
            if (!IsSuppressed) return;

            IsSuppressed = false;
            foreach (ISkill skill in Skills)
                if (IsActive(skill))
                    skill.Attach(owner);
            SuppressionChanged?.Invoke(false);
        }

        public ISkill? GetSkill(string id) => _active.GetValueOrDefault(id);

        /// <summary>Moves the attachment of an Id onto its strongest surviving registration: at most one
        /// instance of an Id is ever attached, and a change costs exactly one detach and one attach.</summary>
        private void RefreshActive(string id)
        {
            ISkill? next = _registrations.TryGetValue(id, out List<ISkill>? registrations)
                ? Strongest(registrations)
                : null;
            ISkill? current = _active.GetValueOrDefault(id);
            if (ReferenceEquals(current, next)) return;

            if (next == null) _active.Remove(id);
            else _active[id] = next;

            if (current != null)
            {
                DetachIfActive(current);
                SkillDeleted?.Invoke(current);
            }

            if (next == null) return;
            AttachIfActive(next);
            SkillAdded?.Invoke(next);
        }

        /// <summary>The <see cref="ISkill.IsStronger"/> collision policy: a registration holds the slot only
        /// while it reports itself stronger than the challenger, so equally strong versions are won by the
        /// one that arrived last.</summary>
        private static ISkill? Strongest(List<ISkill> registrations)
        {
            ISkill? winner = null;
            foreach (ISkill candidate in registrations)
                winner = winner != null && winner.IsStronger(candidate) ? winner : candidate;
            return winner;
        }

        /// <summary>A skill may edit the roster from inside its own Attach, so <see cref="Resume"/> walks a
        /// snapshot and reattaches only what is still the active registration of its Id.</summary>
        private bool IsActive(ISkill skill) => ReferenceEquals(_active.GetValueOrDefault(skill.Id), skill);

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

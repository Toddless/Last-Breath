namespace Core.Entity.NpcModifiers
{
    using System.Collections.Generic;
    using System.Linq;
    using Entity;
    using Enums;

    public class NpcModifiersComponent(IFightable owner) : INpcModifiersComponent
    {
        private readonly List<INpcModifier> _modifiers = [];
        private INpcBuffBinder? _buffs;

        public IReadOnlyList<INpcModifier> AllModifiers => _modifiers;

        public event System.Action<INpcModifier>? ModifierAdded;

        /// <summary>Gives the list a bearer side. Until this is called the modifiers are loot-side only —
        /// which is what a stand without the buff catalog (the drop simulator) wants.</summary>
        public void UseBuffs(INpcBuffBinder binder)
        {
            // Two binders would each own half the bearer's buffs and neither could take its own off again.
            if (_buffs != null)
                throw new System.InvalidOperationException("This NPC already has a buff binder; a second one would split ownership of its buffs.");

            _buffs = binder;
            binder.Rebuild(owner, _modifiers);
        }

        public void AddModifiers(List<INpcModifier> modifiers)
        {
            modifiers.ForEach(Add);
            _buffs?.Rebuild(owner, _modifiers);
        }

        public void AddModifier(INpcModifier modifier)
        {
            Add(modifier);
            _buffs?.Rebuild(owner, _modifiers);
        }

        public void RemoveModifier(string instanceId)
        {
            var modifier = _modifiers.FirstOrDefault(x => x.InstanceId == instanceId);
            if (modifier == null) return;

            Drop(modifier);
            _buffs?.Rebuild(owner, _modifiers);
        }

        // The whole batch binds once, after the last modifier is in: the scaling modifiers have raised
        // everyone's TotalScale by then, so nothing is bound at a scale that is already stale.
        private void Add(INpcModifier modifier)
        {
            if (!MakeRoomFor(modifier)) return;

            modifier.Attach(owner);
            _modifiers.Add(modifier);
            ModifierAdded?.Invoke(modifier);
        }

        /// <summary>
        /// "Уникальные модификаторы не складываются (более сильные заменяют более слабые)". HOW WIDE that
        /// reads is the designer's call, authored per catalog section as uniqueScope, because the vault
        /// answers it differently section by section: the rarity floors are one at a time (three floors is
        /// three difficulties for one floor's worth of effect), while the scaling section says outright
        /// "Нпс может иметь несколько разных модификаторов данного типа" — ×2 health beside ×2 damage, and
        /// only the same scaler twice refused.
        /// <para>Difficulty is what "stronger" means, and the BASE one at that: the scaled figure would let
        /// the order modifiers arrive in decide the winner. A tie keeps the incumbent, and a modifier with
        /// no group (built by hand, not from the catalog) falls back to the id comparison rather than
        /// colliding with everything.</para>
        /// </summary>
        private bool MakeRoomFor(INpcModifier modifier)
        {
            if (!modifier.IsUnique) return true;

            var rival = _modifiers.FirstOrDefault(existing => existing.IsUnique && SameKind(existing, modifier));
            if (rival == null) return true;
            if (rival.BaseDifficultyMultiplier >= modifier.BaseDifficultyMultiplier) return false;

            Drop(rival);
            return true;
        }

        // Either side asking for the narrow reading gets it: a section-wide slot may only be claimed when
        // both entries agree they are section-wide.
        private static bool SameKind(INpcModifier existing, INpcModifier incoming) =>
            existing.UniqueScope == NpcUniqueScope.Id
            || incoming.UniqueScope == NpcUniqueScope.Id
            || string.IsNullOrEmpty(existing.Group)
            || string.IsNullOrEmpty(incoming.Group)
                ? existing.Id == incoming.Id
                : existing.Group == incoming.Group;

        private void Drop(INpcModifier modifier)
        {
            modifier.Detach(owner);
            _modifiers.Remove(modifier);
        }
    }
}

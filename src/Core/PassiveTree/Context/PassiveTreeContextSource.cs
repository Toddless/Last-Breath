namespace Core.PassiveTree.Context
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Entity;
    using Enums;
    using Modifiers;
    using Modifiers.Conditions;
    using Modifiers.Context;

    /// <summary>
    /// Default <see cref="IPassiveTreeContextSource"/>. Differs from
    /// <see cref="PassiveTreeParameterSource"/> in how it reaches a fighter: parameters are pulled from a
    /// registered source, context knobs are pushed into the fighter's handler, so this one must be
    /// explicitly attached/detached (<see cref="Detach"/> is the whole of the cleanup). Totals are
    /// answered lazily rather than stored, so <see cref="Rebuild"/> only recomputes sums — nothing is
    /// re-attached, a whole-number knob rounds once, and a conditional line drops out the moment its
    /// condition does with no modifier touched.
    /// </summary>
    public sealed class PassiveTreeContextSource(IConditionProvider conditions) : IPassiveTreeContextSource
    {
        private readonly Dictionary<ContextParameter, Knob> _knobs = [];

        private IFightable? _owner;

        /// <summary>The fighter the contribution was just handed to, or null when taken back. Announced
        /// because predicates on the other tree half watch that same fighter.</summary>
        public event Action<IFightable?>? OwnerChanged;

        /// <summary>A snapshot, not the live key set — a reaction that triggers another
        /// <see cref="Rebuild"/> mid-walk would otherwise invalidate the collection.</summary>
        public IReadOnlyCollection<ContextParameter> Knobs => _knobs.Keys.ToHashSet();

        public float ValueOf(ContextParameter parameter) =>
            _knobs.TryGetValue(parameter, out Knob? knob) ? knob.Entry.Value : 0f;

        /// <summary>Replaces the contribution with what the given nodes carry. A knob keeping at least one
        /// line keeps its modifier and only updates what it reads; a knob losing its last line loses the
        /// modifier too, which is what makes a refund exact.</summary>
        public void Rebuild(PassiveTreeDocument document, IEnumerable<string> taken)
        {
            Dictionary<ContextParameter, List<TreeContextLine>> gathered = ContextKnobTotals.Gather(document, taken, conditions);

            foreach (ContextParameter parameter in _knobs.Keys.ToArray())
            {
                if (gathered.ContainsKey(parameter)) continue;

                Release(_knobs[parameter]);
                _knobs.Remove(parameter);
            }

            foreach (KeyValuePair<ContextParameter, List<TreeContextLine>> pair in gathered)
            {
                if (!_knobs.TryGetValue(pair.Key, out Knob? knob))
                {
                    knob = new Knob(pair.Key);
                    _knobs[pair.Key] = knob;
                    if (_owner != null) knob.Entry.Attach(_owner);
                }

                knob.Replace(pair.Value, _owner);
            }
        }

        /// <summary>Hands the contribution to a fighter. One fighter carries it at a time: attaching to
        /// another moves every modifier over instead of leaving a copy, which is what a scene change needs
        /// when the world and the arena build separate player objects from one tree service.</summary>
        public void Attach(IFightable owner)
        {
            _owner = owner;
            foreach (Knob knob in _knobs.Values)
            {
                knob.Entry.Attach(owner);
                knob.Watch(owner);
            }

            OwnerChanged?.Invoke(owner);
        }

        /// <summary>Takes the contribution back off a fighter. A fighter that no longer carries it
        /// (already moved to a successor) detaches to nothing, so a late cleanup can't strip whoever holds
        /// it now.</summary>
        public void Detach(IFightable owner)
        {
            if (!ReferenceEquals(_owner, owner)) return;

            foreach (Knob knob in _knobs.Values)
            {
                knob.Entry.Detach(owner);
                knob.Unwatch();
            }

            _owner = null;
            OwnerChanged?.Invoke(null);
        }

        private void Release(Knob knob)
        {
            knob.Unwatch();
            if (_owner != null) knob.Entry.Detach(_owner);
        }

        /// <summary>
        /// One knob and the lines feeding it. The entry is built once and kept — the handle on the
        /// fighter's modifier — and reads lines through <see cref="ContextModifierEntry.Live"/>, so the
        /// list can be rewritten under it on every allocation change without touching the modifier.
        /// <para>A switch knob binds to no number at all: one taken line is the whole rule, and a switch
        /// whose lines are all gated off reads as off.</para>
        /// <para>Slot defaults to <see cref="ContextModifierPriority.Normal"/> so a build stands ahead of
        /// passing tweaks rather than landing among them in attach order; a knob whose modifier already
        /// picked its own slot keeps it.</para>
        /// </summary>
        private sealed class Knob
        {
            private readonly List<TreeContextLine> _lines = [];

            public ContextModifierEntry Entry { get; }

            public Knob(ContextParameter parameter) =>
                Entry = new ContextModifierEntry(parameter, BucketOf(parameter), 0f)
                {
                    Priority = ContextModifierPriority.Innate,
                    Live = () => ContextKnobTotals.AsRead(parameter, _lines)
                };

            /// <summary>Swaps in the new lines, releasing the old ones' predicates first — they watch a
            /// fighter, and lines no longer counted must stop listening to him.</summary>
            public void Replace(List<TreeContextLine> lines, IFightable? owner)
            {
                Unwatch();
                _lines.Clear();
                _lines.AddRange(lines);
                if (owner != null) Watch(owner);
            }

            public void Watch(IFightable owner)
            {
                foreach (TreeContextLine line in _lines) line.Attach(owner);
            }

            public void Unwatch()
            {
                foreach (TreeContextLine line in _lines) line.Detach();
            }

            /// <summary>Which bucket the entry is built with. Bindings are chosen by parameter and read
            /// the value, so this says only what kind of knob it is — a switch is never described as an
            /// amount.</summary>
            private static ModifierValueType BucketOf(ContextParameter parameter) =>
                ContextKnobs.IsFlag(parameter) ? ModifierValueType.Flag : ModifierValueType.Increase;
        }
    }
}

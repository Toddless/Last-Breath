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
    /// The pipeline contribution of the taken nodes: one modifier per knob, reading the total of every
    /// node that feeds it. Never one modifier per node — a pipeline applies its modifiers one after
    /// another instead of adding them up, so twenty nodes wearing "+10%" separately would compound into a
    /// factor nobody authored.
    /// <para>The total is answered when a pipeline reads it and not stored when the allocation changed,
    /// which is what lets an allocation change be a rebuild of the sums with nothing re-attached, what
    /// makes a whole-number knob round its total once instead of once per node, and what lets a line held
    /// up by a condition drop out of the sum the moment the condition does — no modifier is touched.</para>
    /// <para>The counterpart of <see cref="PassiveTreeParameterSource"/> for the lines that never enter
    /// parameter resolution. It differs in how it reaches a fighter: parameters are pulled from a
    /// registered source, context knobs are pushed into the fighter's handler, so this one has to be
    /// attached and detached — <see cref="Detach"/> is the whole of the cleanup.</para>
    /// </summary>
    public sealed class PassiveTreeContextSource(IConditionProvider conditions) : IPassiveTreeContextSource
    {
        private readonly Dictionary<ContextParameter, Knob> _knobs = [];

        private IFightable? _owner;

        /// <summary>The fighter the contribution has just been handed to, or null when it has been taken
        /// back. Announced rather than kept to itself: this is the only half of the tree a fighter is
        /// handed to, and the predicates of the other half watch that same fighter.</summary>
        public event Action<IFightable?>? OwnerChanged;

        /// <summary>A snapshot, not the live key set: a consumer walks these to see what the tree feeds,
        /// and a reaction that ends in another <see cref="Rebuild"/> would invalidate the collection
        /// mid-walk.</summary>
        public IReadOnlyCollection<ContextParameter> Knobs => _knobs.Keys.ToHashSet();

        public float ValueOf(ContextParameter parameter) =>
            _knobs.TryGetValue(parameter, out Knob? knob) ? knob.Entry.Value : 0f;

        /// <summary>
        /// Replaces the contribution with what the given nodes carry. A knob that keeps at least one line
        /// keeps the modifier it already has and only changes what that modifier reads; a knob that lost
        /// its last line loses the modifier as well, which is what makes a refund exact.
        /// </summary>
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

        /// <summary>
        /// Hands the contribution to a fighter. One fighter carries it at a time: attaching to another
        /// moves every modifier over instead of leaving a copy behind, which is what a scene change needs
        /// — the world and the arena build their own player objects out of one tree service.
        /// </summary>
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

        /// <summary>Takes the contribution back off a fighter. A fighter that no longer carries it — one
        /// whose modifiers were already moved to its successor — detaches to nothing, so a late cleanup
        /// cannot strip the fighter that holds the contribution now.</summary>
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
        /// One knob and the lines feeding it. The entry is built once and kept: it is the handle on the
        /// modifier that was added to the fighter, and it reads the lines through
        /// <see cref="ContextModifierEntry.Live"/>, so the list can be rewritten under it as often as the
        /// allocation changes without the modifier being touched.
        /// <para>A switch knob is held the same way. Its binding reads no number at all, so what its lines
        /// add up to never leaves this object — one taken switch is the whole of the rule, and a switch
        /// whose only lines are waiting on a condition adds up to nothing and reads as off.</para>
        /// <para>The slot the entry asks for covers the knobs that named none: what the build is has to
        /// stand ahead of the passing tweaks at <see cref="ContextModifierPriority.Normal"/> rather than
        /// land among them in whatever order things were attached. A knob whose modifier picked its own
        /// slot is left in it — a node carrying a conversion must not drag it off the end of the pipeline
        /// it was written to see.</para>
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

            /// <summary>Takes the new set of lines, releasing the predicates of the old one first: they
            /// watch a fighter, and lines that are no longer counted must stop listening to him.</summary>
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

            /// <summary>The bucket the entry is built with. Nothing on this path reads it — bindings are
            /// chosen by the parameter and read the value — so it says no more than which kind of knob
            /// this is, and a switch is never described as an amount.</summary>
            private static ModifierValueType BucketOf(ContextParameter parameter) =>
                ContextKnobs.IsFlag(parameter) ? ModifierValueType.Flag : ModifierValueType.Increase;
        }
    }
}

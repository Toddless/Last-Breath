namespace Core.PassiveTree.Context
{
    using System.Collections.Generic;
    using System.Linq;
    using Entity;
    using Enums;
    using Modifiers;
    using Modifiers.Context;

    /// <summary>
    /// The pipeline contribution of the taken nodes: one modifier per knob, reading the total of every
    /// node that feeds it. Never one modifier per node — a pipeline applies its modifiers one after
    /// another instead of adding them up, so twenty nodes wearing "+10%" separately would compound into a
    /// factor nobody authored.
    /// <para>The total is answered when a pipeline reads it and not stored when the allocation changed,
    /// which is what lets an allocation change be a rebuild of the sums with nothing re-attached, and what
    /// makes a whole-number knob round its total once instead of once per node.</para>
    /// <para>The counterpart of <see cref="PassiveTreeParameterSource"/> for the lines that never enter
    /// parameter resolution. It differs in how it reaches a fighter: parameters are pulled from a
    /// registered source, context knobs are pushed into the fighter's handler, so this one has to be
    /// attached and detached — <see cref="Detach"/> is the whole of the cleanup.</para>
    /// </summary>
    public sealed class PassiveTreeContextSource : IPassiveTreeContextSource
    {
        private readonly Dictionary<ContextParameter, Knob> _knobs = [];

        private IFightable? _owner;

        /// <summary>A snapshot, not the live key set: a consumer walks these to see what the tree feeds,
        /// and a reaction that ends in another <see cref="Rebuild"/> would invalidate the collection
        /// mid-walk.</summary>
        public IReadOnlyCollection<ContextParameter> Knobs => _knobs.Keys.ToHashSet();

        /// <summary>
        /// Replaces the contribution with what the given nodes carry. A knob that keeps at least one line
        /// keeps the modifier it already has and only changes what that modifier reads; a knob that lost
        /// its last line loses the modifier as well, which is what makes a refund exact.
        /// </summary>
        public void Rebuild(PassiveTreeDocument document, IEnumerable<string> taken)
        {
            Dictionary<ContextParameter, List<ContextModifierLine>> gathered = ContextKnobTotals.Gather(document, taken);

            foreach (ContextParameter parameter in _knobs.Keys.ToArray())
            {
                if (gathered.ContainsKey(parameter)) continue;

                Release(_knobs[parameter]);
                _knobs.Remove(parameter);
            }

            foreach (KeyValuePair<ContextParameter, List<ContextModifierLine>> pair in gathered)
            {
                if (!_knobs.TryGetValue(pair.Key, out Knob? knob))
                {
                    knob = new Knob(pair.Key);
                    _knobs[pair.Key] = knob;
                    if (_owner != null) knob.Entry.Attach(_owner);
                }

                knob.Lines.Clear();
                knob.Lines.AddRange(pair.Value);
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
            foreach (Knob knob in _knobs.Values) knob.Entry.Attach(owner);
        }

        /// <summary>Takes the contribution back off a fighter. A fighter that no longer carries it — one
        /// whose modifiers were already moved to its successor — detaches to nothing, so a late cleanup
        /// cannot strip the fighter that holds the contribution now.</summary>
        public void Detach(IFightable owner)
        {
            if (!ReferenceEquals(_owner, owner)) return;

            foreach (Knob knob in _knobs.Values) knob.Entry.Detach(owner);
            _owner = null;
        }

        private void Release(Knob knob)
        {
            if (_owner != null) knob.Entry.Detach(_owner);
        }

        /// <summary>
        /// One knob and the lines feeding it. The entry is built once and kept: it is the handle on the
        /// modifier that was added to the fighter, and it reads the lines through
        /// <see cref="ContextModifierEntry.Live"/>, so the list can be rewritten under it as often as the
        /// allocation changes without the modifier being touched.
        /// <para>A switch knob is held the same way. Its binding reads no number at all, so what its lines
        /// add up to never leaves this object — one taken switch is the whole of the rule.</para>
        /// </summary>
        private sealed class Knob
        {
            public List<ContextModifierLine> Lines { get; } = [];

            public ContextModifierEntry Entry { get; }

            public Knob(ContextParameter parameter) =>
                Entry = new ContextModifierEntry(parameter, BucketOf(parameter), 0f)
                {
                    Priority = ContextModifierPriority.Innate,
                    Live = () => ContextKnobTotals.Sum(Lines)
                };

            /// <summary>The bucket the entry is built with. Nothing on this path reads it — bindings are
            /// chosen by the parameter and read the value — so it says no more than which kind of knob
            /// this is, and a switch is never described as an amount.</summary>
            private static ModifierValueType BucketOf(ContextParameter parameter) =>
                ContextKnobs.IsFlag(parameter) ? ModifierValueType.Flag : ModifierValueType.Increase;
        }
    }
}

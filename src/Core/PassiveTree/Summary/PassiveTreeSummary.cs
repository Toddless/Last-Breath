namespace Core.PassiveTree.Summary
{
    using System.Collections.Generic;
    using System.Linq;
    using Allocation;
    using Context;
    using Enums;
    using Modifiers;

    /// <summary>
    /// What a set of taken nodes is worth. The final number goes through the game's own
    /// <see cref="Calculations.CalculateFloatValue"/> over real <see cref="SimpleModifier"/> instances
    /// rather than re-adding percentages here, so a panel and a battle can't disagree.
    /// <para>Two readings, mirroring <see cref="ContextKnobTotals.Gather"/>: what the allocation CARRIES
    /// counts a gated line as always-on, since there's no fighter to test the gate against; what the
    /// character HOLDS right now excludes gated lines from every total — a sometimes-true line has no
    /// business in a total presented as fact — and reports them separately instead.</para>
    /// <para>Not the same object as <see cref="PassiveTreeParameterSource"/>: that one is a live
    /// contribution with a lifetime (predicates bound to a fighter and released again) and hands
    /// aggregates over unexpanded since the modifier component folds them at resolution. This is a pure
    /// function with no owner and nothing downstream to fold, so it expands them itself.</para>
    /// </summary>
    public static class PassiveTreeSummary
    {
        /// <summary>What the allocation CARRIES: every line, a gated one counted as if always held and
        /// reported in <see cref="TreeSummary.ConditionalLines"/>.</summary>
        public static TreeSummary Build(PassiveTreeDocument document, AllocationState allocation, IParameterBaseline baseline) =>
            Build(document, allocation.Taken, baseline);

        /// <summary>The same reading over a bare set of ids — for a screen holding a projected allocation
        /// with no allocation object to hand over.</summary>
        public static TreeSummary Build(PassiveTreeDocument document, IEnumerable<string> taken, IParameterBaseline baseline) =>
            Build(document, taken, baseline, skipConditional: false);

        /// <summary>What the character HOLDS: gated lines are left out of every total and only counted,
        /// since a number the player reads as his own must not include a bonus that's off while he reads
        /// it.</summary>
        public static TreeSummary BuildUnconditional(PassiveTreeDocument document, IEnumerable<string> taken, IParameterBaseline baseline) =>
            Build(document, taken, baseline, skipConditional: true);

        private static TreeSummary Build(
            PassiveTreeDocument document,
            IEnumerable<string> taken,
            IParameterBaseline baseline,
            bool skipConditional)
        {
            var accumulators = new Dictionary<EntityParameter, ParameterAccumulator>();
            var summary = new TreeSummary();

            foreach (string id in taken)
            {
                PassiveNode? node = document.Find(id);
                if (node is null) continue;

                Describe(node, summary);
                Count(node, summary);

                foreach (ModifierLine line in node.Modifiers)
                {
                    if (line.ValueType == ModifierValueType.Flag) continue;

                    // A line measured per unit of a carrier parameter has no honest column here: this
                    // reading has no fighter to measure, and folding it against a bare baseline would put
                    // a number in the table nobody actually holds. Counted instead of guessed.
                    if (line.IsScaled || (skipConditional && line.IsConditional)) continue;

                    // An aggregate lands on every family member — the game folds it at resolution, so
                    // "+2 to all attributes" must fold here too.
                    IReadOnlyList<EntityParameter> targets = AggregateParameters.IsAggregate(line.Parameter)
                        ? AggregateParameters.Members(line.Parameter)
                        : [line.Parameter];

                    foreach (EntityParameter target in targets)
                    {
                        if (!accumulators.TryGetValue(target, out ParameterAccumulator? accumulator))
                        {
                            accumulator = new ParameterAccumulator();
                            accumulators[target] = accumulator;
                        }

                        accumulator.Add(target, line);
                    }
                }
            }

            foreach (KeyValuePair<EntityParameter, ParameterAccumulator> pair in accumulators)
                summary.Parameters.Add(pair.Value.ToTotal(pair.Key, baseline.Of(pair.Key)));

            summary.Parameters.Sort(static (first, second) =>
                string.CompareOrdinal(first.Parameter.ToString(), second.Parameter.ToString()));

            // Context lines go through the game's own grouping and summing: one row per knob, holding the
            // number the fighter's single modifier for it reads.
            foreach (KeyValuePair<ContextParameter, List<ContextModifierLine>> knob in ContextKnobTotals.Gather(document, taken))
            {
                int conditional = knob.Value.Count(line => line.IsConditional);

                List<ContextModifierLine> counted = skipConditional
                    ? knob.Value.Where(line => !line.IsConditional).ToList()
                    : knob.Value;

                if (counted.Count == 0) continue;

                summary.Context.Add(new ContextTotal(
                    knob.Key,
                    BucketOf(counted),
                    ContextKnobTotals.AsRead(knob.Key, counted),
                    counted.Count,
                    skipConditional ? 0 : conditional));
            }

            summary.Context.Sort(static (first, second) =>
                string.CompareOrdinal(first.Parameter.ToString(), second.Parameter.ToString()));

            return summary;
        }

        /// <summary>The bucket the knob's lines were authored in — provenance only, since a binding reads
        /// by parameter and never by bucket. Null where lines disagree: one knob written two ways, worth
        /// flagging rather than papering over with whichever bucket won.</summary>
        private static ModifierValueType? BucketOf(List<ContextModifierLine> lines)
        {
            ModifierValueType first = lines[0].ValueType;

            return lines.TrueForAll(line => line.ValueType == first) ? first : null;
        }

        /// <summary>How much of the allocation the totals below don't state plainly, counted in LINES the
        /// player reads rather than records: a composite spelled by three records is one gated line and one
        /// scaled line, not three. Counted before anything is skipped, so both readings say the same about
        /// a line that is gated AND scaled at once.</summary>
        private static void Count(PassiveNode node, TreeSummary summary)
        {
            foreach (NodeLineGroup group in node.LineGroups())
            {
                if (group.IsConditional) summary.ConditionalLines++;
                if (group.IsScaled) summary.ScaledLines++;
            }
        }

        private static void Describe(PassiveNode node, TreeSummary summary)
        {
            if (node.Kind == PassiveNodeKind.Keystone)
            {
                string label = string.IsNullOrWhiteSpace(node.Title) ? node.Id : node.Title;
                summary.Keystones.Add(string.IsNullOrWhiteSpace(node.Description) ? label : $"{label} — {node.Description}");
                return;
            }

            // An empty reference is something Check reports, not a blank entry in the totals.
            if (!NodeKindRules.For(node.Kind).RequiresAbility || string.IsNullOrWhiteSpace(node.AbilityId)) return;

            summary.UnlocksOf(node.Kind).Add(node.AbilityId);
        }

        private sealed class ParameterAccumulator
        {
            private readonly List<IModifier> _modifiers = [];

            private float _flat;
            private float _increase;
            private float _multiplicative;
            private int _lines;
            private int _conditionalLines;

            public void Add(EntityParameter parameter, ModifierLine line)
            {
                _modifiers.Add(line.ToModifier(parameter));
                _lines++;
                if (line.IsConditional) _conditionalLines++;

                switch (line.ValueType)
                {
                    case ModifierValueType.Flat:
                        _flat += line.Value;
                        break;
                    case ModifierValueType.Increase:
                        _increase += line.Value;
                        break;
                    case ModifierValueType.Multiplicative:
                        _multiplicative += line.Value;
                        break;
                    case ModifierValueType.Flag:
                    default:
                        break;
                }
            }

            public ParameterTotal ToTotal(EntityParameter parameter, float baseValue) => new(
                parameter,
                _flat,
                _increase,
                _multiplicative,
                baseValue,
                Calculations.CalculateFloatValue(_modifiers, baseValue),
                _lines,
                _conditionalLines);
        }
    }

    /// <param name="Flat">Sum of the Flat bucket, added to the base before any scaling.</param>
    /// <param name="Increase">Sum of the Increase bucket; the game applies it as (1 + sum).</param>
    /// <param name="Multiplicative">Sum of the Multiplicative bucket; applied as (1 + sum) after Increase.</param>
    /// <param name="ConditionalLines">How many of the lines carry a condition and were counted as if always on.</param>
    public sealed record ParameterTotal(
        EntityParameter Parameter,
        float Flat,
        float Increase,
        float Multiplicative,
        float BaseValue,
        float Total,
        int Lines,
        int ConditionalLines);

    /// <param name="Bucket">Authoring provenance only, null where lines disagree — a knob's value is read
    /// the way its binding reads it, never the way its bucket is spelled.</param>
    /// <param name="Value">Every counted line summed, then read the way the pipeline reads it: a
    /// whole-unit knob shows the turns it really grants rather than a fraction lost at the binding, and a
    /// switch sums to the number of switches taken, one being enough.</param>
    /// <param name="ConditionalLines">Lines carrying a condition, counted as if always on. Zero in the
    /// reading that leaves them out.</param>
    public sealed record ContextTotal(
        ContextParameter Parameter,
        ModifierValueType? Bucket,
        float Value,
        int Lines,
        int ConditionalLines);

    public sealed class TreeSummary
    {
        private readonly Dictionary<PassiveNodeKind, List<string>> _unlocks = [];

        public List<ParameterTotal> Parameters { get; } = [];

        /// <summary>Pipeline knobs, kept apart from <see cref="Parameters"/> since they never enter the
        /// parameter formula — folding them into that table would misstate where they land.</summary>
        public List<ContextTotal> Context { get; } = [];

        public List<string> Keystones { get; } = [];

        /// <summary>Ability references grouped by the class of node that granted them. A class with
        /// nothing taken has no entry, so the panel never prints an empty row.</summary>
        public IReadOnlyDictionary<PassiveNodeKind, List<string>> Unlocks => _unlocks;

        /// <summary>Gated lines the allocation carries — in the counted reading, how much of the total is
        /// only sometimes true; in the excluding reading, how much of the allocation the totals don't
        /// show.</summary>
        public int ConditionalLines { get; set; }

        /// <summary>Lines whose value is per unit of a carrier parameter. Left out of every total — what
        /// they are worth is the carrier's, not the allocation's — so this is how much of the allocation
        /// the numbers above don't cover.</summary>
        public int ScaledLines { get; set; }

        public bool IsEmpty => Parameters.Count == 0 && Context.Count == 0 && Keystones.Count == 0 && _unlocks.Count == 0;

        /// <summary>The write end of <see cref="Unlocks"/>: the list for a class, created on demand.</summary>
        public List<string> UnlocksOf(PassiveNodeKind kind)
        {
            if (!_unlocks.TryGetValue(kind, out List<string>? list))
            {
                list = [];
                _unlocks[kind] = list;
            }

            return list;
        }
    }
}

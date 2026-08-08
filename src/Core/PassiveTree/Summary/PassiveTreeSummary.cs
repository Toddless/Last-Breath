namespace Core.PassiveTree.Summary
{
    using System.Collections.Generic;
    using System.Linq;
    using Allocation;
    using Context;
    using Enums;
    using Modifiers;
    using Modifiers.Context;

    /// <summary>
    /// What a set of taken nodes is actually worth. The final number is produced by the game's own
    /// <see cref="Calculations.CalculateFloatValue"/> over real <see cref="SimpleModifier"/> instances
    /// rather than by re-adding percentages here — a panel and a battle cannot disagree.
    /// <para>Two readings, for the same reason <see cref="ContextKnobTotals.Gather"/> has two: WHAT AN
    /// ALLOCATION CARRIES is the question an authoring tool asks, and it counts a gated line as if it
    /// were always on because there is no fighter to answer the gate against. WHAT THE CHARACTER HOLDS
    /// RIGHT NOW is the question a game screen asks, and a line that only counts sometimes has no
    /// business in a total presented as the truth — it is left out and counted separately, so the panel
    /// can say how much of the allocation it is not showing.</para>
    /// <para>Not the same object as <see cref="PassiveTreeParameterSource"/> and deliberately so: that
    /// one is a live contribution with a lifetime — predicates bound to a fighter and released again —
    /// and it hands aggregates over unexpanded because the modifier component folds them at resolution.
    /// This is a pure function with no owner behind it and nothing downstream to fold, so it expands
    /// them itself.</para>
    /// </summary>
    public static class PassiveTreeSummary
    {
        /// <summary>What the allocation CARRIES: every line of it, a gated one counted as if it always
        /// held and reported in <see cref="TreeSummary.ConditionalLines"/>.</summary>
        public static TreeSummary Build(PassiveTreeDocument document, AllocationState allocation, IParameterBaseline baseline) =>
            Build(document, allocation.Taken, baseline);

        /// <summary>The same reading over a bare set of ids — what a screen holding a projected
        /// allocation has, where there is no allocation object to hand over.</summary>
        public static TreeSummary Build(PassiveTreeDocument document, IEnumerable<string> taken, IParameterBaseline baseline) =>
            Build(document, taken, baseline, skipConditional: false);

        /// <summary>What the character HOLDS: the gated lines are left out of every total and only
        /// counted, because a number a player reads as his own must not include a bonus that is off
        /// while he reads it.</summary>
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

                foreach (ModifierLine line in node.Modifiers)
                {
                    if (line.ValueType == ModifierValueType.Flag) continue;

                    if (line.IsConditional)
                    {
                        summary.ConditionalLines++;
                        if (skipConditional) continue;
                    }

                    // An aggregate is bucket-only: the game folds it into every family member at
                    // resolution, so "+2 to all attributes" has to land on all three here too.
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

            // Context lines go through the game's own grouping and summer: one row per knob holding the
            // number the fighter's single modifier for that knob reads.
            foreach (KeyValuePair<ContextParameter, List<ContextModifierLine>> knob in ContextKnobTotals.Gather(document, taken))
            {
                int conditional = knob.Value.Count(line => line.IsConditional);
                summary.ConditionalLines += conditional;

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

        /// <summary>The bucket the knob's lines were written in — a note on the authoring and nothing the
        /// total is read through: a binding is chosen by the parameter and reads the value, so the same
        /// number does the same thing written as Flat or as Increase, and only the wording of a line's own
        /// sentence follows the bucket. Null where the lines disagree, which is one knob written two ways
        /// and worth pointing at rather than papering over with whichever bucket won.</summary>
        private static ModifierValueType? BucketOf(List<ContextModifierLine> lines)
        {
            ModifierValueType first = lines[0].ValueType;

            return lines.TrueForAll(line => line.ValueType == first) ? first : null;
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

    /// <param name="Bucket">The bucket every line feeding the knob was written in, null where they
    /// disagree. Authoring provenance only — a knob's value is read the way its binding reads it, never
    /// the way its bucket is spelled.</param>
    /// <param name="Value">What a fighter would end up carrying for the knob: every counted line added up
    /// and then read the way the pipeline reads it, so a whole-unit knob shows the turns it really grants
    /// and not the fraction that dies at the binding. A switch sums to the number of switches taken, one
    /// being enough.</param>
    /// <param name="ConditionalLines">How many of the lines carry a condition and were counted as if
    /// always on. Zero in the reading that leaves them out.</param>
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

        /// <summary>Pipeline knobs, kept apart from <see cref="Parameters"/>: they never enter the
        /// parameter formula, so folding them into that table would be a lie about where they land.</summary>
        public List<ContextTotal> Context { get; } = [];

        public List<string> Keystones { get; } = [];

        /// <summary>Ability references grouped by the class of node that granted them. A class with
        /// nothing taken has no entry at all, so the panel never prints an empty row.</summary>
        public IReadOnlyDictionary<PassiveNodeKind, List<string>> Unlocks => _unlocks;

        /// <summary>Gated lines the allocation carries. In the reading that counts them it says how much
        /// of the total is only sometimes true; in the reading that leaves them out it says how much of
        /// the allocation the totals are not showing at all.</summary>
        public int ConditionalLines { get; set; }

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
